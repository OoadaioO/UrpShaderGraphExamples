using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class SDFGenerator : EditorWindow
{
    private Texture2D sourceTexture;
    private Texture2D generatedSDF;
    private Vector2 scrollPosition;
    
    // Configuration parameters
    private RGBFillMode rgbFillMode = RGBFillMode.SolidWhite;
    private float insideDistance = 8f;
    private float outsideDistance = 8f;
    private float postProcessDistance = 0f;
    
    // Preview textures
    private Texture2D sourcePreview;
    private Texture2D sdfPreview;
    
    // Persistent settings keys
    private const string PREF_RGB_FILL_MODE = "SDFGenerator_RGBFillMode";
    private const string PREF_INSIDE_DISTANCE = "SDFGenerator_InsideDistance";
    private const string PREF_OUTSIDE_DISTANCE = "SDFGenerator_OutsideDistance";
    private const string PREF_POST_PROCESS_DISTANCE = "SDFGenerator_PostProcessDistance";
    
    public enum RGBFillMode
    {
        SolidWhite,
        SolidBlack,
        SDF,
        SourceRGB
    }
    
    [MenuItem("Window/SDF Texture Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<SDFGenerator>("SDF Generator");
        window.minSize = new Vector2(400, 500);
        
        // Auto-select texture if one is selected in Project view
        if (Selection.activeObject is Texture2D)
        {
            window.sourceTexture = (Texture2D)Selection.activeObject;
        }
    }
    
    private void OnEnable()
    {
        LoadSettings();
    }
    
    private void OnDisable()
    {
        SaveSettings();
        
        // Clean up preview textures
        if (sourcePreview != null)
            DestroyImmediate(sourcePreview);
        if (sdfPreview != null)
            DestroyImmediate(sdfPreview);
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("SDF Texture Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Source Texture
        EditorGUILayout.LabelField("Source Texture", EditorStyles.boldLabel);
        Texture2D newTexture = (Texture2D)EditorGUILayout.ObjectField(sourceTexture, typeof(Texture2D), false);
        
        if (newTexture != sourceTexture)
        {
            sourceTexture = newTexture;
            UpdatePreviews();
        }
        
        if (sourceTexture == null)
        {
            EditorGUILayout.HelpBox("Select a source texture to begin.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }
        
        EditorGUILayout.Space();
        
        // Configuration
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
        
        rgbFillMode = (RGBFillMode)EditorGUILayout.EnumPopup("RGB Fill Mode", rgbFillMode);
        insideDistance = EditorGUILayout.Slider("Inside Distance", insideDistance, 0f, 32f);
        outsideDistance = EditorGUILayout.Slider("Outside Distance", outsideDistance, 0f, 32f);
        postProcessDistance = EditorGUILayout.Slider("Post-process Distance", postProcessDistance, 0f, 4f);
        
        EditorGUILayout.Space();
        
        // Preview area
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        DrawPreviewArea();
        
        EditorGUILayout.Space();
        
        // Action buttons
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = sourceTexture != null;
        if (GUILayout.Button("Generate", GUILayout.Height(30)))
        {
            GenerateSDF();
        }
        
        GUI.enabled = generatedSDF != null;
        if (GUILayout.Button("Save PNG", GUILayout.Height(30)))
        {
            SaveSDFTexture();
        }
        
        GUI.enabled = generatedSDF != null && sourceTexture != null;
        if (GUILayout.Button("Save Alpha PNG", GUILayout.Height(30)))
        {
            SaveSDFTextureAsAlpha();
        }
        
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawPreviewArea()
    {
        float previewWidth = EditorGUIUtility.currentViewWidth - 40;
        float previewHeight = previewWidth * 0.5f;
        
        Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
        
        // Draw source texture preview
        Rect sourceRect = new Rect(previewRect.x, previewRect.y, previewWidth * 0.48f, previewHeight);
        Rect sdfRect = new Rect(previewRect.x + previewWidth * 0.52f, previewRect.y, previewWidth * 0.48f, previewHeight);
        
        EditorGUI.DrawRect(sourceRect, Color.grey);
        EditorGUI.DrawRect(sdfRect, Color.grey);
        
        if (sourceTexture != null)
        {
            EditorGUI.DrawPreviewTexture(sourceRect, sourceTexture);
            EditorGUI.LabelField(new Rect(sourceRect.x, sourceRect.y - 20, sourceRect.width, 20), "Source", EditorStyles.centeredGreyMiniLabel);
        }
        
        if (generatedSDF != null)
        {
            EditorGUI.DrawPreviewTexture(sdfRect, generatedSDF);
            EditorGUI.LabelField(new Rect(sdfRect.x, sdfRect.y - 20, sdfRect.width, 20), "SDF", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            EditorGUI.LabelField(sdfRect, "Click Generate to create SDF", EditorStyles.centeredGreyMiniLabel);
        }
    }
    
    private void GenerateSDF()
    {
        if (sourceTexture == null)
            return;
        
        // Ensure texture is readable
        string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        
        bool wasReadable = false;
        if (importer != null)
        {
            wasReadable = importer.isReadable;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }
        }
        
        try
        {
            generatedSDF = GenerateSDFTexture(sourceTexture, insideDistance, outsideDistance, postProcessDistance);
            UpdatePreviews();
            
            // Show progress
            EditorUtility.DisplayProgressBar("SDF Generator", "Generation complete!", 1f);
            EditorUtility.ClearProgressBar();
        }
        catch (Exception e)
        {
            Debug.LogError($"SDF Generation failed: {e.Message}");
            EditorUtility.ClearProgressBar();
        }
        finally
        {
            // Restore original settings
            if (importer != null && !wasReadable)
            {
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
        }
    }
    
    private Texture2D GenerateSDFTexture(Texture2D source, float insideDist, float outsideDist, float postProcessDist)
    {
        int width = source.width;
        int height = source.height;
        
        Texture2D sdfTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        
        // Get source pixels
        Color[] sourcePixels = source.GetPixels();
        float[] alphaChannel = new float[width * height];
        
        // Extract alpha channel
        for (int i = 0; i < sourcePixels.Length; i++)
        {
            alphaChannel[i] = sourcePixels[i].a;
        }
        
        // Calculate SDF values
        float[] sdfValues = CalculateSDF(alphaChannel, width, height, insideDist, outsideDist);
        
        // Apply post-processing if enabled
        if (postProcessDist > 0f)
        {
            ApplyPostProcess(sdfValues, width, height, postProcessDist);
        }
        
        // Create final texture
        Color[] outputPixels = new Color[width * height];
        for (int i = 0; i < outputPixels.Length; i++)
        {
            float sdf = sdfValues[i];
            Color rgb = GetRGBColor(sdf, sourcePixels[i]);
            outputPixels[i] = new Color(rgb.r, rgb.g, rgb.b, sdf);
        }
        
        sdfTexture.SetPixels(outputPixels);
        sdfTexture.Apply();
        
        return sdfTexture;
    }
    
    private float[] CalculateSDF(float[] alpha, int width, int height, float insideDist, float outsideDist)
    {
        float[] sdf = new float[width * height];
        float maxDist = Mathf.Max(insideDist, outsideDist);
        
        // Simple SDF approximation - distance from edge
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float alphaValue = alpha[index];
                
                if (alphaValue <= 0f)
                {
                    // Outside - find nearest edge
                    float minDist = FindNearestEdge(alpha, x, y, width, height);
                    sdf[index] = Mathf.Clamp01(0.5f - (minDist / outsideDist) * 0.5f);
                }
                else if (alphaValue >= 1f)
                {
                    // Inside - find nearest edge
                    float minDist = FindNearestEdge(alpha, x, y, width, height);
                    sdf[index] = Mathf.Clamp01(0.5f + (minDist / insideDist) * 0.5f);
                }
                else
                {
                    // On edge
                    sdf[index] = 0.5f;
                }
            }
        }
        
        return sdf;
    }
    
    private float FindNearestEdge(float[] alpha, int x, int y, int width, int height)
    {
        float minDist = float.MaxValue;
        float centerAlpha = alpha[y * width + x];
        
        // Full image search for maximum accuracy
        for (int ny = 0; ny < height; ny++)
        {
            for (int nx = 0; nx < width; nx++)
            {
                if (nx == x && ny == y) continue; // Skip self
                
                float neighborAlpha = alpha[ny * width + nx];
                
                // Check if this is an edge pixel (transition across 0.5 threshold)
                if ((centerAlpha < 0.5f && neighborAlpha >= 0.5f) ||
                    (centerAlpha >= 0.5f && neighborAlpha < 0.5f))
                {
                    float dx = nx - x;
                    float dy = ny - y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    minDist = Mathf.Min(minDist, dist);
                }
            }
        }
        
        return minDist == float.MaxValue ? 0f : minDist;
    }
    
    private void ApplyPostProcess(float[] sdf, int width, int height, float postProcessDist)
    {
        // Brute-force refinement for pixels near edges
        int radius = Mathf.CeilToInt(postProcessDist);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                
                // Only process pixels near the edge (0.4 to 0.6 range)
                if (Mathf.Abs(sdf[index] - 0.5f) < 0.1f)
                {
                    // More accurate distance calculation
                    float refinedDist = CalculateAccurateDistance(sdf, x, y, width, height, radius);
                    sdf[index] = refinedDist;
                }
            }
        }
    }
    
    private float CalculateAccurateDistance(float[] sdf, int x, int y, int width, int height, int radius)
    {
        // Placeholder for more accurate distance calculation
        return sdf[y * width + x]; // For now, return original
    }
    
    private Color GetRGBColor(float sdf, Color sourceColor)
    {
        return rgbFillMode switch
        {
            RGBFillMode.SolidWhite => new Color(sdf, sdf, sdf), // SDF=0=black, SDF=1=white
            RGBFillMode.SolidBlack => new Color(1f - sdf, 1f - sdf, 1f - sdf), // SDF=0=white, SDF=1=black
            RGBFillMode.SDF => new Color(sdf, sdf, sdf), // Use SDF value directly as grayscale
            RGBFillMode.SourceRGB => new Color(sourceColor.r, sourceColor.g, sourceColor.b), // Use source texture RGB
            _ => new Color(sdf, sdf, sdf)
        };
    }
    
    private void SaveSDFTexture()
    {
        if (generatedSDF == null)
            return;
        
        string defaultPath = "Assets";
        if (sourceTexture != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
            if (!string.IsNullOrEmpty(assetPath))
            {
                defaultPath = Path.GetDirectoryName(assetPath);
            }
        }
        
        string savePath = EditorUtility.SaveFilePanel(
            "Save SDF Texture",
            defaultPath,
            sourceTexture != null ? $"{sourceTexture.name}_SDF.png" : "SDF_Texture.png",
            "png"
        );
        
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                byte[] bytes = generatedSDF.EncodeToPNG();
                File.WriteAllBytes(savePath, bytes);
                
                // Refresh AssetDatabase if saved in project
                if (savePath.StartsWith(Application.dataPath))
                {
                    string relativePath = $"Assets{savePath[Application.dataPath.Length..]}";

                    AssetDatabase.ImportAsset(relativePath);
                    
                    // Configure import settings
                    TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.SingleChannel;
                        importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                        importer.alphaIsTransparency = true;
                        importer.SaveAndReimport();
                    }
                }
                
                Debug.Log($"SDF texture saved to: {savePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save SDF texture: {e.Message}");
            }
        }
    }
    
    private void SaveSDFTextureAsAlpha()
    {
        if (generatedSDF == null || sourceTexture == null)
            return;
        
        string defaultPath = "Assets";
        string assetPath = AssetDatabase.GetAssetPath(sourceTexture);
        if (!string.IsNullOrEmpty(assetPath))
        {
            defaultPath = Path.GetDirectoryName(assetPath);
        }
        
        string savePath = EditorUtility.SaveFilePanel(
            "Save SDF Alpha Texture",
            defaultPath,
            $"{sourceTexture.name}_SDF_Alpha.png",
            "png"
        );
        
        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                // Create new texture with source RGB and SDF alpha
                int width = sourceTexture.width;
                int height = sourceTexture.height;
                
                // Ensure source texture is readable
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                bool wasReadable = false;
                if (importer != null)
                {
                    wasReadable = importer.isReadable;
                    if (!wasReadable)
                    {
                        importer.isReadable = true;
                        importer.SaveAndReimport();
                    }
                }
                
                try
                {
                    // Get source pixels and SDF values
                    Color[] sourcePixels = sourceTexture.GetPixels();
                    float[] alphaChannel = new float[width * height];
                    
                    // Extract alpha channel for SDF calculation
                    for (int i = 0; i < sourcePixels.Length; i++)
                    {
                        alphaChannel[i] = sourcePixels[i].a;
                    }
                    
                    // Calculate SDF values
                    float[] sdfValues = CalculateSDF(alphaChannel, width, height, insideDistance, outsideDistance);
                    
                    // Create texture with source RGB and SDF alpha
                    Texture2D alphaTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    Color[] outputPixels = new Color[width * height];
                    
                    for (int i = 0; i < outputPixels.Length; i++)
                    {
                        // Keep original RGB values, use SDF as alpha
                        outputPixels[i] = new Color(sourcePixels[i].r, sourcePixels[i].g, sourcePixels[i].b, sdfValues[i]);
                    }
                    
                    alphaTexture.SetPixels(outputPixels);
                    alphaTexture.Apply();
                    
                    // Save the texture
                    byte[] bytes = alphaTexture.EncodeToPNG();
                    File.WriteAllBytes(savePath, bytes);
                    
                    // Clean up temporary texture
                    DestroyImmediate(alphaTexture);
                    
                    // Refresh AssetDatabase if saved in project
                    if (savePath.StartsWith(Application.dataPath))
                    {
                        string relativePath = $"Assets{savePath[Application.dataPath.Length..]}";
                        AssetDatabase.ImportAsset(relativePath);
                        
                        TextureImporter alphaImporter = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                        if (alphaImporter != null)
                        {
                            alphaImporter.alphaIsTransparency = true;
                            alphaImporter.SaveAndReimport();
                        }
                    }
                    
                    Debug.Log($"SDF alpha texture saved to: {savePath}");
                }
                finally
                {
                    // Restore original settings
                    if (importer != null && !wasReadable)
                    {
                        importer.isReadable = false;
                        importer.SaveAndReimport();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save SDF alpha texture: {e.Message}");
            }
        }
    }
    
    private void UpdatePreviews()
    {
        // Update source preview
        if (sourcePreview != null)
            DestroyImmediate(sourcePreview);
        
        // Update SDF preview
        if (sdfPreview != null)
            DestroyImmediate(sdfPreview);
    }
    
    private void LoadSettings()
    {
        rgbFillMode = (RGBFillMode)EditorPrefs.GetInt(PREF_RGB_FILL_MODE, (int)RGBFillMode.SolidWhite);
        insideDistance = EditorPrefs.GetFloat(PREF_INSIDE_DISTANCE, 8f);
        outsideDistance = EditorPrefs.GetFloat(PREF_OUTSIDE_DISTANCE, 8f);
        postProcessDistance = EditorPrefs.GetFloat(PREF_POST_PROCESS_DISTANCE, 0f);
    }
    
    private void SaveSettings()
    {
        EditorPrefs.SetInt(PREF_RGB_FILL_MODE, (int)rgbFillMode);
        EditorPrefs.SetFloat(PREF_INSIDE_DISTANCE, insideDistance);
        EditorPrefs.SetFloat(PREF_OUTSIDE_DISTANCE, outsideDistance);
        EditorPrefs.SetFloat(PREF_POST_PROCESS_DISTANCE, postProcessDistance);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TentacleInteract : MonoBehaviour {
    [SerializeField] private Material material;
    [SerializeField] private Transform obj;
    [SerializeField] private Vector3 offset;

    [SerializeField] private float scaleMin = 0.5f;

    private void Update() {
        if (material == null || obj == null) return;
        float radius = obj.localScale.x - scaleMin;
        material.SetFloat("_Radius", radius);
        material.SetVector("_InteractPos", transform.InverseTransformPoint(obj.position + offset * radius));
    }
    
}

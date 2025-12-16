#ifndef CUSTOM_PERLIN2D_INCLUDED
#define CUSTOM_PERLIN2D_INCLUDED

#define C_PI 3.14159265f

float rand(float2 pos, float time)
{
    return frac(sin(dot(pos + time, float2(12.9898f, 78.233f))) * 43758.5453123f);
}

float2 randUnitCircle(float2 pos, float time)
{

    float randVal = rand(pos, time);
    float theta = 2.0f * C_PI * randVal;
    return float2(cos(theta), sin(theta));
}

float quinterp(float f)
{
    return f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
}


float perlin2D_core(float2 pixel, float time)
{
    float2 pos00 = floor(pixel);
    float2 pos10 = pos00 + float2(1.0f, 0.0f);
    float2 pos01 = pos00 + float2(0.0f, 1.0f);
    float2 pos11 = pos00 + float2(1.0f, 1.0f);

    float2 rand00 = randUnitCircle(pos00, time);
    float2 rand10 = randUnitCircle(pos10, time);
    float2 rand01 = randUnitCircle(pos01, time);
    float2 rand11 = randUnitCircle(pos11, time);

    float dot00 = dot(rand00, pixel - pos00);
    float dot10 = dot(rand10, pixel - pos10);
    float dot01 = dot(rand01, pixel - pos01);
    float dot11 = dot(rand11, pixel - pos11);

    float2 d = frac(pixel);

    float x1 = lerp(dot00, dot10, quinterp(d.x));
    float x2 = lerp(dot01, dot11, quinterp(d.x));
    float y  = lerp(x1, x2, quinterp(d.y));

    return y;
}

// 👉 Shader Graph-compatible wrapper
void perlin2D_float(float2 pixel, float time, out float Out)
{
    Out = perlin2D_core(pixel, time);
}

#endif // CUSTOM_PERLIN2D_INCLUDED

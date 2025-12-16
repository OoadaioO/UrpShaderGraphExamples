#ifndef CUSTOM_SCREEN_ASPECT_INCLUDED
#define CUSTOM_SCREEN_ASPECT_INCLUDED



void GetAspectRatio_float(float2 screenParams, out float aspect)
{
    aspect = screenParams.x / screenParams.y;
}


#endif // CUSTOM_SCREEN_ASPECT_INCLUDED

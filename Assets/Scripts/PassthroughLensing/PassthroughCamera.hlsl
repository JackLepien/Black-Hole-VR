#ifndef PASSTHROUGH_CAMERA_INCLUDED
#define PASSTHROUGH_CAMERA_INCLUDED

// Globals set each frame by PassthroughLensDriver.cs
TEXTURE2D(_PCA_Texture);
SAMPLER(sampler_PCA_Texture);
float4 _PCA_Position;        // xyz: passthrough camera world position at capture time
float4x4 _PCA_WorldToCamera; // rotation only (world -> camera space)
float4 _PCA_Intrinsics;      // x,y: focal length (px)   z,w: principal point (px)
float4 _PCA_Crop;            // x,y: crop origin (px)    z,w: crop size (px)
float _PCA_Valid;            // 0 until the camera stream is running

// Drop-in replacement for the Scene Color node.
//   ScreenUV         the same 0-1 screen UV you currently feed into Scene Color
//   AssumedDistance  metres; how far away the real-world content is assumed to be
//                    (corrects for the camera being offset from the eye). Try 2-4.
//   InvertY / FlipV  debugging toggles (0 or 1) if the image is upside down or mirrored
//   Color            passthrough colour at that ray
//   Valid            0-1 mask: 0 when no camera data or the ray leaves the camera's view
void PCA_SceneColor_float(float2 ScreenUV, float AssumedDistance, float InvertY, float FlipV,
                          out float3 Color, out float Valid)
{
    Color = float3(0.0, 0.0, 0.0);
    Valid = 0.0;

    if (_PCA_Valid < 0.5)
        return;

    // Screen UV -> clip space of the eye currently being rendered.
    // _ProjectionParams.x is -1 when the projection is flipped (render-to-texture),
    // which mirrors how ComputeScreenPos builds the UV.
    float2 ndc = ScreenUV * 2.0 - 1.0;
    ndc.y *= _ProjectionParams.x;
    if (InvertY > 0.5)
        ndc.y = -ndc.y;

    float4 nearWS = mul(UNITY_MATRIX_I_VP, float4(ndc, UNITY_NEAR_CLIP_VALUE, 1.0));
    nearWS.xyz /= nearWS.w;

    float3 eyePos = UNITY_MATRIX_I_V._m03_m13_m23;
    float3 dir = normalize(nearWS.xyz - eyePos);

    // Point on the eye ray at the assumed distance, then seen from the passthrough camera.
    float3 P = eyePos + dir * max(AssumedDistance, 0.01);
    float3 pc = mul((float3x3)_PCA_WorldToCamera, P - _PCA_Position.xyz);

    if (pc.z <= 0.0001)
        return;

    float2 sensorPx = pc.xy / pc.z * _PCA_Intrinsics.xy + _PCA_Intrinsics.zw;
    float2 vp = (sensorPx - _PCA_Crop.xy) / _PCA_Crop.zw;
    if (FlipV > 0.5)
        vp.y = 1.0 - vp.y;

    // Fade out near the edge of the camera image so the effect doesn't end in a hard line.
    float2 edge = saturate(min(vp, 1.0 - vp) / 0.03);
    Valid = edge.x * edge.y;
    if (Valid <= 0.0)
        return;

    Color = SAMPLE_TEXTURE2D(_PCA_Texture, sampler_PCA_Texture, saturate(vp)).rgb;
}

#endif

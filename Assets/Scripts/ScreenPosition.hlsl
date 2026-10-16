#ifndef SCREEN_POSITION_INCLUDED
#define SCREEN_POSITION_INCLUDED

void GetScreenPosition_float(float3 Position, out float2 ScreenPosition, out float2 ScreenPositionAspectRation) {
    float4 screen = ComputeScreenPos(TransformWorldToHClip(Position), _ProjectionParams.x);
    ScreenPosition = screen.xy / abs(screen.w);

    float aspectRatio = _ScreenParams.y / _ScreenParams.x;
    ScreenPositionAspectRation = float2(ScreenPosition.x, ScreenPosition.y * aspectRatio);
}

#endif
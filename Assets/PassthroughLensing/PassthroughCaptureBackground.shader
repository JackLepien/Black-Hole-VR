// Draws the Quest passthrough camera image across the whole view, using the same
// ray -> camera-image mapping as PassthroughCamera.hlsl, so screenshots/recordings
// (which normally omit the system passthrough layer) contain a background.
// Drawn first (Background queue); the black hole renders on top of it.
Shader "PassthroughLensing/CaptureBackground"
{
    Properties
    {
        _Distance ("Assumed Distance (m)", Float) = 3
        _Opacity ("Opacity", Range(0, 1)) = 1
        [Toggle] _InvertY ("Invert Y", Float) = 0
        [Toggle] _FlipV ("Flip V", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Background" }

        Pass
        {
            Name "CaptureBackground"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/PassthroughLensing/PassthroughCamera.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Distance;
                float _Opacity;
                float _InvertY;
                float _FlipV;
            CBUFFER_END

            struct Attributes
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 clipXY : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // One oversized triangle covering the whole screen, ignoring the object transform.
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                float2 clip = uv * 2.0 - 1.0;
                output.positionCS = float4(clip, UNITY_RAW_FAR_CLIP_VALUE, 1.0);
                output.clipXY = clip;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Convert the clip position we drew at into the screen-UV convention
                // PCA_SceneColor_float expects (it re-applies _ProjectionParams.x itself).
                float2 ndc = float2(input.clipXY.x, input.clipXY.y * _ProjectionParams.x);
                float2 screenUV = ndc * 0.5 + 0.5;

                float3 color;
                float valid;
                PCA_SceneColor_float(screenUV, _Distance, _InvertY, _FlipV, color, valid);
                return half4(color, valid * _Opacity);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

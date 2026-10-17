// GPU-driven stars that orbit and spiral into a black hole. All motion is computed here from
// per-star random seeds stored in the mesh (see StarInflow.cs), so there is no per-frame CPU work.
// Stars that pass behind the event horizon (as seen from the eye) are discarded, and each star
// shrinks away as it reaches the horizon before recycling at a new outer orbit.
Shader "StarInflow/Stars"
{
    Properties
    {
        _ColorA ("Cool Star Color", Color) = (0.65, 0.78, 1.0, 1)
        _ColorB ("Warm Star Color", Color) = (1.0, 0.88, 0.65, 1)
        _HotColor ("Near-Horizon Color", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Float) = 2

        [HideInInspector] _Center ("Center", Vector) = (0, 0, 0, 0)
        [HideInInspector] _Axis ("Axis", Vector) = (0, 1, 0, 0)
        // x: horizon radius, y: outer radius, z: inner start radius, w: star size (all metres)
        [HideInInspector] _P0 ("Radii", Vector) = (1, 8, 3, 0.02)
        // x: lifetime (s), y: orbit turns per life, z: spin-up, w: infall exponent
        [HideInInspector] _P1 ("Motion", Vector) = (20, 3, 2, 2.5)
        // x: flatten (0 sphere .. 1 disc)
        [HideInInspector] _P2 ("Shape", Vector) = (0.85, 0, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent+1" }

        Pass
        {
            Name "Stars"
            // Premultiplied alpha: correct for both normal rendering and Quest passthrough compositing.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float4 _HotColor;
                float _Brightness;
                float4 _Center;
                float4 _Axis;
                float4 _P0;
                float4 _P1;
                float4 _P2;
            CBUFFER_END

            struct Attributes
            {
                float3 corner : POSITION;   // xy: quad corner in [-1, 1]
                float4 seedA : TEXCOORD1;   // x: speed, y: phase, z: start radius, w: start angle
                float4 seedB : TEXCOORD2;   // x: size/color, yzw: orbit-plane randomness
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                float3 centerWS : TEXCOORD1;
                float2 ageColor : TEXCOORD2; // x: age, y: colour seed
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                const float TAU = 6.2831853;
                float horizon = _P0.x;
                float outerR = _P0.y;
                float innerR = _P0.z;
                float size = _P0.w;
                float lifetime = max(_P1.x, 0.01);

                // Age 0..1 loops forever; at 1 the star has reached the horizon and wraps to a new orbit.
                float speedJitter = lerp(0.8, 1.2, input.seedA.x);
                float cycle = _Time.y / lifetime * speedJitter + input.seedA.y;
                float age = frac(cycle);
                // Re-randomise the start radius/angle each cycle so loops don't repeat exactly.
                float cycleId = floor(cycle);
                float reroll = frac(sin(dot(float2(input.seedA.z + cycleId * 0.137, input.seedA.w), float2(12.9898, 78.233))) * 43758.5453);

                float r0 = lerp(innerR, outerR, sqrt(frac(input.seedA.z + reroll)));
                // Slow drift at first, plunging near the end.
                float r = lerp(r0, horizon, pow(age, _P1.w));

                // Orbit speeds up as the star falls in (spin-up term).
                float turns = _P1.y * lerp(0.8, 1.2, input.seedB.x);
                float phi = frac(input.seedA.w + reroll * 0.5) * TAU + turns * TAU * (age + _P1.z * age * age * age);

                // Orbit plane: near the axis' disc, with randomness controlled by Flatten.
                float3 rnd = input.seedB.yzw * 2.0 - 1.0;
                float3 axis = normalize(_Axis.xyz);
                float3 n = normalize(axis + rnd * (1.0 - _P2.x) * 2.0 + 1e-4);
                float3 helper = abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0);
                float3 u = normalize(cross(helper, n));
                float3 v = cross(n, u);

                float3 centerWS = _Center.xyz + (u * cos(phi) + v * sin(phi)) * r;

                // Fade in at spawn, shrink away at the horizon.
                float s = size * lerp(0.6, 1.5, input.seedB.x)
                        * smoothstep(0.0, 0.05, age) * (1.0 - smoothstep(0.88, 1.0, age));

                float3 camRight = UNITY_MATRIX_V[0].xyz;
                float3 camUp = UNITY_MATRIX_V[1].xyz;
                float3 positionWS = centerWS + (camRight * input.corner.x + camUp * input.corner.y) * s;

                output.positionCS = TransformWorldToHClip(positionWS);
                output.corner = input.corner.xy;
                output.centerWS = centerWS;
                output.ageColor = float2(age, input.seedB.x);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float dist = length(input.corner);
                if (dist > 1.0)
                    discard;

                // Hide stars that are behind the event horizon from where the eye is.
                float3 eye = UNITY_MATRIX_I_V._m03_m13_m23;
                float3 toStar = input.centerWS - eye;
                float starDist = length(toStar);
                float3 dir = toStar / max(starDist, 1e-4);
                float3 L = _Center.xyz - eye;
                float tca = dot(L, dir);
                float d2 = dot(L, L) - tca * tca;
                if (tca > 0.0 && tca < starDist && d2 < _P0.x * _P0.x)
                    discard;

                float soft = 1.0 - dist;
                soft *= soft;

                float age = input.ageColor.x;
                float heat = smoothstep(0.55, 1.0, age);
                float3 baseColor = lerp(_ColorA.rgb, _ColorB.rgb, input.ageColor.y);
                float3 color = lerp(baseColor, _HotColor.rgb, heat) * _Brightness * (1.0 + heat * 2.0);

                float alpha = saturate(soft);
                return half4(color * alpha, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}

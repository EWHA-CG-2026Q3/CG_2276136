Shader "CG/Mid_ShardAfterglow"
{
    Properties
    {
        _PreviewTime ("Preview Time", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha One
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _PreviewTime;

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _PreviewTime > 0.001 ? _PreviewTime : _Time.y;
                float u = saturate(input.uv.x);
                float v = input.uv.y;

                // Soft cross-section beam profile (bright photon core at v=0.5, soft aura toward edges)
                float distFromCenter = abs(v - 0.5) * 2.0;
                float coreBeam = exp(-distFromCenter * distFromCenter * 8.5);
                float softAura = pow(saturate(1.0 - distFromCenter), 1.8);

                // Smooth head-to-tail decay: dense and bright right behind the shard (u=0), fading over the long arc (u->1)
                float headConcentrate = exp(-u * 12.0);
                float tailFade = pow(1.0 - u, 1.85);

                // Photon cluster scintillation / subtle flickering along the orbital arc
                float wave1 = sin(u * 42.0 - t * 6.5 + input.positionWS.x * 3.1);
                float wave2 = sin(u * 95.0 - t * 11.2 + input.positionWS.z * 4.7);
                float wave3 = cos(u * 160.0 + t * 4.3 + input.positionWS.y * 5.3);
                float photonCluster = 0.72 + 0.28 * (0.5 * wave1 + 0.35 * wave2 + 0.15 * wave3);

                // Fine starlight sparkles embedded within the stream
                float sparkle = pow(saturate(0.5 + 0.5 * sin(u * 130.0 - t * 8.0) * cos(u * 78.0 + t * 5.2)), 4.0);
                float shimmer = lerp(1.0, photonCluster + sparkle * 0.65, saturate(u * 3.5));

                float intensity = (coreBeam * 1.45 + softAura * 0.55) * (tailFade + headConcentrate * 0.95) * shimmer;
                half3 hdrColor = input.color.rgb * (1.25 + headConcentrate * 1.35 + sparkle * 0.55);
                half alpha = saturate(input.color.a * intensity);

                return half4(hdrColor, alpha);
            }
            ENDHLSL
        }
    }
}


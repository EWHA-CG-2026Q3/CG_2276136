Shader "CG/Mid_HearthFlame"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _PreviewTime ("Capture Time", Float) = -1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Ember Flame"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _PreviewTime;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float seconds = _PreviewTime < 0 ? _Time.y : _PreviewTime;
                float phase = input.positionOS.z * 12.0 + input.positionOS.x * 15.0;
                float bend = input.uv.y * input.uv.y;
                float3 position = input.positionOS;
                position.x += sin(seconds * 7.0 + phase + input.uv.y * 2.4) * bend * 0.039;
                position.z += cos(seconds * 5.1 + phase) * bend * 0.026;
                position.y *= 0.92 + 0.17 * sin(seconds * 9.0 + phase);
                output.positionCS = TransformObjectToHClip(position);
                output.uv = input.uv;
                output.color = input.color * _BaseColor;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float width = 1.0 - abs(input.uv.x * 2.0 - 1.0);
                float alpha = smoothstep(0.01, 0.66, width) *
                    smoothstep(0.0, 0.15, input.uv.y) * (1.0 - smoothstep(0.58, 1.0, input.uv.y));
                float core = smoothstep(0.35, 0.96, width) * (1.0 - input.uv.y);
                return half4(input.color.rgb * (0.8 + core * 0.7), alpha * 0.92);
            }
            ENDHLSL
        }
    }
}

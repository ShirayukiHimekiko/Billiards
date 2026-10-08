Shader "Billiards/RollingBall"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _RollPhase ("Rolling phase", Float) = 0
        _RollDirection ("Rolling direction", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                float _RollPhase;
                float4 _RollDirection;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = (input.uv - 0.5) * 2;
                float r2 = dot(p, p);
                clip(1 - r2);

                float z = sqrt(saturate(1 - r2));
                float3 n = float3(p, z);
                float diffuse = saturate(dot(n, normalize(float3(-0.45, 0.55, 1))));
                float along = dot(p, _RollDirection.xy);
                float across = dot(p, float2(-_RollDirection.y, _RollDirection.x));

                // 球面上的淡金色标记绕滚动轴旋转；亮斑保持固定光照方向。
                float latitude = along * cos(_RollPhase) + z * sin(_RollPhase);
                float marking = 1 - smoothstep(0.08, 0.13, abs(latitude));
                marking *= 1 - smoothstep(0.22, 0.32, abs(across));

                float3 baseColor = lerp(
                    float3(0.96, 0.95, 0.89),
                    float3(0.65, 0.55, 0.34),
                    marking * 0.55);
                float highlight = pow(saturate(dot(n, normalize(float3(-0.32, 0.40, 1)))), 36);
                float3 color = baseColor * (0.48 + 0.52 * diffuse) + highlight * 0.25;
                float alpha = 1 - smoothstep(1 - fwidth(r2) * 1.5, 1, r2);

                return half4(color * input.color.rgb, alpha * input.color.a);
            }

            ENDHLSL
        }
    }
}

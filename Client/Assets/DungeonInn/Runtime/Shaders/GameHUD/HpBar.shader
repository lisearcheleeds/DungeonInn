Shader "DungeonInn/GameHUD/HpBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FillRatio ("Fill Ratio", Range(0, 1)) = 1
        _FillColor ("Fill Color", Color) = (0.1, 0.9, 0.25, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _BorderColor ("Border Color", Color) = (0, 0, 0, 1)
        _HighlightColor ("Highlight Color", Color) = (0.55, 1, 0.55, 1)
        _ReferenceSizePixels ("Reference Size Pixels", Vector) = (60, 28, 0, 0)
        _VerticalMarginPixels ("Vertical Margin Pixels", Float) = 10
        _BorderPixels ("Border Pixels", Float) = 12
        _HighlightPixels ("Highlight Pixels", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZTest Always
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _FillRatio;
                float4 _FillColor;
                float4 _BackgroundColor;
                float4 _BorderColor;
                float4 _HighlightColor;
                float4 _ReferenceSizePixels;
                float _VerticalMarginPixels;
                float _BorderPixels;
                float _HighlightPixels;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 textureColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float2 pixel = input.uv * _ReferenceSizePixels.xy;
                float bodyMinY = _VerticalMarginPixels;
                float bodyMaxY = _ReferenceSizePixels.y - _VerticalMarginPixels;
                float bodyVisible = step(bodyMinY, pixel.y) * (1.0 - step(bodyMaxY, pixel.y));
                float fillRatio = saturate(input.color.a);
                float innerMinX = _BorderPixels;
                float innerMaxX = _ReferenceSizePixels.x - _BorderPixels;
                float innerMinY = bodyMinY + _BorderPixels;
                float innerMaxY = bodyMaxY - _BorderPixels;
                float innerVisible = step(innerMinX, pixel.x) *
                    (1.0 - step(innerMaxX, pixel.x)) *
                    step(innerMinY, pixel.y) *
                    (1.0 - step(innerMaxY, pixel.y));

                float fillEndX = lerp(innerMinX, innerMaxX, fillRatio);
                float fillVisible = innerVisible * (1.0 - step(fillEndX, pixel.x));
                float highlightVisible = fillVisible *
                    step(innerMinY, pixel.y) *
                    (1.0 - step(innerMinY + _HighlightPixels, pixel.y));
                float borderVisible = saturate(bodyVisible - innerVisible);

                float4 barColor = _BackgroundColor * bodyVisible;
                barColor = lerp(barColor, _FillColor, fillVisible);
                barColor = lerp(barColor, _HighlightColor, highlightVisible);
                barColor = lerp(barColor, _BorderColor, borderVisible);
                barColor.a *= textureColor.a * bodyVisible;
                return barColor;
            }
            ENDHLSL
        }
    }
}

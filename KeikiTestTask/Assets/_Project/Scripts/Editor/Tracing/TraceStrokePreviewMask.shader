Shader "Hidden/Keiki/Trace Stroke Preview Mask"
{
    Properties
    {
        _MainTex ("Silhouette", 2D) = "white" {}
        _Color ("Stroke Color", Color) = (1, 0, 0, 1)
        _SpriteRect ("Sprite Rect", Vector) = (0, 0, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _SpriteRect;
            fixed4 _Color;

            struct VertexInput
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            VertexOutput Vertex(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                return output;
            }

            fixed4 Fragment(VertexOutput input) : SV_Target
            {
                if (input.uv.x < 0.0 ||
                    input.uv.x > 1.0 ||
                    input.uv.y < 0.0 ||
                    input.uv.y > 1.0)
                {
                    discard;
                }

                float2 textureUv =
                    _SpriteRect.xy + input.uv * _SpriteRect.zw;
                fixed maskAlpha = tex2D(_MainTex, textureUv).a;
                fixed4 color = _Color;
                color.a *= maskAlpha;
                return color;
            }
            ENDCG
        }
    }
}

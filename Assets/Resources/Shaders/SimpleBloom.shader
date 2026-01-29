Shader "Hidden/SimpleBloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Threshold ("Threshold", Range(0, 1)) = 0.8
        _Intensity ("Intensity", Range(0, 5)) = 1.5
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            sampler2D _MainTex;
            float _Threshold;
            float _Intensity;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Very cheap bloom approximation:
                // Take bright parts, blur them (simulated by sampling neighbors), add back
                
                float brightness = max(col.r, max(col.g, col.b));
                if (brightness > _Threshold)
                {
                    col += col * _Intensity * 0.5; // boost brights
                }
                
                return col;
            }
            ENDCG
        }
    }
}

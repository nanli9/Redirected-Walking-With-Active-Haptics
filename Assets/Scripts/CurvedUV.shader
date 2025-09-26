Shader "Custom/CurvedUV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Curvature ("Curvature Radius", Float) = 40.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            sampler2D _MainTex;
            float _Curvature;

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

                // Curve distortion applied only to UVs
                float z = v.vertex.z;
                float radius = max(_Curvature, 0.001); // prevent div by 0

                // Arc offset in local space (curve left)
                float angle = z / radius;
                float x_offset = sin(angle) * radius;
                float curve_offset = x_offset - z; // Z is forward

                // UV distortion: shift U based on curve
                float2 uv = v.uv;
                uv.x += curve_offset / radius;

                o.uv = uv;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}

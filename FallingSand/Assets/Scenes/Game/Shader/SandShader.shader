Shader "Custom/SandShader" {
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.0
        _Alpha ("Alpha", Range(0.0, 1.0)) = 1.0
        _OutlineWidth ("Outline Width", Range(0.0005, 0.005)) = 0.002
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThreshold ("Outline Threshold", Range(0.1, 1.0)) = 0.5
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
        }
        LOD 100
         
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
         
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
                fixed4 color : COLOR;
            };
             
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };
             
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Brightness;
            float _Alpha;
            float _OutlineWidth;
            fixed4 _OutlineColor;
            float _OutlineThreshold;
             
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
             
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample texture gốc
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Nếu pixel hiện tại có alpha thấp, bỏ qua
                if (col.a < 0.1)
                {
                    return fixed4(0,0,0,0);
                }
                
                // Edge detection - Sobel operator
                float2 texelSize = _OutlineWidth;
                
                // Sample các pixel xung quanh
                float tl = tex2D(_MainTex, i.uv + float2(-texelSize.x, texelSize.y)).a;   // top left
                float tm = tex2D(_MainTex, i.uv + float2(0, texelSize.y)).a;             // top middle
                float tr = tex2D(_MainTex, i.uv + float2(texelSize.x, texelSize.y)).a;   // top right
                float ml = tex2D(_MainTex, i.uv + float2(-texelSize.x, 0)).a;            // middle left
                float mr = tex2D(_MainTex, i.uv + float2(texelSize.x, 0)).a;             // middle right
                float bl = tex2D(_MainTex, i.uv + float2(-texelSize.x, -texelSize.y)).a; // bottom left
                float bm = tex2D(_MainTex, i.uv + float2(0, -texelSize.y)).a;            // bottom middle
                float br = tex2D(_MainTex, i.uv + float2(texelSize.x, -texelSize.y)).a;  // bottom right
                
                // Sobel X
                float sobelX = (tr + 2.0 * mr + br) - (tl + 2.0 * ml + bl);
                // Sobel Y  
                float sobelY = (tl + 2.0 * tm + tr) - (bl + 2.0 * bm + br);
                
                // Tính độ lớn của gradient
                float edge = sqrt(sobelX * sobelX + sobelY * sobelY);
                
                // Nếu là edge, tạo viền đen
                if (edge > _OutlineThreshold)
                {
                    // Blend viền với màu gốc
                    col.rgb = lerp(col.rgb * _Brightness, _OutlineColor.rgb, _OutlineColor.a);
                }
                else
                {
                    // Pixel bình thường
                    col.rgb *= _Brightness;
                }
                
                col *= i.color;
                col.a *= _Alpha;
                
                return col;
            }
            ENDCG
        }
    }
}
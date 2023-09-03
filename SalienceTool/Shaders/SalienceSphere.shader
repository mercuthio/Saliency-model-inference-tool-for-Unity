Shader "Hidden/SalienceSphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _SecondTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        // No culling or depth
        Cull Front Blend SrcAlpha OneMinusSrcAlpha

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
            sampler2D _SecondTex;

            fixed4 grayToColorMap(fixed4 pixel)
            {
                fixed4 blue =   fixed4(0,0,1,1);
                fixed4 cyan =   fixed4(0,1,1,1);
                fixed4 green =  fixed4(0,1,0,1);
                fixed4 yellow = fixed4(1,1,0,1);
                fixed4 orange = fixed4(1,0.5,0,1);
                fixed4 red =    fixed4(1,0,0,1);

                // All channels have the same value
                float t = pixel.r;
                float exp = 6.0f;

                if (t <= 0.2)
                    // Iterpolates between the two colors. The lower t is,
                    // the more the final colour will resemble the first one.
                    return lerp(blue, cyan, pow(t / 0.2f, exp)); 
                else if (t <= 0.4)
                    return lerp(cyan, green, pow((t - 0.2f) / 0.2f, exp));
                else if (t <= 0.6)
                    return lerp(green, yellow, pow((t - 0.4f) / 0.2f, exp));
                else if (t <= 0.75)
                    return lerp(yellow, orange, pow((t - 0.6f) / 0.2f, exp));
                else
                    return lerp(orange, red, pow((t - 0.75f) / 0.2f, exp));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 background = tex2D(_MainTex, i.uv);
                fixed4 overlay = tex2D(_SecondTex, i.uv);

                overlay = grayToColorMap(overlay);
                overlay.a = 1;

                fixed4 col = lerp(background, overlay, 0.25);
                return col;
            }


            ENDCG
        }
    }
}

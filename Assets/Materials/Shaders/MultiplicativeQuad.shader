Shader "Custom/MultiplicativeQuad"
{
    //show values to edit in inspector
    Properties
    {
       [HDR] _Color("Additive_colour", Color) = (0, 0, 0, 1)
        _MainTex("Texture", 2D) = "white" {}
        _Texture("Tex2", 2D) = "white" {}
    }

    SubShader
    {
        //the material is completely non-transparent and is rendered at the same time as the other opaque geometry
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent"
        }

        //This fixes the transparency problem 
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            //include useful shader functions
            #include "UnityCG.cginc"

            //define vertex and fragment shader functions
            #pragma vertex vert
            #pragma fragment frag

            //texture and transforms of the texture
            sampler2D _MainTex;
            sampler2D _Texture;
            float4 _MainTex_ST;

            //tint of the texture
            fixed4 _Color;

            //the mesh data thats read by the vertex shader
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            //the data thats passed from the vertex to the fragment shader and interpolated by the rasterizer
            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            //the vertex shader function
            v2f vert(appdata v)
            {
                v2f o;
                //convert the vertex positions from object space to clip space so they can be rendered correctly
                o.position = UnityObjectToClipPos(v.vertex);
                //apply the texture transforms to the UV coordinates and pass them to the v2f struct
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            //the fragment shader function
            fixed4 frag(v2f i) : SV_TARGET
            {
                //read the texture color at the uv coordinate
                fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col2 = tex2D(_Texture, i.uv);
                col *= col2;
                //multiply the texture color and tint color
                col *= _Color;
                //return the final color to be drawn on screen
                return col;
            }
            ENDCG
        }
    }
    Fallback "VertexLit"
}
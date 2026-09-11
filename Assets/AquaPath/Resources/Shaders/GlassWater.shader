Shader "AquaPath/GlassWater"
{
    Properties
    {
        [PerRendererData] _MainTex ("Water texture", 2D) = "white" {}
        _DistanceTex ("Entry distance channels", 2D) = "white" {}
        _Fill ("Fill", Range(0,1)) = 0
        _Entry ("Entry channel", Float) = 0
        _Seed ("Bubble variation", Float) = 0
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float4 world:TEXCOORD1; };
            sampler2D _MainTex, _DistanceTex;
            float4 _Color, _ClipRect;
            float _Fill, _Entry, _Seed;
            v2f vert(appdata v) { v2f o; o.world=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 col=tex2D(_MainTex,i.uv)*i.color;
                float4 channels=tex2D(_DistanceTex,i.uv);
                float distance=_Entry<.5?channels.r:_Entry<1.5?channels.g:_Entry<2.5?channels.b:channels.a;
                float wave=sin(i.uv.x*42+_Time.y*3.1+_Seed)*.006+sin(i.uv.y*33-_Time.y*2)*.003;
                float liquid=smoothstep(distance-.015,distance+.012,_Fill+wave);
                liquid*=smoothstep(0,.035,_Fill);
                float meniscus=exp(-pow((distance-_Fill-wave)/.014,2))*step(.04,_Fill)*step(_Fill,.97);
                col.rgb=lerp(col.rgb,float3(.59,.95,1),meniscus*.85);
                // Small translucent bubbles have a dark refracted underside and a white crescent.
                float2 bubbleUV=i.uv*float2(8,7);
                bubbleUV.y-= _Time.y*.18;
                float2 cell=floor(bubbleUV);
                float hash=frac(sin(dot(cell,float2(127.1,311.7))+_Seed)*43758.5453);
                float2 center=float2(.2+.6*hash,.2+.6*frac(hash*13.7));
                float2 bp=frac(bubbleUV)-center;
                float radius=.06+.085*frac(hash*7.23);
                float ring=exp(-pow((length(bp)-radius)/.019,2))*step(.38,hash);
                float glint=saturate(dot(normalize(bp+float2(.0001,0)),normalize(float2(-.6,.8)))*.7+.15);
                col.rgb=lerp(col.rgb,float3(.72,.98,1),ring*glint*.8);
                col.rgb*=1-ring*(1-glint)*.2;
                col.a*=liquid;
                #ifdef UNITY_UI_CLIP_RECT
                col.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a-.001);
                #endif
                return col;
            }
            ENDCG
        }
    }
}

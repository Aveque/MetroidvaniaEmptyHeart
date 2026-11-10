Shader "EmptyHeart/ParallaxFeather"
{
 Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _EdgeFade("Edge overlap",Float)=.15 _DstBlend("Destination blend",Float)=10 }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
 Cull Off ZWrite Off Blend One [_DstBlend]
 Pass { Tags {"LightMode"="Universal2D"}
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex;fixed4 _Color;float _EdgeFade;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target {fixed4 c=tex2D(_MainTex,i.uv)*i.color;float2 lower=smoothstep(0,_EdgeFade,i.uv),upper=smoothstep(0,_EdgeFade,1-i.uv);c.a*=lower.x*lower.y*upper.x*upper.y;c.rgb*=c.a;return c;}
 ENDCG
 }
 }
}


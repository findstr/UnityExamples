Shader "Unlit/Ripple"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _NormalTex("NormalTex", 2D) = "white" {}
        _RippleTex("_RippleTex", 2D) = "white" {}
        _Metallic("_Metallic", Range(0,1)) = 0.5
        _Smoothness("_Smoothness", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry"}
        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define UNITY_BRDF_GGX 1
            #define PI 3.141592653

			#include "AutoLight.cginc"
			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "UnityPBSLighting.cginc"
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent: TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 TtoW0: TEXCOORD1;
                float4 TtoW1: TEXCOORD2;
                float4 TtoW2: TEXCOORD3;
                float3 normal: TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _NormalTex;
            sampler2D _RippleTex;
            float4 _MainTex_ST;
            float _Metallic;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				float3 worldNormal = UnityObjectToWorldNormal(float3(0,1,0));
				float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;

				o.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
				o.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
				o.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
 
                o.normal = v.normal;
                
                return o;
            }

            fixed3 blend_normal(fixed3 n1, fixed3 n2) {
				return half3(n1.xy + n2.xy, n1.z * n2.z);
			}

            float3 ComputeRipple(float2 uv)
            {
                float3 normal;
                float t = frac(_Time.y);
                float4 ripple = tex2D(_RippleTex, uv);
                float dropFrac = frac(ripple.a + t);
                float timeFrac = dropFrac - 1.0 + ripple.r;
                float final = (1 - saturate(dropFrac)) * sin(clamp(timeFrac * 9.0, 0.0, 4.0) * PI);
                ripple.yz = ripple.yz * 2.0 - 1.0;
                normal.xy = ripple.yz * final;
                normal.z =  sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                return normal;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float3 ripple = ComputeRipple(i.uv * 5);
                fixed3 normal = UnpackNormal(tex2D(_NormalTex, i.uv));
                normal = blend_normal(normal, ripple);

				fixed3 worldPos = fixed3(i.TtoW0.w, i.TtoW1.w, i.TtoW2.w);
				fixed3 worldNormal = normalize(fixed3(dot(i.TtoW0.xyz, normal),dot(i.TtoW1.xyz, normal),dot(i.TtoW2.xyz, normal)));
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(worldPos));
				fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                fixed4 diff = tex2D(_MainTex, i.uv);

                fixed3 reflDir = reflect(-worldViewDir, worldNormal);
                half3 specColor;
                half oneMinusReflectivity;
                fixed3 albedo = DiffuseAndSpecularFromMetallic(diff.rgb, _Metallic, specColor, oneMinusReflectivity);
            
                UnityLight DirectLight;
                DirectLight.dir = worldLightDir;
                DirectLight.color = _LightColor0.xyz;

                UnityIndirect IndirectLight;
                IndirectLight.diffuse = unity_AmbientSky.rgb;
                IndirectLight.specular = fixed3(0, 0, 0);
                
                half3 result = UNITY_BRDF_PBS(albedo, specColor, oneMinusReflectivity, _Smoothness, worldNormal, worldViewDir, DirectLight, IndirectLight);
                return fixed4(result, diff.a);
            }
            ENDCG
        }
    }
}

Shader "TerrainAtlas"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_IndexTex("Index Texture", 2D) = "white" {}
		_BlendTex("Blend Texture", 2D) = "white" {}
		_tilling13("tilling13",Vector)=(1,1,1,1)
		_tilling46("tilling46",Vector)=(1,1,1,1)
		_tilling79("tilling79",Vector)=(1,1,1,1)
		_distance("distance",Float)=15
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			
			#define threeOne 1.0/3

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 worldPos : TEXCOORD2;
				float3 worldNormal : TEXCOORD3;

			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			sampler2D _IndexTex;
			float4 _IndexTex_ST;
			
			sampler2D _BlendTex;
			float3 _tilling13;
			float3 _tilling46;
			float3 _tilling79;

			float modeFunction(float ori, float modFactor)
			{
				return ori - modFactor * floor(ori / modFactor);
			}

			half4 GetColorByIndex(float index, float lodLevel, float2 worldPos)
			{
				float2 columnAndRow;
				columnAndRow.x = (index % 3.0);
				columnAndRow.y = floor((float((index / 3.0))) % 3.0);

				float4 curUV;
				float3x3 m={_tilling13.xyz,_tilling46.xyz,_tilling79.xyz};
				float scale=m[columnAndRow.y][columnAndRow.x];
				
				curUV.xy = (columnAndRow * threeOne) +float2(threeOne,threeOne)*4/340+ frac(worldPos/scale)*threeOne*332/340;
				curUV.w = 0;
				
				
				return tex2Dlod(_MainTex, curUV);
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv.zw = TRANSFORM_TEX(v.uv, _IndexTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);

				//记录观察空间的Z值
				o.worldPos.w = UnityObjectToViewPos(v.vertex).z;

				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//half lodLevel = min(-i.worldPos.w * 0.008, 3);
				half lodLevel = 0;

				//将贴图中被压缩到0,1之间的Index还原
				float4 indexRGB = tex2D (_IndexTex, i.uv.zw);
				float indexLayer1 = floor((indexRGB.r * 8) + 0.3f);
				float indexLayer2 = floor((indexRGB.g * 8) + 0.3f);
				float indexLayer3 = floor((indexRGB.b * 8) + 0.3f);

				//利用Index取得具体的贴图位置
				float4 colorLayer1 = GetColorByIndex(indexLayer1, lodLevel, i.worldPos.xz);
				float4 colorLayer2 = GetColorByIndex(indexLayer2, lodLevel, i.worldPos.xz);
				float4 colorLayer3 = GetColorByIndex(indexLayer3, lodLevel, i.worldPos.xz);

				//混合因子，其中r通道为第一层贴图所占权重，g通道为第二层贴图所占权重，b通道为第三层贴图所占权重
				float3 blend = tex2D (_BlendTex, i.uv.xy).rgb;
				half4 albedo = colorLayer1 * blend.r + colorLayer2 * blend.g + colorLayer3 * blend.b;

				//Lambert 光照模型
				float3 lightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				half NoL = saturate(dot(normalize(i.worldNormal), lightDir));
				half4 diffuseColor = _LightColor0 * NoL * albedo;
				return diffuseColor;
			}
			ENDCG
		}
	}
}

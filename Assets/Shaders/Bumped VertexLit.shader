Shader "Custom/Bumped VertexLit Full"
{
    Properties
    {
        // Основные параметры как в стандартном материале
        _Color ("Main Color", Color) = (1,1,1,1)
        _SpecularColor ("Spec Color", Color) = (0.5,0.5,0.5,1)
        _Emission ("Emissive Color", Color) = (0,0,0,0)
        _Shininess ("Shininess", Range(0.01, 1)) = 0.7
        
        // Текстуры
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        
        // Параметры бликов (опционально)
        _SpecularPower ("Specular Power", Range(1, 128)) = 20
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "LightMode" = "Vertex" }
        LOD 200
        
        Pass
        {
            Name "VertexLit"
            Tags { "LightMode" = "Vertex" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uvBump : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD3;
                float3 worldTangent : TEXCOORD4;
                float3 worldBinormal : TEXCOORD5;
                float3 worldPos : TEXCOORD6;
                fixed3 ambient : COLOR0;
                fixed3 vertexLight : COLOR1;
            };
            
            // Свойства материала (имена не конфликтуют с Lighting.cginc)
            sampler2D _MainTex;
            sampler2D _BumpMap;
            float4 _MainTex_ST;
            float4 _BumpMap_ST;
            fixed4 _Color;
            fixed4 _SpecularColor;  // Переименовано с _SpecColor
            fixed4 _Emission;
            float _Shininess;
            float _SpecularPower;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Позиция в клип пространстве
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // UV координаты
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uvBump = TRANSFORM_TEX(v.uv, _BumpMap);
                
                // Мировые векторы для TBN матрицы
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
                
                o.worldNormal = worldNormal;
                o.worldTangent = worldTangent;
                o.worldBinormal = worldBinormal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // ⚡ Вершинное освещение (окружение + 4 источника)
                o.ambient = ShadeSH9(float4(worldNormal, 1));
                o.vertexLight = ShadeVertexLights(v.vertex, v.normal);
                
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Получаем нормаль из карты бампа
                fixed4 bump = tex2D(_BumpMap, i.uvBump);
                fixed3 tangentNormal = UnpackNormal(bump);
                
                // TBN матрица для перевода в мировое пространство
                float3x3 TBN = float3x3(i.worldTangent, i.worldBinormal, i.worldNormal);
                float3 worldNormal = normalize(mul(tangentNormal, TBN));
                
                // Основная текстура
                fixed4 albedo = tex2D(_MainTex, i.uv) * _Color;
                
                // Позиция источника света (первый направленный)
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = max(0, dot(worldNormal, lightDir));
                
                // Вектор взгляда для бликов
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflectDir = reflect(-lightDir, worldNormal);
                float spec = pow(max(0, dot(viewDir, reflectDir)), _SpecularPower) * _Shininess;
                
                // Диффузное освещение
                fixed3 diffuse = ndotl * _LightColor0.rgb;
                
                // Спекулярное освещение (блики) - используем переименованную переменную
                fixed3 specular = spec * _SpecularColor.rgb * _LightColor0.rgb;
                
                // Собираем всё освещение
                fixed3 lighting = i.ambient + i.vertexLight + diffuse + specular;
                
                // Финальный цвет с эмиссией
                fixed3 final = albedo.rgb * lighting + _Emission.rgb;
                
                UNITY_APPLY_FOG(i.fogCoord, final);
                return fixed4(final, albedo.a);
            }
            ENDCG
        }
    }
    
    Fallback "VertexLit"
}
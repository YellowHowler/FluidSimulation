Shader "Custom/Particle"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="2.0"}
        LOD 300

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types  
        #pragma surface surf Standard vertex:vert fullforwardshadows
        #pragma instancing_options procedural:setup

        // Use shader model 3.0 target, to get nicer looking lighting 
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        float _Radius;
        float3 _Position;
        fixed4 _Color;

        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            struct Particle
            {
                int ind;
                float density;
                float pressure;
                float3 force;
                float3 velocity;
                float3 normal;
                float3 position;
            };

            StructuredBuffer<Particle> particleBuffer;
        #endif

        UNITY_INSTANCING_BUFFER_START(Props)
            
        UNITY_INSTANCING_BUFFER_END(Props)

        void setup()
        {
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                Particle particle = particleBuffer[unity_InstanceID];
                _Position = particle.position;
            #endif
        }

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.vertex.xyz *= _Radius;
                v.vertex.xyz += _Position;
            #endif
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = _Color.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

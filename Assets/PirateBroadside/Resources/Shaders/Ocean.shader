Shader "PirateBroadside/Ocean"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.03, 0.72, 0.76, 1)
        _DeepColor ("Deep Color", Color) = (0.01, 0.16, 0.31, 1)
        _FoamColor ("Foam Color", Color) = (0.75, 0.96, 1, 1)
        _WaveHeight ("Wave Height", Range(0, 2)) = 0.62
        _WaveScale ("Wave Scale", Range(0.01, 0.2)) = 0.065
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 1.15
        _Smoothness ("Smoothness", Range(0, 1)) = 0.88
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250

        CGPROGRAM
        #pragma surface surf Standard vertex:vert addshadow
        #pragma target 3.0

        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        float _WaveHeight;
        float _WaveScale;
        float _WaveSpeed;
        float _Smoothness;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float3 viewDir;
        };

        void vert(inout appdata_full v)
        {
            float2 p = v.vertex.xz;
            float t = _Time.y * _WaveSpeed;
            float waveA = sin(p.x * _WaveScale + t);
            float waveB = sin((p.x * 0.57 + p.y * 0.83) * _WaveScale * 1.7 - t * 0.72);
            float waveC = cos((p.x * -0.42 + p.y) * _WaveScale * 0.88 + t * 0.46);
            v.vertex.y += (waveA * 0.50 + waveB * 0.28 + waveC * 0.22) * _WaveHeight;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal))), 3.0);
            float waveBand = sin((IN.worldPos.x + IN.worldPos.z) * 0.18 + _Time.y * 1.7) * 0.5 + 0.5;
            float3 water = lerp(_ShallowColor.rgb, _DeepColor.rgb, saturate(fresnel * 0.72 + 0.16));
            float foam = smoothstep(0.86, 1.0, waveBand) * (1.0 - fresnel) * 0.15;
            o.Albedo = lerp(water, _FoamColor.rgb, foam);
            o.Metallic = 0.08;
            o.Smoothness = _Smoothness;
            o.Emission = _ShallowColor.rgb * 0.035;
            o.Alpha = 1.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

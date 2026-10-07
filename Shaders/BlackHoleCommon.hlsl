#ifndef BLACKHOLE_COMMON_INCLUDED
#define BLACKHOLE_COMMON_INCLUDED

#if defined(SHADER_API_GLES3) || defined(SHADER_API_WEBGPU)
    #define MAX_STEPS 600
#else
    #define MAX_STEPS 700
#endif

static const float WIND_PERIOD = 40.0;

TEXTURECUBE(_SkyTex);
TEXTURE2D(_BlackbodyLUT);
SamplerState sampler_linear_clamp;

float2 _BlackbodyRange; // (min kelvin, max kelvin) of the LUT, log-spaced

float3 _CamPos;
float3 _CamForward;
float3 _CamRight;
float3 _CamUp;
float2 _TanHalfFov;

float3 _BlackHolePos;
float _Rs;
float _Spin;
float _BHTime;
float _NoiseScale;
float _DiskDensity;
float _TwistIntensity;
float _TempMultiplier;
float _DiskThickness;
float _DiskOuterRadius;
float _DiskExposure;
float _EscapeRadius;
float _SkyIntensity;
float _SkyRotation;
float4 _BaseColor;

float3 hash33(float3 p)
{
    p = frac(p * float3(443.8975, 397.2973, 491.1871));
    p += dot(p, p.yxz + 19.19);
    return frac(float3(p.x * p.y, p.y * p.z, p.z * p.x)) * 2.0 - 1.0;
}

float gradient_noise(float3 x)
{
    float3 p = floor(x);
    float3 f = frac(x);
    float3 u = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(
            lerp(dot(hash33(p + float3(0, 0, 0)), f - float3(0, 0, 0)),
                 dot(hash33(p + float3(1, 0, 0)), f - float3(1, 0, 0)), u.x),
            lerp(dot(hash33(p + float3(0, 1, 0)), f - float3(0, 1, 0)),
                 dot(hash33(p + float3(1, 1, 0)), f - float3(1, 1, 0)), u.x),
            u.y),
        lerp(
            lerp(dot(hash33(p + float3(0, 0, 1)), f - float3(0, 0, 1)),
                 dot(hash33(p + float3(1, 0, 1)), f - float3(1, 0, 1)), u.x),
            lerp(dot(hash33(p + float3(0, 1, 1)), f - float3(0, 1, 1)),
                 dot(hash33(p + float3(1, 1, 1)), f - float3(1, 1, 1)), u.x),
            u.y),
        u.z);
}

float fbm(float3 p)
{
    float f = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 4; i++)
    {
        f += amp * gradient_noise(p);
        p *= 2.0;
        amp *= 0.5;
    }
    return f;
}

float disk_noise(float2 xz, float pos_y, float angle, float seed)
{
    float s, c;
    sincos(angle, s, c);
    
    float2 spiral_xz = float2(xz.x * c - xz.y * s, xz.x * s + xz.y * c);
    float3 noisePos = float3(spiral_xz.x, pos_y * 10.0, spiral_xz.y) * _NoiseScale;
    noisePos.y += seed * 7.31;
    
    return fbm(noisePos) * 0.5 + 0.5;
}

float3 BH_RayDir(float2 ndc)
{
    return normalize(_CamForward
        + _CamRight * (ndc.x * _TanHalfFov.x)
        + _CamUp    * (ndc.y * _TanHalfFov.y));
}

float3 BH_Render(float3 pos, float3 vel)
{
    bool hit = false;
    float3 diskLight = 0;
    float diskAlpha = 0;
    
    float a_phys = min(abs(_Spin), 0.4999 * _Rs);
    float r_horizon = 0.5 * _Rs + sqrt(max(0.25 * _Rs * _Rs - a_phys * a_phys, 0.0));
    
    float M = 0.5 * _Rs;
    float sqrtM = sqrt(M);
    float sn = a_phys / M;
    float z1 = 1.0 + pow(max(1.0 - sn * sn, 0.0), 1.0 / 3.0) *
        (pow(max(1.0 + sn, 0.0), 1.0 / 3.0) + pow(max(1.0 - sn, 0.0), 1.0 / 3.0));
    float z2 = sqrt(max(3.0 * sn * sn + z1 * z1, 0.0));
    float r_isco = M * (3.0 + z2 - sqrt(max((3.0 - z1) * (3.0 + z1 + 2.0 * z2), 0.0)));

    float outerR = _DiskOuterRadius * _Rs;
    float b = cross(pos - _BlackHolePos, vel).y;
    
    float cycle = _BHTime / WIND_PERIOD;
    
    float phase_a = frac(cycle);
    float phase_b = frac(cycle + 0.5);
    
    float weight_a = 1 - abs(2.0 * phase_a - 1.0);
    float weight_b = 1.0 - weight_a;
    
    float seed_a = floor(cycle) * 2.0;
    float seed_b = floor(cycle + 0.5) * 2.0 + 1.0;

    [loop]
    for (int i = 0; i < MAX_STEPS; i++)
    {
        float3 r = pos - _BlackHolePos;
        float r_sq = dot(r, r);
        float r1 = sqrt(r_sq);

        if (r1 < r_horizon) { hit = true; break; }
        if (r1 > _EscapeRadius) break;
        
        float dt = min(0.04 * max(r1 - r_horizon, 0.01 * _Rs), max(0.25, 0.02 * r1));
        if (r1 < (_DiskOuterRadius + 2.0) * _Rs)
        {
            dt = min(dt, max(abs(pos.y - _BlackHolePos.y) * 0.8, 0.005));
        }
        
        float3 h_vec = cross(r, vel);
        float h_sq = dot(h_vec, h_vec);
        float3 grav = -(1.5 * _Rs / (r_sq * r_sq * r1)) * h_sq * r;
        
        float3 J = float3(0, 1, 0) * (M * a_phys);
        float jdotr = dot(J, r);
        float r3 = r_sq * r1;
        float3 drag = (2.0 / r3) * (-3.0 * (jdotr / r_sq) * cross(r, vel) + cross(J, vel));

        vel += (grav + drag) * dt;
        vel = normalize(vel);
        pos += vel * dt;

        if (diskAlpha >= 0.95) continue;

        float3 rd = pos - _BlackHolePos;
        float disk_r = length(rd.xz);

        if (disk_r > r_isco && disk_r < outerR)
        {
            float h = _DiskThickness * disk_r;
            float pos_y = rd.y;
            float vertical = exp(-0.5 * pos_y * pos_y / (h * h));

            if (vertical > 0.001)
            {
                float x = saturate((disk_r - r_isco) / (outerR - r_isco));
                float base_density = smoothstep(1.0, 0.2, x);
                
                float r_three_halves = pow(disk_r, 1.5);
                float omega_k = sqrtM / (r_three_halves + a_phys * sqrtM);
                
                float twist = _TwistIntensity * log(max(disk_r, 0.01));
                float layer_a = disk_noise(rd.xz, pos_y, twist - omega_k * phase_a * WIND_PERIOD, seed_a);
                float layer_b = disk_noise(rd.xz, pos_y, twist - omega_k * phase_b * WIND_PERIOD, seed_b);
                float raw_noise = layer_a * weight_a + layer_b * weight_b;
                
                float cloud_mask = pow(smoothstep(0.2, 0.8, raw_noise), 2.0);
                float final_density = base_density * cloud_mask;

                float u_t = (r_three_halves + a_phys * sqrtM) / (pow(disk_r, 0.75) * sqrt(max(r_three_halves - 3.0 * M * sqrt(disk_r) + 2.0 * a_phys * sqrtM, 1e-6)));
                float g = 1.0 / (u_t * max(1.0 - omega_k * b, 1e-4));

                // Novikov–Thorne profile, normalized so its peak (at r ≈ 1.36 r_isco) is 1
                float temp_profile = pow(r_isco / disk_r, 0.75) * pow(max(1.0 - sqrt(r_isco / disk_r), 0.0), 0.25) / 0.488;
                float rest_temp = _TempMultiplier * temp_profile;
                float obs_temp = rest_temp * g;

                float lut_temp = max(obs_temp, _BlackbodyRange.x);
                float lut_u = log(lut_temp / _BlackbodyRange.x) / log(_BlackbodyRange.y / _BlackbodyRange.x);
                float3 emission = SAMPLE_TEXTURE2D_LOD(_BlackbodyLUT, sampler_linear_clamp, float2(lut_u, 0.5), 0).rgb * _BaseColor.rgb;
                
                emission *= _DiskExposure;

                float stepAlpha = 1.0 - exp(-(final_density * vertical * _DiskDensity * 10.0) * dt);
                diskLight += emission * stepAlpha * (1.0 - diskAlpha);
                diskAlpha += stepAlpha * (1.0 - diskAlpha);
            }
        }
    }
    
    float sr, cr;
    sincos(radians(_SkyRotation), sr, cr);
    
    float3 skyDir = normalize(vel);
    skyDir = float3(cr * skyDir.x + sr * skyDir.z, skyDir.y, -sr * skyDir.x + cr * skyDir.z);
    
    float3 bg = SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_linear_clamp, skyDir, 0).rgb * _SkyIntensity;
    float3 col = hit ? float3(0, 0, 0) : bg;
    
    col = col * (1.0 - diskAlpha) + diskLight;
    
    return col;
}

#endif

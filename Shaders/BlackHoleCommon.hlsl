#ifndef BLACKHOLE_COMMON_INCLUDED
#define BLACKHOLE_COMMON_INCLUDED

#if defined(SHADER_API_GLES3) || defined(SHADER_API_WEBGPU)
    #define MAX_STEPS 600
#else
    #define MAX_STEPS 700
#endif

TEXTURECUBE(_SkyTex);
SamplerState sampler_linear_clamp;

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
float _EvolutionSpeed;
float _TwistIntensity;
float _TempMultiplier;
float _DiskThickness;
float _DiskOuterRadius;
float _BeamingPower;
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

float3 kelvin_to_rgb(float kelvin)
{
    if (kelvin < 400.0) return float3(0.0, 0.0, 0.0);
    float teff = (kelvin - 6500.0) / (6500.0 * kelvin * 2.2);
    float3 col;
    col.r = exp(2.05539304e4 * teff);
    col.g = exp(2.63463675e4 * teff);
    col.b = exp(3.30145739e4 * teff);
    float norm = 1.0 / max(max(1.5 * col.r, col.g), col.b);
    if (kelvin < 1000.0) norm *= (kelvin - 400.0) / 600.0;
    return col * norm;
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
                float base_density = smoothstep(0.0, 0.05, x) * smoothstep(1.0, 0.2, x);

                float twist = _TwistIntensity * log(max(disk_r, 0.01));
                float s, c;
                sincos(twist, s, c);
                float2 spiral_xz = float2(rd.x * c - rd.z * s, rd.x * s + rd.z * c);

                float drift = _BHTime * _EvolutionSpeed * 2.0;
                float3 noisePos = float3(spiral_xz.x - drift, pos_y * 10.0, spiral_xz.y - drift) * _NoiseScale;
                float raw_noise = fbm(noisePos) * 0.5 + 0.5;
                float cloud_mask = pow(smoothstep(0.2, 0.8, raw_noise), 2.0);
                float final_density = base_density * cloud_mask;

                float omega_k = sqrtM / (pow(disk_r, 1.5) + a_phys * sqrtM);
                float v_orb = min(omega_k * disk_r, 0.95);

                float2 orbital = normalize(float2(-rd.z, rd.x));
                float ddot = dot(orbital, normalize(vel.xz));
                float gamma = 1.0 / sqrt(max(1.0 - v_orb * v_orb, 1e-6));
                float freq_ratio = 1.0 / (gamma * (1.0 - v_orb * ddot));

                float grav_z = sqrt(max(1.0 - _Rs / disk_r, 0.0));

                float temp_profile = pow(max(r_isco / disk_r, 0.0), 0.75);
                float rest_temp = _TempMultiplier * temp_profile;
                float obs_temp = rest_temp * freq_ratio * grav_z;

                float3 emission = kelvin_to_rgb(obs_temp) * _BaseColor.rgb;

                float beaming = pow(max(freq_ratio, 0.0), _BeamingPower);
                emission *= beaming * 5.0;

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

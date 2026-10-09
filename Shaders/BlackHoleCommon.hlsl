#ifndef BLACKHOLE_COMMON_INCLUDED
#define BLACKHOLE_COMMON_INCLUDED

#if defined(SHADER_API_GLES3) || defined(SHADER_API_WEBGPU)
#define MAX_STEPS 600
#else
#define MAX_STEPS 1000
#endif

static const float WIND_PERIOD = 40.0;

TEXTURECUBE(_SkyTex);
TEXTURE2D(_BlackbodyLUT);
SamplerState sampler_linear_clamp;

float2 _BlackbodyRange;

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
float _DopplerStrength;
float _TemperatureFalloff;
float _TurbulenceContrast;
float _FilamentSharpness;
float _TurbulenceWarp;
float _OrbitalStretch;
float _EdgeFraying;
float _PlungingGas;
float _PlungeGlow;
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

float ridged_fbm(float3 p)
{
    float f = 0.0;
    float amp = 0.5;

    for (int i = 0; i < 4; i++)
    {
        float n = 1.0 - abs(gradient_noise(p));
        f += amp * n * n;
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
    noisePos.y += length(xz) * _OrbitalStretch * _NoiseScale;

    float3 warp = float3(gradient_noise(noisePos + float3(17.1, 3.7, 0.0)), 0.0,
                         gradient_noise(noisePos + float3(-5.3, 11.9, 0.0)));
    noisePos += _TurbulenceWarp * warp;

    float soft = fbm(noisePos) * 0.5 + 0.5;
    float sharp = ridged_fbm(noisePos);

    return lerp(soft, sharp, _FilamentSharpness);
}

float3 BH_RayDir(float2 ndc)
{
    return normalize(_CamForward
        + _CamRight * (ndc.x * _TanHalfFov.x)
        + _CamUp * (ndc.y * _TanHalfFov.y));
}

float3 unity_to_kerr(float3 v)
{
    return float3(v.x, v.z, -v.y);
}

float3 kerr_to_unity(float3 v)
{
    return float3(v.x, -v.z, v.y);
}

void ks_metric(float3 x, float a, float M, out float r, out float f, out float3 l)
{
    float rho2 = dot(x, x);
    float rho2_minus_a2 = rho2 - a * a;

    r = sqrt(0.5 * (rho2_minus_a2 + sqrt(rho2_minus_a2 * rho2_minus_a2 + 4.0 * a * a * x.z * x.z)));
    float r2 = r * r;
    float a2 = a * a;

    f = (2 * M * r * r2) / (r2 * r2 + a2 * x.z * x.z);

    float lx = (r * x.x + a * x.y) / (r2 + a2);
    float ly = (r * x.y - a * x.x) / (r2 + a2);
    float lz = x.z / max(r, 1e-6);
    l = float3(lx, ly, lz);
}

void ks_deriv(float3 x, float3 p, float a, float M, out float3 dx, out float3 dp)
{
    float r, f;
    float3 l;
    ks_metric(x, a, M, r, f, l);
    float k = 1 + dot(l, p);

    dx = p - f * l * k;

    float r2 = r * r;
    float a2 = a * a;
    float w = r2 + a2;
    float N = r2 * r2 + a2 * x.z * x.z;
    float D = r * (2.0 * r2 - dot(x, x) + a2);

    float3 grad_r = float3(x.x * r2, x.y * r2, x.z * w) / D;
    float3 grad_lnf = (3.0 / r - 4 * r2 * r / N) * grad_r - float3(0, 0, 2.0 * a2 * x.z / N);

    float c = (x.x * p.x + x.y * p.y - 2 * r * (l.x * p.x + l.y * p.y)) / w - x.z * p.z / r2;
    float3 grad_k = c * grad_r
        + r / w * float3(p.x, p.y, 0.0)
        + a / w * float3(-p.y, p.x, 0.0)
        + float3(0, 0, p.z / r);

    float3 grad_S = f * k * (k * grad_lnf + 2.0 * grad_k);
    dp = 0.5 * grad_S;
}

void ks_rk4(inout float3 x, inout float3 p, float h, float a, float M)
{
    float3 dx1, dp1;
    ks_deriv(x, p, a, M, dx1, dp1);

    float3 x2 = x + 0.5 * h * dx1;
    float3 p2 = p + 0.5 * h * dp1;
    float3 dx2, dp2;
    ks_deriv(x2, p2, a, M, dx2, dp2);

    float3 x3 = x + 0.5 * h * dx2;
    float3 p3 = p + 0.5 * h * dp2;
    float3 dx3, dp3;
    ks_deriv(x3, p3, a, M, dx3, dp3);

    float3 x4 = x + h * dx3;
    float3 p4 = p + h * dp3;
    float3 dx4, dp4;
    ks_deriv(x4, p4, a, M, dx4, dp4);

    x += (h / 6.0) * (dx1 + 2.0 * dx2 + 2.0 * dx3 + dx4);
    p += (h / 6.0) * (dp1 + 2.0 * dp2 + 2.0 * dp3 + dp4);
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

    float cycle = _BHTime / WIND_PERIOD;

    float phase_a = frac(cycle);
    float phase_b = frac(cycle + 0.5);

    float weight_a = 1 - abs(2.0 * phase_a - 1.0);
    float weight_b = 1.0 - weight_a;

    float seed_a = floor(cycle) * 2.0;
    float seed_b = floor(cycle + 0.5) * 2.0 + 1.0;

    #if defined(BH_KERR)
    float a_ks = -a_phys;

    float3 kx = unity_to_kerr(pos - _BlackHolePos);
    float3 n = unity_to_kerr(vel);

    float kr0, f0;
    float3 l0;
    ks_metric(kx, a_ks, M, kr0, f0, l0);
    float r_now = kr0;

    float A = -1 - f0;
    float dotln = dot(l0, n);
    float B = 2 * f0 * dotln;
    float C = dot(n, n) - f0 * dotln * dotln;

    float disc = sqrt(max(B * B - 4.0 * A * C, 0.0));
    float pt = min((-B + disc) / (2.0 * A), (-B - disc) / (2.0 * A));

    float3 kp = n / -pt;
    float L_ks = kx.y * kp.x - kx.x * kp.y;
    #else
    float L_ks = cross(pos - _BlackHolePos, vel).y;
    float3 J = float3(0, -1, 0) * (M * a_phys);
    #endif

    [loop]
    for (int i = 0; i < MAX_STEPS; i++)
    {
        #if defined(BH_KERR)
        if (r_now < r_horizon)
        {
            hit = true;
            break;
        }

        if (r_now > _EscapeRadius)
        {
            break;
        }

        float ds = (r_now < 4.0 * M) ? 0.02 * r_now : 0.08 * r_now;
        if (r_now < (_DiskOuterRadius + 2.0) * _Rs)
        {
            ds = min(ds, max(abs(pos.y - _BlackHolePos.y) * 0.8, 0.3 * _DiskThickness * r_now));
        }

        float3 prev_pos = pos;
        ks_rk4(kx, kp, ds, a_ks, M);

        float f_tmp;
        float3 l_tmp;
        ks_metric(kx, a_ks, M, r_now, f_tmp, l_tmp);

        pos = _BlackHolePos + kerr_to_unity(kx);

        float dt = length(pos - prev_pos);
        vel = (pos - prev_pos) / max(dt, 1e-6);
        #else
        float3 r = pos - _BlackHolePos;
        float r_sq = dot(r, r);
        float r1 = sqrt(r_sq);

        if (r1 < r_horizon)
        {
            hit = true;
            break;
        }
        if (r1 > _EscapeRadius) break;

        float dt = min(0.04 * max(r1 - r_horizon, 0.01 * _Rs), max(0.25, 0.02 * r1));
        if (r1 < (_DiskOuterRadius + 2.0) * _Rs)
        {
            dt = min(dt, max(abs(pos.y - _BlackHolePos.y) * 0.8, 0.3 * _DiskThickness * r1));
        }

        float3 h_vec = cross(r, vel);
        float h_sq = dot(h_vec, h_vec);
        float3 grav = -(1.5 * _Rs / (r_sq * r_sq * r1)) * h_sq * r;

        float jdotr = dot(J, r);
        float r3 = r_sq * r1;
        float3 drag = (2.0 / r3) * (-3.0 * (jdotr / r_sq) * cross(r, vel) + cross(J, vel));

        vel += (grav + drag) * dt;
        vel = normalize(vel);
        pos += vel * dt;
        #endif

        if (diskAlpha >= 0.95) continue;

        float3 rd = pos - _BlackHolePos;
        #if defined(BH_KERR)
        float disk_r = r_now;
        #else
        float disk_r = length(rd.xz);
        #endif

        if (disk_r > r_horizon && disk_r < outerR)
        {
            bool plunging = disk_r < r_isco;
            float t_plunge = saturate((disk_r - r_horizon) / (r_isco - r_horizon));
            
            float h = _DiskThickness * disk_r;
            float pos_y = rd.y;
            float vertical = exp(-0.5 * pos_y * pos_y / (h * h));

            if (vertical > 0.001)
            {
                float x = saturate((disk_r - r_isco) / (outerR - r_isco));
                float base_density = smoothstep(1.0, 0.2, x);
                
                if (plunging)
                {
                    base_density = _PlungingGas * t_plunge * t_plunge;
                }
                
                float r_orb = max(disk_r, r_isco);
                float r_three_halves = pow(r_orb, 1.5);
                float omega_k = sqrtM / (r_three_halves + a_phys * sqrtM);

                float twist = _TwistIntensity * log(max(disk_r, 0.01));
                
                if (plunging)
                {
                    twist += 6.0 * (1.0 - t_plunge);
                }
                
                float u_t = (r_three_halves + a_phys * sqrtM) / (pow(r_orb, 0.75) * sqrt(
                            max(r_three_halves - 3.0 * M * sqrt(r_orb) + 2.0 * a_phys * sqrtM, 1e-6)));
                
                float v_in = 0;
                if (plunging)
                {
                    v_in = sqrt(2.0 * M / (3.0 * r_isco)) * pow(max(r_isco / disk_r - 1.0, 0), 1.5) / u_t;
                }
                
                float r_cyl = length(rd.xz);
                float2 xz_a = rd.xz * (r_cyl + v_in * phase_a * WIND_PERIOD) / r_cyl;
                float2 xz_b = rd.xz * (r_cyl + v_in * phase_b * WIND_PERIOD) / r_cyl;
                
                float layer_a = disk_noise(xz_a, pos_y, twist - omega_k * phase_a * WIND_PERIOD, seed_a);
                float layer_b = disk_noise(xz_b, pos_y, twist - omega_k * phase_b * WIND_PERIOD, seed_b);
                float raw_noise = layer_a * weight_a + layer_b * weight_b;
                
                float fray = _EdgeFraying * x * x;
                float lo = lerp(0.2, 0.7, fray);
                float hi = lerp(0.8, 0.9, fray);
                float cloud_mask = pow(smoothstep(lo, hi, raw_noise), 2.0);
                float final_density = base_density * cloud_mask;

                float g = 1.0 / (u_t * pow(max(1.0 - omega_k * L_ks, 1e-4), _DopplerStrength));

                // Novikov–Thorne profile, normalized so its peak (at r ≈ 1.36 r_isco) is 1
                float temp_profile = pow(r_isco / disk_r, 0.75) * pow(max(1.0 - sqrt(r_isco / disk_r), 0.0), 0.25) /
                    0.488;
                temp_profile = pow(temp_profile, max(_TemperatureFalloff, 0.01));
                
                if (plunging)
                {
                    temp_profile = _PlungeGlow * t_plunge;
                }

                float rest_temp = _TempMultiplier * temp_profile;
                rest_temp *= 1 + _TurbulenceContrast * (2.0 * raw_noise - 1.0);
                float obs_temp = rest_temp * g;

                float lut_temp = max(obs_temp, _BlackbodyRange.x);
                float lut_u = log(lut_temp / _BlackbodyRange.x) / log(_BlackbodyRange.y / _BlackbodyRange.x);
                float3 emission = SAMPLE_TEXTURE2D_LOD(_BlackbodyLUT, sampler_linear_clamp, float2(lut_u, 0.5), 0).rgb *
                    _BaseColor.rgb;

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

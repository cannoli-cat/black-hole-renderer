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
float _PixelAngle;

float3 _BlackHolePos;
float3 _ObserverVelocity;
float _ObserverMode;
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
float4 _HotSpot; // x = strength, y = orbit radius / ISCO, z = size / Rs
float4 _Jet; // x = brightness, y = speed (fraction of c), z = width / Rs, w = length / Rs
float _JetKnots;
float _JetTurbulence;
float4 _JetColor;
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

float erf_approx(float x)
{
    float t = 1.0 / (1.0 + 0.3275911 * abs(x));
    float y = 1.0 - ((((1.061405429 * t - 1.453152027) * t + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * exp(-x * x);
    return sign(x) * y;
}

float3 blackbody(float T)
{
    float lut_temp = max(T, _BlackbodyRange.x);
    float lut_u = log(lut_temp / _BlackbodyRange.x) / log(_BlackbodyRange.y / _BlackbodyRange.x);
    return SAMPLE_TEXTURE2D_LOD(_BlackbodyLUT, sampler_linear_clamp, float2(lut_u, 0.5), 0).rgb;
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

// fbm & ridged
void fbm_both(float3 p, float fp, out float soft, out float sharp)
{
    soft = 0.0;
    sharp = 0.0;
    float amp = 0.5;
    float freq = 1.0;

    for (int i = 0; i < 4; i++)
    {
        float w = 1.0 - smoothstep(0.25, 0.5, freq * fp);

        if (w > 0.0)
        {
            float g = gradient_noise(p);
            soft += amp * w * g;

            float n = 1.0 - abs(g);
            sharp += amp * lerp(0.74, n * n, w);
        }
        else
        {
            sharp += amp * 0.74;
        }

        p *= 2.0;
        freq *= 2.0;
        amp *= 0.5;
    }

    soft = soft * 0.5 + 0.5;
}

float disk_noise(float2 xz, float pos_y, float angle, float seed, float pixel_world)
{
    float s, c;
    sincos(angle, s, c);

    float fp = pixel_world * _NoiseScale * sqrt(1 + _OrbitalStretch * _OrbitalStretch);
    
    float2 spiral_xz = float2(xz.x * c - xz.y * s, xz.x * s + xz.y * c);
    float3 noisePos = float3(spiral_xz.x, pos_y * 10.0, spiral_xz.y) * _NoiseScale;
    noisePos.y += seed * 7.31;
    noisePos.y += length(xz) * _OrbitalStretch * _NoiseScale;
    
    if (_TurbulenceWarp > 0)
    {
        float3 warp = float3(gradient_noise(noisePos + float3(17.1, 3.7, 0.0)), 0.0,
                             gradient_noise(noisePos + float3(-5.3, 11.9, 0.0)));
        noisePos += _TurbulenceWarp * warp;
    }

    float soft, sharp;
    fbm_both(noisePos, fp, soft, sharp);

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

void ks_deriv(float3 x, float3 p, float a, float M, float E, out float3 dx, out float3 dp, out float dtdl)
{
    float r, f;
    float3 l;
    ks_metric(x, a, M, r, f, l);
    float k = E + dot(l, p);

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
    
    dtdl = E + f * k;
}

void ks_rk4(inout float3 x, inout float3 p, inout float t, float h, float a, float M, float E)
{
    float3 dx1, dp1;
    float dt1;
    ks_deriv(x, p, a, M, E, dx1, dp1, dt1);

    float3 x2 = x + 0.5 * h * dx1;
    float3 p2 = p + 0.5 * h * dp1;
    float3 dx2, dp2;
    float dt2;
    ks_deriv(x2, p2, a, M, E, dx2, dp2, dt2);

    float3 x3 = x + 0.5 * h * dx2;
    float3 p3 = p + 0.5 * h * dp2;
    float3 dx3, dp3;
    float dt3;
    ks_deriv(x3, p3, a, M, E, dx3, dp3, dt3);

    float3 x4 = x + h * dx3;
    float3 p4 = p + h * dp3;
    float3 dx4, dp4;
    float dt4;
    ks_deriv(x4, p4, a, M, E, dx4, dp4, dt4);

    x += (h / 6.0) * (dx1 + 2.0 * dx2 + 2.0 * dx3 + dx4);
    p += (h / 6.0) * (dp1 + 2.0 * dp2 + 2.0 * dp3 + dp4);
    t += (h / 6.0) * (dt1 + 2.0 * dt2 + 2.0 * dt3 + dt4);
}

float g_dot(float4 A, float4 B, float f, float3 l)
{
    float LA = A.w + dot(l, A.xyz);
    float LB = B.w + dot(l, B.xyz);
    
    return -A.w * B.w + dot(A.xyz, B.xyz) + f * LA * LB;
}

float3 fast_accel(float3 r, float3 v, float3 J)
{
    float r_sq = dot(r, r);
    float r1 = sqrt(r_sq);
    float r3 = r_sq * r1;

    float3 h_vec = cross(r, v);
    float h_sq = dot(h_vec, h_vec);
    float3 grav = -(1.5 * _Rs / (r_sq * r_sq * r1)) * h_sq * r;

    float jdotr = dot(J, r);
    float3 drag = (2.0 / r3) * (-3.0 * (jdotr / r_sq) * h_vec + cross(J, v));

    return grav + drag;
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

    float t_emit = _BHTime;
    float ray_len = 0;

    float3 beta = _ObserverVelocity;
    float3 r_hat = normalize(pos - _BlackHolePos);
    float3 phi_hat = cross(float3(0, -1, 0), r_hat);
    phi_hat /= max(length(phi_hat), 1e-4);

    #if defined(BH_KERR)
    float a_ks = a_phys;

    float3 kx = unity_to_kerr(pos - _BlackHolePos);

    float kr0, f0;
    float3 l0;
    ks_metric(kx, a_ks, M, kr0, f0, l0);
    float r_now = kr0;

    float r2 = kr0 * kr0;
    float a2 = a_ks * a_ks;

    float delta = r2 - 2.0 * M * kr0 + a2;
    float cos_t = kx.z / kr0;
    float sigma = r2 + a2 * cos_t * cos_t;
    float big_a = (r2 + a2) * (r2 + a2) - a2 * delta * (1.0 - cos_t * cos_t);
    float alpha = sqrt(delta * sigma / big_a);

    float sin_t = sqrt(max(1.0 - cos_t * cos_t, 0.0));
    float omega_orb = sqrtM / (pow(kr0, 1.5) + a_phys * sqrtM);
    float omega_drag = 2.0 * M * a_phys * kr0 / big_a;
    float varpi = sqrt(big_a) * sin_t / sqrt(sigma);
    float v_orb = (omega_orb - omega_drag) * varpi / alpha;
    float v_fall = sqrt(max(1.0 - alpha * alpha, 0.0));
    float r_photon = 2.0 * M * (1.0 + cos(2.0 / 3.0 * acos(-a_phys / M)));
    
    #else
    
    float r_c = length(pos - _BlackHolePos);
    float alpha = sqrt(1.0 - _Rs / r_c);
    float v_orb = sqrt(M / r_c) * length(r_hat.xz) / alpha;
    float v_fall = sqrt(_Rs / r_c);
    
    #endif
    
    int mode = (int)round(_ObserverMode);
    if (mode == 1) beta = min(v_orb, 0.99) * phi_hat;
    else if (mode == 2) beta = min(v_fall, 0.99) * -r_hat;
    
    #if defined(BH_KERR)
    bool rain = mode == 2 || kr0 < r_horizon * 1.001;
    if (rain) beta = 0;
    #endif

    float beta2 = dot(beta, beta);
    float gamma_l = 1.0 / sqrt(1.0 - beta2);
    float bd = dot(beta, vel);
    float doppler = 1.0 / (gamma_l * (1.0 - bd));

    float3 ab = (vel / gamma_l + beta * (gamma_l * bd / (1.0 + gamma_l) - 1.0)) / (1.0 - bd);
    vel = normalize(ab);

    #if defined(BH_KERR)
    float3 n = unity_to_kerr(vel);
    
    float3 grad_r = float3(kx.x * r2, kx.y * r2, kx.z * (r2 + a2)) / (kr0 * (2.0 * r2 - dot(kx, kx) + a2));
    float4 u;
    
    if (rain)
    {
        float S = sqrt(2.0 * M * kr0 * (r2 + a2));
        float3 u_cov = -(2.0 * M * kr0 / (2.0 * M * kr0 + S)) * grad_r;
        float Lu = 1.0 + dot(l0, u_cov);
        u = float4(u_cov, 1.0) - f0 * Lu * float4(l0, -1.0);
    }
    else 
    {
        float c_r = 2.0 * M * kr0 / delta;
        float lu = alpha * (1.0 + c_r * dot(l0, grad_r));
        u = float4(alpha * c_r * grad_r, alpha) - f0 * lu * float4(l0, -1.0);
    } 

    float4 e1 = float4(1, 0, 0, 0);
    e1 += g_dot(e1, u, f0, l0) * u;
    e1 /= sqrt(g_dot(e1, e1, f0, l0));
    
    float4 e2 = float4(0, 1, 0, 0);
    e2 += g_dot(e2, u, f0, l0) * u;
    e2 -= g_dot(e2, e1, f0, l0) * e1;
    e2 /= sqrt(g_dot(e2, e2, f0, l0));
    
    float4 e3 = float4(0, 0, 1, 0);
    e3 += g_dot(e3, u, f0, l0) * u;
    e3 -= g_dot(e3, e1, f0, l0) * e1;
    e3 -= g_dot(e3, e2, f0, l0) * e2;
    e3 /= sqrt(g_dot(e3, e3, f0, l0));

    float4 k = u - (n.x * e1 + n.y * e2 + n.z * e3);

    float Lk = k.w + dot(l0, k.xyz);
    float4 k_low = float4(k.xyz, -k.w) + f0 * Lk * float4(l0, 1.0);
    
    float E_ph = -k_low.w;

    float3 kp = k_low.xyz;
    float L_ks = (kx.x * kp.y - kx.y * kp.x) / E_ph;
    
    float E_cam = 1.0 / E_ph;
    doppler *= E_cam;
    #else
    float L_ks = cross(pos - _BlackHolePos, vel).y;
    float3 J = float3(0, -1, 0) * (M * a_phys);

    doppler /= sqrt(1.0 - _Rs / length(pos - _BlackHolePos));
    #endif
    
    float cached_noise = 0.5;
    float since_noise = 1e9;
    float noise_spacing = 0.1 / (_NoiseScale * sqrt(1.0 + _OrbitalStretch * _OrbitalStretch));
    
    float r_hs = _HotSpot.y * r_isco;
    float omega_hs = sqrtM / (pow(r_hs, 1.5) + a_phys * sqrtM);
    float sigma_hs = _HotSpot.z * _Rs;
    
    float3 jetLight = 0;
    
    [loop]
    for (int i = 0; i < MAX_STEPS; i++)
    {
        #if defined(BH_KERR)
        if (r_now > _EscapeRadius)
        {
            break;
        }

        float ds = (r_now < 4.0 * M) ? 0.02 * r_now : 0.08 * r_now;
        if (diskAlpha < 0.95 && r_now < (_DiskOuterRadius + 2.0) * _Rs)
        {
            ds = min(ds, max(abs(pos.y - _BlackHolePos.y) * 0.8, 0.3 * _DiskThickness * r_now));
        }
        
        if (_Jet.x > 0)
        {
            float jy0 = abs(pos.y - _BlackHolePos.y);
            float jw0 = _Jet.z * _Rs * sqrt(max(jy0, _Rs) / _Rs);
            if (length((pos - _BlackHolePos).xz) < 3.0 * jw0) ds = min(ds, 0.5 * jw0);
        }

        float3 prev_pos = pos;
        float r_before = r_now;
        ks_rk4(kx, kp, t_emit, -ds, a_ks, M, E_ph);

        float f_tmp;
        float3 l_tmp;
        ks_metric(kx, a_ks, M, r_now, f_tmp, l_tmp);
        
        if ((r_now < r_photon && r_now < r_before) || any(abs(kp) > 100.0))
        {
            hit = true;
            break;
        }

        pos = _BlackHolePos + kerr_to_unity(kx);

        float dt = length(pos - prev_pos);
        vel = (pos - prev_pos) / max(dt, 1e-6);
        
        ray_len += dt;
        #else
        float3 r = pos - _BlackHolePos;
        float r1 = length(r);

        if (r1 < r_horizon)
        {
            hit = true;
            break;
        }
        if (r1 > _EscapeRadius) break;

        float dt = (r1 < 4.0 * M) ? 0.02 * r1 : 0.08 * r1;
        if (diskAlpha < 0.95 && r1 < (_DiskOuterRadius + 2.0) * _Rs)
        {
            dt = min(dt, max(abs(pos.y - _BlackHolePos.y) * 0.8, 0.3 * _DiskThickness * r1));
        }
        
        if (_Jet.x > 0)
        {
            float jy0 = abs(pos.y - _BlackHolePos.y);
            float jw0 = _Jet.z * _Rs * sqrt(max(jy0, _Rs) / _Rs);
            if (length((pos - _BlackHolePos).xz) < 3.0 * jw0) dt = min(dt, 0.5 * jw0);
        }

        float3 prev_pos = pos;

        float3 a1 = fast_accel(pos - _BlackHolePos, vel, J);

        float3 p2 = pos + 0.5 * dt * vel;
        float3 v2 = normalize(vel + 0.5 * dt * a1);
        float3 a2 = fast_accel(p2 - _BlackHolePos, v2, J);

        float3 p3 = pos + 0.5 * dt * v2;
        float3 v3 = normalize(vel + 0.5 * dt * a2);
        float3 a3 = fast_accel(p3 - _BlackHolePos, v3, J);

        float3 p4 = pos + dt * v3;
        float3 v4 = normalize(vel + dt * a3);
        float3 a4 = fast_accel(p4 - _BlackHolePos, v4, J);

        pos += (dt / 6.0) * (vel + 2.0 * v2 + 2.0 * v3 + v4);
        vel = normalize(vel + (dt / 6.0) * (a1 + 2.0 * a2 + 2.0 * a3 + a4));

        ray_len += dt;
        t_emit -= dt * (1.0 + _Rs / r1);
        #endif
        
        if (_Jet.x > 0)
        {
            float jy = abs(pos.y - _BlackHolePos.y);
            float rho = length((pos - _BlackHolePos).xz);
            float jw = _Jet.z * _Rs * sqrt(max(jy, _Rs) / _Rs);
            float across = exp(-0.5 * rho * rho / (jw * jw));
            float along = _Rs / max(jy, _Rs);
            float ends = smoothstep(r_horizon, 2.0 * r_horizon, jy) * (1.0 - smoothstep(0.7, 1.0, jy / (_Jet.w * _Rs)));
            float j = _Jet.x * across * along * ends / jw;
            
            float beta_j = _Jet.y;
            float gamma_j = 1.0 / sqrt(1.0 - beta_j * beta_j);
            
            float cos_th = sign(pos.y - _BlackHolePos.y) * -vel.y;
            float delta_j = 1.0 / (gamma_j * (1.0 - beta_j * cos_th));
            float r_j = length(pos - _BlackHolePos);
            float g_grav = sqrt(saturate(1.0 - _Rs / r_j));
            
            j *= pow(delta_j * g_grav * doppler, 2.7);
            
            float spacing = 4.0 * _Rs;
            float phase = (jy - _Jet.y * t_emit) / spacing;
            float pulse = pow(0.5 + 0.5 * cos(6.2831853 * phase), 4);
            
            j *= lerp(1.0, pulse / 0.273, _JetKnots);
            
            if (_JetTurbulence > 0 && across > 0.01)
            {
                float3 rel = pos - _BlackHolePos;
                float tw = 0.5 * jy / _Rs;
            
                float s_tw, c_tw;
                sincos(tw, s_tw, c_tw);
                float2 rot = float2(rel.x * c_tw - rel.z * s_tw, rel.x * s_tw + rel.z * c_tw);
            
                float flow = jy - _Jet.y * t_emit;
                float3 q = float3(rot.x / jw, flow / (3.0 * jw), rot.y / jw) * 2.0;
                q.y += sign(rel.y) * 17.0;
            
                float jn = 0.5 + 0.5 * (gradient_noise(q) + 0.5 * gradient_noise(q * 2.0));
                j *= lerp(1.0, saturate(jn) * 2.0, _JetTurbulence);
            }
            
            jetLight += j * _JetColor.rgb * dt * (1.0 - diskAlpha);
        }

        if (diskAlpha >= 0.95) 
        {
            if (diskAlpha >= 0.995)
            {
                break;
            }
            continue;
        }

        float3 rd = pos - _BlackHolePos;
        #if defined(BH_KERR)
        float disk_r = r_now;
        #else
        float disk_r = length(rd.xz);
        #endif

        if (_DiskDensity > 0 && disk_r > r_horizon && disk_r < outerR)
        {
            bool plunging = disk_r < r_isco;
            float t_plunge = saturate((disk_r - r_horizon) / (r_isco - r_horizon));
            
            float h = _DiskThickness * disk_r;
            float pos_y = rd.y;
            float y0 = prev_pos.y - _BlackHolePos.y;
            float y_near = y0 * pos_y < 0 ? 0 : min(abs(y0), abs(pos_y));
            float vertical = exp(-0.5 * y_near * y_near / (h * h));

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
                
                float raw_noise;
                since_noise += dt;

                if (since_noise >= noise_spacing)
                {
                    float cycle = t_emit / WIND_PERIOD;

                    float phase_a = frac(cycle);
                    float phase_b = frac(cycle + 0.5);

                    float weight_a = 1 - abs(2.0 * phase_a - 1.0);
                    float weight_b = 1.0 - weight_a;

                    float seed_a = floor(cycle) * 2.0;
                    float seed_b = floor(cycle + 0.5) * 2.0 + 1.0;
                    
                    float r_cyl = length(rd.xz);
                    float2 xz_a = rd.xz * (r_cyl + v_in * phase_a * WIND_PERIOD) / r_cyl;
                    float2 xz_b = rd.xz * (r_cyl + v_in * phase_b * WIND_PERIOD) / r_cyl;

                    float pixel_world = ray_len * _PixelAngle;
                    float layer_a = disk_noise(xz_a, pos_y, twist - omega_k * phase_a * WIND_PERIOD, seed_a, pixel_world);
                    float layer_b = disk_noise(xz_b, pos_y, twist - omega_k * phase_b * WIND_PERIOD, seed_b, pixel_world);
                    raw_noise = layer_a * weight_a + layer_b * weight_b;

                    cached_noise = raw_noise;
                    since_noise = 0.0;
                }
                else
                {
                    raw_noise = cached_noise;
                }
                
                float phi_hs = omega_hs * t_emit;
                float phi = atan2(rd.z, rd.x);
                float dphi = atan2(sin(phi - phi_hs), cos(phi - phi_hs));
                float d2 = (disk_r - r_hs) * (disk_r - r_hs) + (r_hs * dphi) * (r_hs * dphi);
                float hs = exp(-0.5 * d2 / (sigma_hs * sigma_hs)) * step(1e-4, _HotSpot.x);
                
                float fray = _EdgeFraying * x * x;
                float lo = lerp(0.2, 0.7, fray);
                float hi = lerp(0.8, 0.9, fray);
                float cloud_mask = pow(smoothstep(lo, hi, raw_noise), 2.0);
                float final_density = base_density * cloud_mask;
                final_density = max(final_density, hs * base_density);

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
                rest_temp *= 1.0 + _TurbulenceContrast * (2.0 * raw_noise - 1.0);
                rest_temp *= 1.0 + _HotSpot.x * hs;
                float obs_temp = rest_temp * g * doppler;
                
                float3 emission = blackbody(obs_temp) * _BaseColor.rgb;
                emission *= _DiskExposure;
                
                float s = 1.0 / (1.41421356 * h);
                float dy = abs(pos_y - y0);
                float column = dy > 1e-5 ? h * 1.2533141 * abs(erf_approx(pos_y * s) - erf_approx(y0 * s)) * dt / dy : vertical * dt;
                
                float stepAlpha = 1.0 - exp(-(final_density * _DiskDensity * 10.0) * column);
                diskLight += emission * stepAlpha * (1.0 - diskAlpha);
                diskAlpha += stepAlpha * (1.0 - diskAlpha);
            }
            else
            {
                since_noise = 1e9;
            }
        }
        else
        {
            since_noise = 1e9;
        }
    }

    float sr, cr;
    sincos(radians(_SkyRotation), sr, cr);

    float3 skyDir = normalize(vel);
    skyDir = float3(cr * skyDir.x + sr * skyDir.z, skyDir.y, -sr * skyDir.x + cr * skyDir.z);

    float3 bg = SAMPLE_TEXTURECUBE_LOD(_SkyTex, sampler_linear_clamp, skyDir, 0).rgb * _SkyIntensity;
    bg *= blackbody(6500.0 * doppler) / blackbody(6500.0);
    float3 col = hit ? float3(0, 0, 0) : bg;

    col = col * (1.0 - diskAlpha) + diskLight + jetLight;

    return col;
}

#endif

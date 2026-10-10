using System.Threading.Tasks;
using UnityEngine;

namespace CannoliCat.Space {
    public static class NoiseTexture {
        public const int Period = 16;
        private const int TexelsPerCell = 8;
        private const int Size = Period * TexelsPerCell;
        private const string TextureName = "BlackHoleNoiseRG";

        private static Texture3D cached;

        public static Texture3D Get() {
            if (cached != null) return cached;
            
            foreach (var t in Resources.FindObjectsOfTypeAll<Texture3D>()) {
                if (t.name == TextureName) {
                    cached = t;
                    return cached;
                }
            }

            cached = Build();
            return cached;
        }

        private static Vector3[] Gradients(int period, int seed) {
            var rng = new System.Random(seed);
            var g = new Vector3[period * period * period];
            for (var i = 0; i < g.Length; i++) {
                g[i] = new Vector3((float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0),
                    (float)(rng.NextDouble() * 2.0 - 1.0));
            }
            return g;
        }

        private static float Smooth(float f) => f * f * (3f - 2f * f);

        private static float Corner(Vector3[] g, int period, int x, int y, int z, float fx, float fy, float fz) {
            var v = g[((z % period) * period + (y % period)) * period + (x % period)];
            return v.x * fx + v.y * fy + v.z * fz;
        }
        
        private static float Gradient(Vector3[] g, int period, float px, float py, float pz) {
            int x0 = Mathf.FloorToInt(px), y0 = Mathf.FloorToInt(py), z0 = Mathf.FloorToInt(pz);
            float fx = px - x0, fy = py - y0, fz = pz - z0;
            float ux = Smooth(fx), uy = Smooth(fy), uz = Smooth(fz);

            var c000 = Corner(g, period, x0, y0, z0, fx, fy, fz);
            var c100 = Corner(g, period, x0 + 1, y0, z0, fx - 1f, fy, fz);
            var c010 = Corner(g, period, x0, y0 + 1, z0, fx, fy - 1f, fz);
            var c110 = Corner(g, period, x0 + 1, y0 + 1, z0, fx - 1f, fy - 1f, fz);
            var c001 = Corner(g, period, x0, y0, z0 + 1, fx, fy, fz - 1f);
            var c101 = Corner(g, period, x0 + 1, y0, z0 + 1, fx - 1f, fy, fz - 1f);
            var c011 = Corner(g, period, x0, y0 + 1, z0 + 1, fx, fy - 1f, fz - 1f);
            var c111 = Corner(g, period, x0 + 1, y0 + 1, z0 + 1, fx - 1f, fy - 1f, fz - 1f);

            var x00 = Mathf.LerpUnclamped(c000, c100, ux);
            var x10 = Mathf.LerpUnclamped(c010, c110, ux);
            var x01 = Mathf.LerpUnclamped(c001, c101, ux);
            var x11 = Mathf.LerpUnclamped(c011, c111, ux);
            return Mathf.LerpUnclamped(Mathf.LerpUnclamped(x00, x10, uy), Mathf.LerpUnclamped(x01, x11, uy), uz);
        }

        private static Texture3D Build() {
            var g1 = Gradients(Period, 1337);
            var g2 = Gradients(Period * 2, 7331);
            var values = new float[Size * Size * Size * 2];

            void Slice(int z) {
                for (var y = 0; y < Size; y++) {
                    for (var x = 0; x < Size; x++) {
                        var px = (x + 0.5f) / TexelsPerCell;
                        var py = (y + 0.5f) / TexelsPerCell;
                        var pz = (z + 0.5f) / TexelsPerCell;
                        var one = Gradient(g1, Period, px, py, pz);
                        var idx = 2 * ((z * Size + y) * Size + x);
                        values[idx] = one + 0.5f * Gradient(g2, Period * 2, 2f * px, 2f * py, 2f * pz);
                        values[idx + 1] = one;
                    }
                }
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            for (var z = 0; z < Size; z++) Slice(z); // no threads on WebGL
#else
            Parallel.For(0, Size, Slice);
#endif

            var halves = new ushort[values.Length];
            for (var i = 0; i < values.Length; i++) halves[i] = Mathf.FloatToHalf(values[i]);

            var tex = new Texture3D(Size, Size, Size, TextureFormat.RGHalf, mipChain: false) {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = TextureName,
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixelData(halves, 0);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return tex;
        }
    }
}

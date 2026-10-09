using UnityEngine;

namespace CannoliCat.Space {
    public class BlackHoleFallCamera : MonoBehaviour {
        public enum LookMode { BlackHole, Away, Free }

        [Tooltip("1 = fall on the same clock as the disk animation. Higher values fall faster.")]
        [Min(0f)] public float speedMultiplier = 1f;

        [Tooltip("BlackHole: face the hole. Away: face back out at the universe you're leaving. Free: leave the rotation alone.")]
        public LookMode look = LookMode.BlackHole;

        [Tooltip("Start over from the starting point after reaching the bottom.")]
        public bool loop = true;

        [Tooltip("Seconds to hold at the bottom before starting over.")]
        [Min(0f)] public float loopDelay = 2f;

        public bool InsideHorizon { get; private set; }

        private Vector3 startOffset;
        private bool started;
        private double kx, ky, kz;
        private float lastTime = float.NaN;
        private float holdTimer;

        private void OnEnable() {
            started = false;
            var bh = BlackHole.Active;
            if (bh != null) startOffset = transform.position - bh.transform.position;
            started = bh != null;
            Restart();
        }

        private void Restart() {
            kx = startOffset.x;
            ky = startOffset.z;
            kz = -startOffset.y;
            lastTime = float.NaN;
            holdTimer = 0f;
        }

        private void LateUpdate() {
            var bh = BlackHole.Active;
            if (bh == null) return;
            if (!started) {
                startOffset = transform.position - bh.transform.position;
                started = true;
                Restart();
            }

            double rs = bh.SchwarzschildRadius;
            var m = 0.5 * rs;
            var a = System.Math.Min(System.Math.Abs(bh.Spin) * m, 0.4999 * rs);
            var rMinus = m - System.Math.Sqrt(System.Math.Max(m * m - a * a, 0.0));
            var rPlus = m + System.Math.Sqrt(System.Math.Max(m * m - a * a, 0.0));
            var rStop = System.Math.Max(rMinus + 0.1 * m, 0.3 * m);

            var t = bh.SimulatedTime;
            var dt = float.IsNaN(lastTime) ? 0.0 : (t - lastTime) * (double)speedMultiplier;
            lastTime = t;

            var r = RadiusKS(kx, ky, kz, a);
            if (r <= rStop) {
                holdTimer += Time.deltaTime;
                if (loop && holdTimer >= loopDelay) {
                    Restart();
                    r = RadiusKS(kx, ky, kz, a);
                }
            }
            else if (dt > 0.0) {
                var steps = (int)System.Math.Min(System.Math.Ceiling(dt / (0.02 * r)), 200.0);
                var h = dt / steps;
                for (var i = 0; i < steps && r > rStop; i++) {
                    Velocity(kx, ky, kz, a, m, out var v1x, out var v1y, out var v1z);
                    Velocity(kx + 0.5 * h * v1x, ky + 0.5 * h * v1y, kz + 0.5 * h * v1z, a, m, out var v2x, out var v2y, out var v2z);
                    Velocity(kx + 0.5 * h * v2x, ky + 0.5 * h * v2y, kz + 0.5 * h * v2z, a, m, out var v3x, out var v3y, out var v3z);
                    Velocity(kx + h * v3x, ky + h * v3y, kz + h * v3z, a, m, out var v4x, out var v4y, out var v4z);
                    kx += h / 6.0 * (v1x + 2.0 * v2x + 2.0 * v3x + v4x);
                    ky += h / 6.0 * (v1y + 2.0 * v2y + 2.0 * v3y + v4y);
                    kz += h / 6.0 * (v1z + 2.0 * v2z + 2.0 * v3z + v4z);
                    r = RadiusKS(kx, ky, kz, a);
                }
            }

            InsideHorizon = r < rPlus;
            
            var center = bh.transform.position;
            transform.position = center + new Vector3((float)kx, (float)-kz, (float)ky);

            var toCenter = center - transform.position;
            if (toCenter.sqrMagnitude < 1e-8f) return;
            switch (look) {
                case LookMode.BlackHole:
                    transform.rotation = Quaternion.LookRotation(toCenter, Vector3.up);
                    break;
                case LookMode.Away:
                    transform.rotation = Quaternion.LookRotation(-toCenter, Vector3.up);
                    break;
            }
        }

        private static double RadiusKS(double x, double y, double z, double a) {
            var w = x * x + y * y + z * z - a * a;
            return System.Math.Sqrt(0.5 * (w + System.Math.Sqrt(w * w + 4.0 * a * a * z * z)));
        }
        
        private static void Velocity(double x, double y, double z, double a, double m,
            out double vx, out double vy, out double vz) {
            var r = RadiusKS(x, y, z, a);
            var r2 = r * r;
            var a2 = a * a;
            var f = 2.0 * m * r2 * r / (r2 * r2 + a2 * z * z);
            var lx = (r * x + a * y) / (r2 + a2);
            var ly = (r * y - a * x) / (r2 + a2);
            var lz = z / r;

            var d = r * (2.0 * r2 - (x * x + y * y + z * z) + a2);
            var grx = x * r2 / d;
            var gry = y * r2 / d;
            var grz = z * (r2 + a2) / d;

            var s = System.Math.Sqrt(2.0 * m * r * (r2 + a2));
            var c = -2.0 * m * r / (2.0 * m * r + s);
            var ux = c * grx;
            var uy = c * gry;
            var uz = c * grz;

            var lu = 1.0 + lx * ux + ly * uy + lz * uz;
            var ut = 1.0 + f * lu;
            vx = (ux - f * lu * lx) / ut;
            vy = (uy - f * lu * ly) / ut;
            vz = (uz - f * lu * lz) / ut;
        }
    }
}

using UnityEngine;

namespace CannoliCat.Space {
    public class BlackHoleOrbitCamera : MonoBehaviour {
        public enum LookMode { BlackHole, Forward, Backward, Free }

        [Tooltip("Orbit radius as a multiple of the Schwarzschild radius. Circular orbits are stable down to 3 at spin 0 and about 1 at spin 0.95. Below that the speed is capped just under the speed of light.")]
        [Min(1f)] public float orbitRadius = 5f;

        [Tooltip("Height above the disk plane, in world units. Keep it above zero so the camera isn't inside the disk.")]
        public float height = 0.3f;

        [Tooltip("1 = orbit on the same clock as the disk animation, so the gas beside you looks still. Higher values orbit faster than the gas.")]
        [Min(0f)] public float speedMultiplier = 1f;

        [Tooltip("BlackHole: always face the hole. Forward: face the direction of motion, where the sky is blue-shifted. Backward: face away from it. Free: leave the rotation alone.")]
        public LookMode look = LookMode.BlackHole;

        private float angle;
        private float lastTime = float.NaN;

        private void OnEnable() {
            lastTime = float.NaN;
            var bh = BlackHole.Active;
            if (bh == null) return;

            var d = transform.position - bh.transform.position;
            angle = Mathf.Atan2(d.z, d.x);
        }

        private void LateUpdate() {
            var bh = BlackHole.Active;
            if (bh == null) return;

            var rs = bh.SchwarzschildRadius;
            var m = 0.5f * rs;
            var a = bh.Spin * m;
            var r = orbitRadius * rs;
            
            var sqrtM = Mathf.Sqrt(m);
            var omega = sqrtM / (Mathf.Pow(r, 1.5f) + a * sqrtM);

            var t = bh.SimulatedTime;
            if (!float.IsNaN(lastTime)) angle += omega * (t - lastTime) * speedMultiplier;
            lastTime = t;
            
            var radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            var center = bh.transform.position;
            transform.position = center + radial * r + Vector3.up * height;

            switch (look) {
                case LookMode.BlackHole:
                    transform.rotation = Quaternion.LookRotation(center - transform.position, Vector3.up);
                    break;
                case LookMode.Forward:
                    transform.rotation = Quaternion.LookRotation(tangent, Vector3.up);
                    break;
                case LookMode.Backward:
                    transform.rotation = Quaternion.LookRotation(-tangent, Vector3.up);
                    break;
            }
        }
    }
}

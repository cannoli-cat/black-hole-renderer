using UnityEngine;

namespace CannoliCat.Space {
    [ExecuteAlways]
    public class BlackHole : MonoBehaviour {
        public static BlackHole Active { get; private set; }

        // Published by BlackHoleFeature each frame, for scripts like BlackHoleOrbitCamera.
        public float SchwarzschildRadius { get; internal set; } = 2f;
        public float Spin { get; internal set; }
        public float SimulatedTime { get; internal set; }

        private void OnEnable() {
            Active = this;
        }

        private void OnDisable() {
            if (Active == this) Active = null;
        }
    }
}

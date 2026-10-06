using UnityEngine;

namespace CannoliCat.Space {
    [ExecuteAlways]
    public class BlackHole : MonoBehaviour {
        public static BlackHole Active { get; private set; }

        private void OnEnable() {
            Active = this;
        }

        private void OnDisable() {
            if (Active == this) Active = null;
        }
    }
}

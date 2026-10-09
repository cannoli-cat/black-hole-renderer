#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CannoliCat.Space {
    [CustomEditor(typeof(BlackHoleFeature))]
    public class BlackHoleFeatureEditor : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();

            var presetProp = serializedObject.FindProperty("preset");
            EditorGUILayout.PropertyField(presetProp);
            var preset = presetProp.objectReferenceValue as BlackHolePreset;

            using (new EditorGUI.DisabledScope(preset == null))
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Apply Preset")) {
                    CopyMatching(new SerializedObject(preset), serializedObject);
                    GUI.changed = true;
                }

                if (GUILayout.Button("Save Current to Preset")) {
                    var presetObject = new SerializedObject(preset);
                    CopyMatching(serializedObject, presetObject);
                    presetObject.ApplyModifiedProperties();
                    AssetDatabase.SaveAssetIfDirty(preset);
                }
            }

            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, "m_Script", "preset");

            serializedObject.ApplyModifiedProperties();
        }
        
        private static void CopyMatching(SerializedObject from, SerializedObject to) {
            var it = from.GetIterator();
            if (!it.NextVisible(true)) return;
            do {
                if (it.name == "m_Script" || it.name == "preset") continue;
                if (to.FindProperty(it.propertyPath) != null)
                    to.CopyFromSerializedProperty(it);
            } while (it.NextVisible(false));
        }
    }
}
#endif

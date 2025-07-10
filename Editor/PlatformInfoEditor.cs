using UnityEditor;

namespace Enbug.Billing.Editor
{
    [CustomEditor(typeof(PlatformInfo))]
    public class PlatformInfoEditor : UnityEditor.Editor
    {
        private SerializedProperty _scriptProperty;
        private SerializedProperty _appStoreProperty;

        private void OnEnable()
        {
            _scriptProperty = serializedObject.FindProperty("m_Script");
            _appStoreProperty = serializedObject.FindProperty("appStore");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_scriptProperty);
            }

            var oldValue = _appStoreProperty.enumValueFlag;
            EditorGUILayout.PropertyField(_appStoreProperty);
            var newValue = _appStoreProperty.enumValueFlag;
            if (oldValue != newValue)
            {
                EnbugBillingEditor.SetAppStore((AppStore)newValue);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
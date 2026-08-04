using Game.AssetCore;
using UnityEditor;

[CustomEditor(typeof(AssetBackendConfig))]
public sealed class AssetBackendConfigEditor : Editor
{
    private SerializedProperty _backend;
    private SerializedProperty _defaultPackageName;
    private SerializedProperty _yooPlayMode;
    private SerializedProperty _yooEditorSimulateRoot;

    private void OnEnable()
    {
        _backend = serializedObject.FindProperty(nameof(AssetBackendConfig.Backend));
        _defaultPackageName = serializedObject.FindProperty(nameof(AssetBackendConfig.DefaultPackageName));
        _yooPlayMode = serializedObject.FindProperty(nameof(AssetBackendConfig.YooPlayMode));
        _yooEditorSimulateRoot = serializedObject.FindProperty(nameof(AssetBackendConfig.YooEditorSimulateRoot));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_backend);
        switch ((AssetBackendType)_backend.intValue)
        {
            case AssetBackendType.BundleMaster:
                EditorGUILayout.PropertyField(_defaultPackageName);
                break;
            case AssetBackendType.YooAsset:
                EditorGUILayout.PropertyField(_defaultPackageName);
                EditorGUILayout.PropertyField(_yooPlayMode);
                if ((YooPlayModeKind)_yooPlayMode.intValue == YooPlayModeKind.EditorSimulate)
                    EditorGUILayout.PropertyField(_yooEditorSimulateRoot);
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }
}

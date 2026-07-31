using UnityEditor;

[CustomEditor(typeof(RockSwitchTrap), true)]
[CanEditMultipleObjects]
public class RockSwitchTrapEditor : Editor
{
    private SerializedProperty targetRock;

    private void OnEnable()
    {
        targetRock = serializedObject.FindProperty("targetRock");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(targetRock);
        serializedObject.ApplyModifiedProperties();
    }
}

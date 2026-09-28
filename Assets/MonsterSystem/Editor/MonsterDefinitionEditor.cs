using UnityEditor;

[CustomEditor(typeof(MonsterDefinition))]
public class MonsterDefinitionEditor : Editor
{
    SerializedProperty m_MonsterName;
    SerializedProperty m_BaseHp;
    SerializedProperty m_ModelPrefab;
    SerializedProperty m_Material;
    SerializedProperty m_ScaleCorrection;
    SerializedProperty m_MovementType;
    SerializedProperty m_NavMeshSpeed;
    SerializedProperty m_NavMeshAngularSpeed;
    SerializedProperty m_NavMeshAvoidanceQuality;
    SerializedProperty m_NavMeshAvoidancePriority;

    void OnEnable()
    {
        m_MonsterName = serializedObject.FindProperty("monsterName");
        m_BaseHp = serializedObject.FindProperty("baseHp");
        m_ModelPrefab = serializedObject.FindProperty("modelPrefab");
        m_Material = serializedObject.FindProperty("material");
        m_ScaleCorrection = serializedObject.FindProperty("scaleCorrection");
        m_MovementType = serializedObject.FindProperty("movementType");
        m_NavMeshSpeed = serializedObject.FindProperty("navMeshSpeed");
        m_NavMeshAngularSpeed = serializedObject.FindProperty("navMeshAngularSpeed");
        m_NavMeshAvoidanceQuality = serializedObject.FindProperty("navMeshAvoidanceQuality");
        m_NavMeshAvoidancePriority = serializedObject.FindProperty("navMeshAvoidancePriority");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_MonsterName);
        EditorGUILayout.PropertyField(m_BaseHp);
        EditorGUILayout.PropertyField(m_ModelPrefab);
        EditorGUILayout.PropertyField(m_Material);
        EditorGUILayout.PropertyField(m_ScaleCorrection);
        EditorGUILayout.PropertyField(m_MovementType);

        if ((MonsterMovementType)m_MovementType.enumValueIndex == MonsterMovementType.NavMesh)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("NavMesh Agent", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_NavMeshSpeed);
            EditorGUILayout.PropertyField(m_NavMeshAngularSpeed);
            EditorGUILayout.PropertyField(m_NavMeshAvoidanceQuality);
            EditorGUILayout.PropertyField(m_NavMeshAvoidancePriority);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

using UnityEditor;

[CustomEditor(typeof(MonsterDefinition))]
public class MonsterDefinitionEditor : Editor
{
    SerializedProperty m_MonsterName;
    SerializedProperty m_BaseHp;
    SerializedProperty m_ModelPrefab;
    SerializedProperty m_ScaleCorrection;
    SerializedProperty m_RotationSpeed;
    SerializedProperty m_MovementType;
    SerializedProperty m_NavMeshSpeed;
    SerializedProperty m_NavMeshAngularSpeed;
    SerializedProperty m_NavMeshAvoidanceQuality;
    SerializedProperty m_NavMeshAvoidancePriority;
    SerializedProperty m_IsRotated;
    SerializedProperty m_Diameter;
    SerializedProperty m_RotationPoints;

    void OnEnable()
    {
        m_MonsterName = serializedObject.FindProperty("monsterName");
        m_BaseHp = serializedObject.FindProperty("baseHp");
        m_ModelPrefab = serializedObject.FindProperty("modelPrefab");
        m_ScaleCorrection = serializedObject.FindProperty("scaleCorrection");
        m_RotationSpeed = serializedObject.FindProperty("rotationSpeed");
        m_MovementType = serializedObject.FindProperty("movementType");
        m_NavMeshSpeed = serializedObject.FindProperty("navMeshSpeed");
        m_NavMeshAngularSpeed = serializedObject.FindProperty("navMeshAngularSpeed");
        m_NavMeshAvoidanceQuality = serializedObject.FindProperty("navMeshAvoidanceQuality");
        m_NavMeshAvoidancePriority = serializedObject.FindProperty("navMeshAvoidancePriority");
        m_IsRotated = serializedObject.FindProperty("isRotated");
        m_Diameter = serializedObject.FindProperty("diameter");
        m_RotationPoints = serializedObject.FindProperty("rotationPoints");
        
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(m_MonsterName);
        EditorGUILayout.PropertyField(m_BaseHp);
        EditorGUILayout.PropertyField(m_ModelPrefab);
        EditorGUILayout.PropertyField(m_ScaleCorrection);
        EditorGUILayout.PropertyField(m_MovementType);

        if ((MonsterMovementType)m_MovementType.enumValueIndex == MonsterMovementType.NavMesh)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("NavMesh Agent", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_RotationSpeed);
            EditorGUILayout.PropertyField(m_NavMeshSpeed);
            EditorGUILayout.PropertyField(m_NavMeshAngularSpeed);
            EditorGUILayout.PropertyField(m_NavMeshAvoidanceQuality);
            EditorGUILayout.PropertyField(m_NavMeshAvoidancePriority);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(m_IsRotated);
        if (m_IsRotated.boolValue)
        {
            EditorGUILayout.LabelField("Rotation Points", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Diameter);
            EditorGUILayout.PropertyField(m_RotationPoints, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}

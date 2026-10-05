using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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
    SerializedProperty m_RotationPointPaths;

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
        m_RotationPointPaths = serializedObject.FindProperty("rotationPointPaths");
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

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_IsRotated);
            if (m_IsRotated.boolValue)
            {
                EditorGUILayout.LabelField("Rotation Points", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_Diameter);
                DrawRotationPointSelection();
            }


        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawRotationPointSelection()
    {
        var prefab = m_ModelPrefab.objectReferenceValue as GameObject;
        if (prefab == null)
            return;

        GUILayout.BeginVertical(EditorStyles.helpBox);
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(15f);

            GUILayout.BeginVertical();
            {
                var selectedPaths = new HashSet<string>();
                for (var i = 0; i < m_RotationPointPaths.arraySize; i++)
                    selectedPaths.Add(m_RotationPointPaths.GetArrayElementAtIndex(i).stringValue);

                var prefabRoot = prefab.transform;
                var children = prefab.GetComponentsInChildren<Transform>(true);
                for (var i = 0; i < children.Length; i++)
                {
                    var child = children[i];
                    if (child == prefabRoot)
                        continue;

                    var path = AnimationUtility.CalculateTransformPath(child, prefabRoot);
                    var wasSelected = selectedPaths.Contains(path);
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = indent + PathDepth(path);
                    var isSelected = EditorGUILayout.ToggleLeft(child.name, wasSelected);
                    EditorGUI.indentLevel = indent;

                    if (isSelected == wasSelected)
                        continue;

                    if (isSelected)
                    {
                        var index = m_RotationPointPaths.arraySize;
                        m_RotationPointPaths.InsertArrayElementAtIndex(index);
                        m_RotationPointPaths.GetArrayElementAtIndex(index).stringValue = path;
                        selectedPaths.Add(path);
                    }
                    else
                    {
                        RemovePath(path);
                        selectedPaths.Remove(path);
                    }
                }
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
    }

    void RemovePath(string path)
    {
        for (var i = 0; i < m_RotationPointPaths.arraySize; i++)
        {
            if (m_RotationPointPaths.GetArrayElementAtIndex(i).stringValue != path)
                continue;

            m_RotationPointPaths.DeleteArrayElementAtIndex(i);
            return;
        }
    }

    static int PathDepth(string path)
    {
        var depth = 0;
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] == '/')
                depth++;
        }

        return depth;
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public readonly struct SkillTreeGenerationResult
{
    public readonly string GenerationId;
    public readonly string AssetFolderPath;
    public readonly bool ReplacedExisting;

    public SkillTreeGenerationResult(
        string generationId,
        string assetFolderPath,
        bool replacedExisting)
    {
        GenerationId = generationId;
        AssetFolderPath = assetFolderPath;
        ReplacedExisting = replacedExisting;
    }
}

public static class SkillTreeGenerationProcessor
{
    public static SkillTreeGenerationResult Generate(
        SkillTreeDraftData draft,
        RectTransform parent)
    {
        var outputPath = NormalizeOutputPath(draft.assetOutputPath);
        if (draft.baseSize.x <= 0f || draft.baseSize.y <= 0f)
            throw new ArgumentException("Base Size must be greater than zero.");

        var existingMarker = FindExistingMarker(parent);
        var replacedExisting = existingMarker != null;
        if (existingMarker != null)
        {
            var oldAssetFolder = existingMarker.AssetFolderPath;
            UnityEngine.Object.DestroyImmediate(existingMarker.gameObject);
            if (!string.IsNullOrEmpty(oldAssetFolder))
                AssetDatabase.DeleteAsset(oldAssetFolder);
        }

        var generationId = Guid.NewGuid().ToString("N");
        var generationFolder = $"{outputPath}/Generated_{generationId}";
        var nodesFolder = $"{generationFolder}/Nodes";
        var nodeInfosFolder = $"{generationFolder}/NodeInfos";
        EnsureFolder(nodesFolder);
        EnsureFolder(nodeInfosFolder);

        var treeData = ScriptableObject.CreateInstance<SkillTreeData>();
        treeData.name = "SkillTreeData";
        AssetDatabase.CreateAsset(treeData, $"{generationFolder}/SkillTreeData.asset");

        var generatedNodes = new List<SkillNodeData>(draft.nodes.Count);
        foreach (var nodeDraft in draft.nodes)
        {
            var nodeInfo = CreateNodeInfo(nodeDraft, nodeInfosFolder);
            generatedNodes.Add(CreateNodeData(nodeDraft, nodeInfo, nodesFolder));
        }

        WriteTreeData(treeData, draft, generatedNodes);
        AssetDatabase.SaveAssets();

        var generatedRoot = SkillTreeUGuiGenerator.Generate(draft, parent);
        var marker = generatedRoot.AddComponent<GeneratedSkillTreeMarker>();
        marker.Initialize(generationId, generationFolder, treeData);
        EditorUtility.SetDirty(marker);
        EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);

        return new SkillTreeGenerationResult(
            generationId,
            generationFolder,
            replacedExisting);
    }

    static NodeInfo CreateNodeInfo(SkillNodeDraftData draft, string folder)
    {
        var info = ScriptableObject.CreateInstance<NodeInfo>();
        info.name = $"NodeInfo_{draft.guid}";
        AssetDatabase.CreateAsset(info, $"{folder}/{info.name}.asset");

        var serialized = new SerializedObject(info);
        serialized.FindProperty("mainText").stringValue = draft.nodeInfo.mainText;
        serialized.FindProperty("font").objectReferenceValue =
            SkillTreeAssetReferenceUtility.Load<Font>(draft.nodeInfo.fontGuid, 0);
        serialized.FindProperty("description").stringValue = draft.nodeInfo.description;
        serialized.FindProperty("statName").stringValue = draft.nodeInfo.statName;
        serialized.FindProperty("valueText").stringValue = draft.nodeInfo.valueText;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return info;
    }

    static SkillNodeData CreateNodeData(
        SkillNodeDraftData draft,
        NodeInfo nodeInfo,
        string folder)
    {
        var node = ScriptableObject.CreateInstance<SkillNodeData>();
        node.name = $"Node_{draft.guid}";
        AssetDatabase.CreateAsset(node, $"{folder}/{node.name}.asset");

        var serialized = new SerializedObject(node);
        serialized.FindProperty("guid").stringValue = draft.guid;
        serialized.FindProperty("nodeSprite").objectReferenceValue =
            SkillTreeAssetReferenceUtility.Load<Sprite>(draft.nodeSpriteGuid, draft.nodeSpriteLocalId);
        serialized.FindProperty("nodeColor").colorValue = draft.nodeColor;
        serialized.FindProperty("skillSprite").objectReferenceValue =
            SkillTreeAssetReferenceUtility.Load<Sprite>(draft.skillSpriteGuid, draft.skillSpriteLocalId);
        serialized.FindProperty("skillColor").colorValue = draft.skillColor;
        serialized.FindProperty("skillSpriteSizeRatio").vector2Value = draft.skillSpriteSizeRatio;
        serialized.FindProperty("nodeInfo").objectReferenceValue = nodeInfo;
        serialized.FindProperty("actionType").enumValueIndex = (int)draft.actionType;
        serialized.FindProperty("position").vector2Value = draft.position;
        serialized.FindProperty("size").vector2Value = draft.size;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return node;
    }

    static void WriteTreeData(
        SkillTreeData treeData,
        SkillTreeDraftData draft,
        IReadOnlyList<SkillNodeData> nodes)
    {
        var serialized = new SerializedObject(treeData);
        serialized.FindProperty("baseSize").vector2Value = draft.baseSize;

        var nodeProperty = serialized.FindProperty("nodes");
        nodeProperty.arraySize = nodes.Count;
        for (var i = 0; i < nodes.Count; i++)
            nodeProperty.GetArrayElementAtIndex(i).objectReferenceValue = nodes[i];

        var connectionProperty = serialized.FindProperty("connections");
        connectionProperty.arraySize = draft.connections.Count;
        for (var i = 0; i < draft.connections.Count; i++)
            WriteConnection(
                connectionProperty.GetArrayElementAtIndex(i),
                draft.connections[i]);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WriteConnection(
        SerializedProperty property,
        SkillConnectionDraftData draft)
    {
        property.FindPropertyRelative("parentNodeGuid").stringValue = draft.parentNodeGuid;
        property.FindPropertyRelative("childNodeGuid").stringValue = draft.childNodeGuid;
        property.FindPropertyRelative("startAnchor").enumValueIndex = (int)draft.startAnchor;
        property.FindPropertyRelative("endAnchor").enumValueIndex = (int)draft.endAnchor;
        property.FindPropertyRelative("routingMode").enumValueIndex = (int)draft.routingMode;

        var pathProperty = property.FindPropertyRelative("pathPoints");
        pathProperty.arraySize = draft.pathPoints.Count;
        for (var i = 0; i < draft.pathPoints.Count; i++)
            pathProperty.GetArrayElementAtIndex(i).vector2Value = draft.pathPoints[i];
    }

    static GeneratedSkillTreeMarker FindExistingMarker(RectTransform parent)
    {
        for (var i = 0; i < parent.childCount; i++)
        {
            var marker = parent.GetChild(i).GetComponent<GeneratedSkillTreeMarker>();
            if (marker != null)
                return marker;
        }

        return null;
    }

    static string NormalizeOutputPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("ScriptableObject Path is required.");

        var normalized = path.Replace('\\', '/').TrimEnd('/');
        if (normalized != "Assets" && !normalized.StartsWith("Assets/", StringComparison.Ordinal))
            throw new ArgumentException("ScriptableObject Path must be inside Assets.");
        return normalized;
    }

    static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class SkillTreeDraftData
{
    public int version = 5;
    public Vector2 baseSize = new(1920f, 1080f);
    public string assetOutputPath = "Assets/Generated/SkillTree";
    public string uguiParentGlobalObjectId;
    public List<SkillNodeDraftData> nodes = new();
    public List<SkillConnectionDraftData> connections = new();
}

public static class SkillTreeDraftStore
{
    const string k_DraftPath = "Library/SkillTreeEditor/SkillTreeDraft.json";

    public static SkillTreeDraftData Load()
    {
        if (!File.Exists(k_DraftPath))
            return new SkillTreeDraftData();

        var draft = JsonUtility.FromJson<SkillTreeDraftData>(File.ReadAllText(k_DraftPath))
            ?? new SkillTreeDraftData();
        Normalize(draft);
        return draft;
    }

    public static void Save(SkillTreeDraftData draft)
    {
        Normalize(draft);
        Directory.CreateDirectory(Path.GetDirectoryName(k_DraftPath));
        File.WriteAllText(k_DraftPath, JsonUtility.ToJson(draft, true));
    }

    static void Normalize(SkillTreeDraftData draft)
    {
        var defaultSpriteColor = new Color(100f / 255f, 100f / 255f, 100f / 255f);
        draft.nodes ??= new List<SkillNodeDraftData>();
        draft.connections ??= new List<SkillConnectionDraftData>();

        if (draft.version == 0)
        {
            foreach (var node in draft.nodes)
            {
                node.nodeColor = defaultSpriteColor;
                node.skillColor = defaultSpriteColor;
                node.skillSpriteSizeRatio = new Vector2(0.64f, 0.64f);
            }
        }
        else if (draft.version < 2)
        {
            foreach (var node in draft.nodes)
            {
                if (node.nodeColor == Color.white)
                    node.nodeColor = defaultSpriteColor;
                if (node.skillColor == Color.white)
                    node.skillColor = defaultSpriteColor;
            }
        }

        if (draft.version < 3)
        {
            var oldOriginOffset = draft.baseSize * 0.5f;
            foreach (var node in draft.nodes)
                node.position += node.size * 0.5f - oldOriginOffset;

            foreach (var connection in draft.connections)
            {
                for (var i = 0; i < connection.pathPoints.Count; i++)
                    connection.pathPoints[i] -= oldOriginOffset;
            }
        }

        if (draft.version < 4)
        {
            foreach (var connection in draft.connections)
                connection.bendPointCount = -1;
        }

        if (draft.version < 5)
        {
            foreach (var connection in draft.connections)
                connection.hasCustomBendPointCount = false;
        }

        foreach (var node in draft.nodes)
            node.nodeInfo ??= new SkillNodeInfoDraftData();
        foreach (var connection in draft.connections)
            connection.pathPoints ??= new List<Vector2>();

        draft.version = 5;
    }
}

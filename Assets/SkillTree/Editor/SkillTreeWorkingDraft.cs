using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SkillNodeDraftData
{
    public string guid;
    public string nodeSpriteGuid;
    public long nodeSpriteLocalId;
    public Color nodeColor = new(100f / 255f, 100f / 255f, 100f / 255f);
    public string skillSpriteGuid;
    public long skillSpriteLocalId;
    public Color skillColor = new(100f / 255f, 100f / 255f, 100f / 255f);
    public Vector2 skillSpriteSizeRatio = new(0.64f, 0.64f);
    public SkillNodeInfoDraftData nodeInfo = new();
    public SkillActionType actionType;
    public Vector2 position;
    public Vector2 size = new(200f, 100f);
}

[Serializable]
public sealed class SkillNodeInfoDraftData
{
    public string mainText;
    public string fontGuid;
    public string description;
    public string statName;
    public string valueText;
}

[Serializable]
public sealed class SkillConnectionDraftData
{
    public string parentNodeGuid;
    public string childNodeGuid;
    public SkillNodeAnchor startAnchor;
    public SkillNodeAnchor endAnchor;
    public SkillConnectionRoutingMode routingMode;
    public int bendPointCount = -1;
    public bool hasCustomBendPointCount;
    public List<Vector2> pathPoints = new();
}

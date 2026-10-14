using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SkillConnectionData
{
    [SerializeField] Color color = new(0.68f, 0.72f, 0.78f);
    [SerializeField] Sprite sprite;
    [SerializeField] bool repeatSprite;
    [SerializeField] string parentNodeGuid;
    [SerializeField] string childNodeGuid;
    [SerializeField] SkillNodeAnchor startAnchor;
    [SerializeField] SkillNodeAnchor endAnchor;
    [SerializeField] SkillConnectionRoutingMode routingMode;
    [SerializeField] List<Vector2> pathPoints = new();

    public Color Color => color;
    public Sprite Sprite => sprite;
    public bool RepeatSprite => repeatSprite;
    public string ParentNodeGuid => parentNodeGuid;
    public string ChildNodeGuid => childNodeGuid;
    public SkillNodeAnchor StartAnchor => startAnchor;
    public SkillNodeAnchor EndAnchor => endAnchor;
    public SkillConnectionRoutingMode RoutingMode => routingMode;
    public IReadOnlyList<Vector2> PathPoints => pathPoints;
}

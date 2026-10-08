using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SkillConnectionData
{
    [SerializeField] string parentNodeGuid;
    [SerializeField] string childNodeGuid;
    [SerializeField] SkillNodeAnchor startAnchor;
    [SerializeField] SkillNodeAnchor endAnchor;
    [SerializeField] SkillConnectionRoutingMode routingMode;
    [SerializeField] List<Vector2> pathPoints = new();

    public string ParentNodeGuid => parentNodeGuid;
    public string ChildNodeGuid => childNodeGuid;
    public SkillNodeAnchor StartAnchor => startAnchor;
    public SkillNodeAnchor EndAnchor => endAnchor;
    public SkillConnectionRoutingMode RoutingMode => routingMode;
    public IReadOnlyList<Vector2> PathPoints => pathPoints;
}

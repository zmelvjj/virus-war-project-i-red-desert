using System.Collections.Generic;
using UnityEngine;

public sealed class SkillTreeData : ScriptableObject
{
    [SerializeField] Vector2 baseSize;
    [SerializeField] List<SkillNodeData> nodes = new();
    [SerializeField] List<SkillConnectionData> connections = new();

    public Vector2 BaseSize => baseSize;
    public IReadOnlyList<SkillNodeData> Nodes => nodes;
    public IReadOnlyList<SkillConnectionData> Connections => connections;
}

using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    readonly Dictionary<string, SkillNodeData> m_NodeData = new();
    readonly Dictionary<string, HashSet<string>> m_ParentGuids = new();
    readonly Dictionary<string, SkillNodeState> m_NodeStates = new();
    readonly HashSet<string> m_UnlockedNodeGuids = new();

    SkillTreeData m_TreeData;
    SkillTreeUI m_TreeUi;
    bool m_Initialized;

    public event Action<SkillNodeData> OnNodeUnlocked;

    public SkillTreeData TreeData => m_TreeData;
    public IReadOnlyDictionary<string, SkillNodeState> NodeStates => m_NodeStates;
    public IReadOnlyCollection<string> UnlockedNodeGuids => m_UnlockedNodeGuids;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        SkillTreeNodeUI.NodePointerUp += HandleNodePointerUp;
    }

    void OnDisable()
    {
        SkillTreeNodeUI.NodePointerUp -= HandleNodePointerUp;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Initialize(
        SkillTreeData treeData,
        SkillTreeUI treeUi,
        IReadOnlyDictionary<string, SkillNodeData> nodeData)
    {
        m_Initialized = false;
        m_TreeData = treeData;
        m_TreeUi = treeUi;
        m_NodeData.Clear();
        m_ParentGuids.Clear();
        m_NodeStates.Clear();
        m_UnlockedNodeGuids.Clear();

        foreach (var pair in nodeData)
        {
            m_NodeData.Add(pair.Key, pair.Value);
            m_ParentGuids.Add(pair.Key, new HashSet<string>());
        }

        foreach (var connection in treeData.Connections)
        {
            if (!m_NodeData.ContainsKey(connection.ParentNodeGuid)
                || !m_NodeData.ContainsKey(connection.ChildNodeGuid))
            {
                Debug.LogError(
                    $"[SkillTreeManager] 연결 대상 Node GUID가 없습니다: "
                    + $"{connection.ParentNodeGuid} -> {connection.ChildNodeGuid}",
                    treeData);
                return;
            }

            m_ParentGuids[connection.ChildNodeGuid].Add(connection.ParentNodeGuid);
        }

        var rootCount = 0;
        foreach (var parentGuids in m_ParentGuids.Values)
        {
            if (parentGuids.Count == 0)
                rootCount++;
        }

        if (rootCount != 1)
        {
            Debug.LogWarning(
                $"[SkillTreeManager] Parent가 없는 Root Node는 하나여야 합니다. 현재 개수: {rootCount}",
                treeData);
        }

        m_Initialized = true;
        RecalculateStates();
    }

    public SkillNodeState GetState(string nodeGuid)
    {
        return m_NodeStates[nodeGuid];
    }

    public bool TryGetNodeData(string nodeGuid, out SkillNodeData nodeData)
    {
        return m_NodeData.TryGetValue(nodeGuid, out nodeData);
    }

    public bool TryUnlock(string nodeGuid)
    {
        if (!m_Initialized
            || !m_NodeData.TryGetValue(nodeGuid, out var nodeData)
            || m_NodeStates[nodeGuid] != SkillNodeState.Open
            || !CanUnlock(nodeData))
        {
            return false;
        }

        m_UnlockedNodeGuids.Add(nodeGuid);
        RecalculateStates();
        OnNodeUnlocked?.Invoke(nodeData);
        return true;
    }

    bool CanUnlock(SkillNodeData nodeData)
    {
        return true;
    }

    void HandleNodePointerUp(string nodeGuid)
    {
        TryUnlock(nodeGuid);
    }

    void RecalculateStates()
    {
        m_NodeStates.Clear();

        foreach (var pair in m_NodeData)
        {
            var state = CalculateState(pair.Key);
            m_NodeStates.Add(pair.Key, state);
            pair.Value.NodeInfo.SetState(
                state == SkillNodeState.Open || state == SkillNodeState.Unlocked
                    ? SkillNodeInfoState.Open
                    : SkillNodeInfoState.Locked);
        }

        m_TreeUi.ApplyStates(m_NodeStates);
    }

    SkillNodeState CalculateState(string nodeGuid)
    {
        if (m_UnlockedNodeGuids.Contains(nodeGuid))
            return SkillNodeState.Unlocked;

        var parentGuids = m_ParentGuids[nodeGuid];
        if (parentGuids.Count == 0)
            return SkillNodeState.Open;

        var unlockedParentCount = 0;
        foreach (var parentGuid in parentGuids)
        {
            if (m_UnlockedNodeGuids.Contains(parentGuid))
                unlockedParentCount++;
        }

        if (unlockedParentCount == 0)
            return SkillNodeState.Locked;

        return unlockedParentCount == parentGuids.Count
            ? SkillNodeState.Open
            : SkillNodeState.Closed;
    }
}

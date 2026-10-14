using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image))]
public sealed class SkillTreeUI : MonoBehaviour,
    IScrollHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IDragHandler
{
    [SerializeField, Min(0.01f)] float zoomSpeed = 0.15f;

    readonly Dictionary<string, SkillTreeNodeUI> m_NodeUis = new();
    readonly Dictionary<string, SkillNodeData> m_NodeData = new();
    readonly List<SkillTreeConnectionUI> m_ConnectionUis = new();
    readonly Vector3[] m_ContentCorners = new Vector3[4];
    readonly Vector3[] m_ViewportCorners = new Vector3[4];

    RectTransform m_RectTransform;
    RectTransform m_Viewport;
    Vector2 m_LastPointerPosition;
    bool m_IsDragging;

    public IReadOnlyDictionary<string, SkillNodeData> NodeData => m_NodeData;

    void Awake()
    {
        m_RectTransform = (RectTransform)transform;
        m_Viewport = m_RectTransform.parent as RectTransform;
    }

    void Start()
    {
        ClampScaleAndPosition();
        Initialize();
    }

    void OnDisable()
    {
        m_IsDragging = false;
    }

    void OnRectTransformDimensionsChange()
    {
        if (m_RectTransform != null && m_Viewport != null)
            ClampScaleAndPosition();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData.scrollDelta.y == 0f)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_RectTransform,
            eventData.position,
            eventData.enterEventCamera,
            out var cursorPosition);

        var cursorWorldPosition = m_RectTransform.TransformPoint(cursorPosition);
        var scale = m_RectTransform.localScale.x
            * Mathf.Exp(eventData.scrollDelta.y * zoomSpeed);
        scale = Mathf.Max(CalculateMinimumScale(), scale);
        m_RectTransform.localScale = new Vector3(scale, scale, 1f);

        var cursorOffset = cursorWorldPosition
            - m_RectTransform.TransformPoint(cursorPosition);
        m_RectTransform.anchoredPosition +=
            (Vector2)m_Viewport.InverseTransformVector(cursorOffset);

        ClampPosition();
        eventData.Use();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        m_IsDragging = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_Viewport,
            eventData.position,
            eventData.pressEventCamera,
            out m_LastPointerPosition);
        eventData.Use();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!m_IsDragging
            || eventData.button != PointerEventData.InputButton.Left
            || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_Viewport,
                eventData.position,
                eventData.pressEventCamera,
                out var pointerPosition))
        {
            return;
        }

        m_RectTransform.anchoredPosition += pointerPosition - m_LastPointerPosition;
        m_LastPointerPosition = pointerPosition;
        ClampPosition();
        eventData.Use();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        m_IsDragging = false;
        eventData.Use();
    }

    public void Initialize()
    {
        var markers = GetComponentsInChildren<GeneratedSkillTreeMarker>(true);
        if (markers.Length != 1)
        {
            Debug.LogError(
                $"[SkillTreeUI] Generated Skill Tree Root가 정확히 하나 필요합니다. 현재 개수: {markers.Length}",
                this);
            return;
        }

        var treeData = markers[0].SkillTreeData;
        if (treeData == null)
        {
            Debug.LogError("[SkillTreeUI] Generated Root에 SkillTreeData가 없습니다.", this);
            return;
        }

        if (!CacheNodeData(treeData) || !CacheGeneratedUi(markers[0]))
            return;

        if (!ValidateNodeBindings())
            return;

        if (!SkillTreeManager.HasInstance)
        {
            Debug.LogError("[SkillTreeUI] Scene에 SkillTreeManager가 없습니다.", this);
            return;
        }

        SkillTreeManager.Instance.Initialize(treeData, this, m_NodeData);
    }

    public void ApplyStates(IReadOnlyDictionary<string, SkillNodeState> states)
    {
        foreach (var pair in m_NodeUis)
            pair.Value.ApplyState(states[pair.Key]);

        foreach (var connectionUi in m_ConnectionUis)
        {
            var parentState = states[connectionUi.ParentNodeGuid];
            connectionUi.SetVisible(parentState == SkillNodeState.Unlocked);
        }
    }

    bool CacheNodeData(SkillTreeData treeData)
    {
        m_NodeData.Clear();

        foreach (var node in treeData.Nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.Guid))
            {
                Debug.LogError("[SkillTreeUI] GUID가 없는 SkillNodeData가 있습니다.", treeData);
                return false;
            }

            if (m_NodeData.ContainsKey(node.Guid))
            {
                Debug.LogError($"[SkillTreeUI] 중복 Node Data GUID: {node.Guid}", treeData);
                return false;
            }

            m_NodeData.Add(node.Guid, node);
        }

        return true;
    }

    bool CacheGeneratedUi(GeneratedSkillTreeMarker marker)
    {
        m_NodeUis.Clear();
        m_ConnectionUis.Clear();

        foreach (var nodeUi in marker.GetComponentsInChildren<SkillTreeNodeUI>(true))
        {
            if (string.IsNullOrEmpty(nodeUi.NodeGuid))
            {
                Debug.LogError("[SkillTreeUI] GUID가 없는 SkillTreeNodeUI가 있습니다.", nodeUi);
                return false;
            }

            if (m_NodeUis.ContainsKey(nodeUi.NodeGuid))
            {
                Debug.LogError($"[SkillTreeUI] 중복 Node UI GUID: {nodeUi.NodeGuid}", nodeUi);
                return false;
            }

            m_NodeUis.Add(nodeUi.NodeGuid, nodeUi);
        }

        m_ConnectionUis.AddRange(
            marker.GetComponentsInChildren<SkillTreeConnectionUI>(true));
        return true;
    }

    bool ValidateNodeBindings()
    {
        foreach (var guid in m_NodeData.Keys)
        {
            if (!m_NodeUis.ContainsKey(guid))
            {
                Debug.LogError($"[SkillTreeUI] Node Data와 연결할 UI가 없습니다: {guid}", this);
                return false;
            }
        }

        foreach (var guid in m_NodeUis.Keys)
        {
            if (!m_NodeData.ContainsKey(guid))
            {
                Debug.LogError($"[SkillTreeUI] Node UI와 연결할 Data가 없습니다: {guid}", this);
                return false;
            }
        }

        foreach (var connectionUi in m_ConnectionUis)
        {
            if (!m_NodeData.ContainsKey(connectionUi.ParentNodeGuid)
                || !m_NodeData.ContainsKey(connectionUi.ChildNodeGuid))
            {
                Debug.LogError(
                    $"[SkillTreeUI] Connection UI와 연결할 Node Data가 없습니다: "
                    + $"{connectionUi.ParentNodeGuid} -> {connectionUi.ChildNodeGuid}",
                    connectionUi);
                return false;
            }
        }

        return true;
    }

    float CalculateMinimumScale()
    {
        var contentSize = m_RectTransform.rect.size;
        var viewportSize = m_Viewport.rect.size;
        return Mathf.Max(
            viewportSize.x / contentSize.x,
            viewportSize.y / contentSize.y);
    }

    void ClampScaleAndPosition()
    {
        var scale = Mathf.Max(
            CalculateMinimumScale(),
            m_RectTransform.localScale.x);
        m_RectTransform.localScale = new Vector3(scale, scale, 1f);
        ClampPosition();
    }

    void ClampPosition()
    {
        m_RectTransform.GetWorldCorners(m_ContentCorners);
        m_Viewport.GetWorldCorners(m_ViewportCorners);

        var correction = Vector3.zero;
        if (m_ContentCorners[0].x > m_ViewportCorners[0].x)
            correction.x = m_ViewportCorners[0].x - m_ContentCorners[0].x;
        else if (m_ContentCorners[2].x < m_ViewportCorners[2].x)
            correction.x = m_ViewportCorners[2].x - m_ContentCorners[2].x;

        if (m_ContentCorners[0].y > m_ViewportCorners[0].y)
            correction.y = m_ViewportCorners[0].y - m_ContentCorners[0].y;
        else if (m_ContentCorners[2].y < m_ViewportCorners[2].y)
            correction.y = m_ViewportCorners[2].y - m_ContentCorners[2].y;

        m_RectTransform.anchoredPosition +=
            (Vector2)m_Viewport.InverseTransformVector(correction);
    }
}

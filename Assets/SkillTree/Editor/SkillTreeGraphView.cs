using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public enum SkillNodeEditMode
{
    Move,
    Resize,
    Info
}

public enum SkillNodeResizeCorner
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

public sealed class SkillTreeGraphView : GraphView
{
    static readonly Vector2 k_DefaultNodeSize = new(200f, 100f);
    static readonly Color k_ParentHighlightColor = new Color32(70, 189, 114, 255);
    static readonly Color k_ChildHighlightColor = new Color32(57, 155, 234, 255);

    readonly SkillTreeDraftData m_Draft;
    readonly List<SkillNodeView> m_NodeViews = new();
    readonly Dictionary<SkillConnectionDraftData, SkillConnectionView> m_ConnectionViews = new();
    readonly Dictionary<SkillNodeView, Vector2> m_NodeDragStartCenters = new();
    SkillTreeCanvasElement m_BaseCanvas;
    SkillNodeView m_SelectedNode;
    SkillConnectionView m_SelectedConnection;
    bool m_HasPendingTransformChange;
    SkillNodeView m_ConnectionStartNode;
    SkillNodeAnchor m_ConnectionStartAnchor;
    SkillConnectionView m_ConnectionPreview;
    SkillNodeInfoTooltipView m_InfoTooltip;
    SkillNodeView m_SnapReferenceNode;
    bool m_SnapToInteger;

    public event Action<SkillNodeView> NodeSelected;
    public event Action<SkillConnectionView> ConnectionSelected;
    public event Action<SkillNodeView> NodeChanged;
    public event Action<SkillNodeView> NodeInfoChanged;
    public event Action DraftChanged;
    public event Action<SkillNodeEditMode> ModeChanged;

    public SkillNodeEditMode EditMode { get; private set; } = SkillNodeEditMode.Move;
    public int SnapStep { get; set; } = 1;

    public SkillTreeGraphView(SkillTreeDraftData draft)
    {
        m_Draft = draft;
        name = "skillTreeGraph";
        focusable = true;
        AddToClassList("skill-tree-graph");

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        grid.AddToClassList("graph-grid");
        Insert(0, grid);

        m_BaseCanvas = new SkillTreeCanvasElement
        {
            capabilities = 0,
            pickingMode = PickingMode.Ignore
        };
        m_BaseCanvas.AddToClassList("base-canvas");
        m_BaseCanvas.SetPosition(new Rect(-draft.baseSize * 0.5f, draft.baseSize));
        AddElement(m_BaseCanvas);
        m_BaseCanvas.SendToBack();

        RegisterCallback<KeyDownEvent>(OnKeyDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
        RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<GeometryChangedEvent>(CenterInitialView);

        foreach (var node in draft.nodes)
            AddNodeView(node);

        foreach (var connection in draft.connections)
        {
            if (connection.pathPoints == null || connection.pathPoints.Count < 2)
                RouteConnection(connection);
            AddConnectionView(connection);
        }

        m_InfoTooltip = new SkillNodeInfoTooltipView();
        m_InfoTooltip.NodeInfoChanged += node =>
        {
            NodeInfoChanged?.Invoke(node);
            DraftChanged?.Invoke();
        };
        contentViewContainer.Add(m_InfoTooltip);
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        if (EditMode == SkillNodeEditMode.Info)
            return;

        var target = evt.target as VisualElement;
        var connection = target as SkillConnectionView
            ?? target?.GetFirstAncestorOfType<SkillConnectionView>();
        if (connection != null && connection.Draft != null)
        {
            evt.menu.AppendAction("Delete Line", _ => DeleteConnection(connection));
            if (connection.Draft.routingMode == SkillConnectionRoutingMode.Manual)
                evt.menu.AppendAction("Auto Route", _ => SetAutoRoute(connection));
            return;
        }

        var anchor = target as SkillNodeAnchorView
            ?? target?.GetFirstAncestorOfType<SkillNodeAnchorView>();
        var node = anchor?.Node
            ?? target as SkillNodeView
            ?? target?.GetFirstAncestorOfType<SkillNodeView>();
        if (node != null)
        {
            evt.menu.AppendAction("Delete Node", _ => DeleteNode(node));
            return;
        }

        var graphPosition = contentViewContainer.WorldToLocal(evt.mousePosition);
        evt.menu.AppendAction("Create Node", _ => CreateNode(graphPosition));
    }

    void CreateNode(Vector2 graphPosition)
    {
        var draft = new SkillNodeDraftData
        {
            guid = Guid.NewGuid().ToString("N"),
            position = graphPosition,
            size = k_DefaultNodeSize
        };
        SkillTreeAssetReferenceUtility.GetDefaultNodeSpriteReference(
            out draft.nodeSpriteGuid,
            out draft.nodeSpriteLocalId);

        m_Draft.nodes.Add(draft);
        AddNodeView(draft);
        DraftChanged?.Invoke();
    }

    SkillNodeView AddNodeView(SkillNodeDraftData draft)
    {
        var node = new SkillNodeView(
            draft,
            () => scale,
            SnapNodePosition,
            () => SnapStep,
            GetSelectedNodes,
            BeginConnectionDrag,
            UpdateConnectionDrag,
            EndConnectionDrag);
        node.Selected += OnNodeSelected;
        node.Unselected += OnNodeUnselected;
        node.Changed += changedNode =>
        {
            m_HasPendingTransformChange = true;
            UpdateConnectionsForNode(changedNode);
            NodeChanged?.Invoke(changedNode);
        };
        node.SetEditMode(EditMode);
        m_NodeViews.Add(node);
        AddElement(node);
        foreach (var anchor in node.Anchors)
            contentViewContainer.Add(anchor);
        node.UpdateAnchorPositions();
        return node;
    }

    void DeleteNode(SkillNodeView node, bool notify = true)
    {
        var wasInspectorNode = m_SelectedNode == node;
        if (wasInspectorNode)
            m_SelectedNode = null;

        for (var i = m_Draft.connections.Count - 1; i >= 0; i--)
        {
            var connection = m_Draft.connections[i];
            if (connection.parentNodeGuid == node.Draft.guid
                || connection.childNodeGuid == node.Draft.guid)
            {
                RemoveConnection(connection);
            }
        }

        m_Draft.nodes.Remove(node.Draft);
        m_NodeViews.Remove(node);
        foreach (var anchor in node.Anchors)
            anchor.RemoveFromHierarchy();
        RemoveElement(node);
        UpdateRelationshipHighlights();
        if (wasInspectorNode && notify)
            SelectFirstNodeForInspector();
        if (notify)
            DraftChanged?.Invoke();
    }

    void OnNodeSelected(SkillNodeView node)
    {
        if (m_SelectedConnection != null)
        {
            m_SelectedConnection = null;
            ConnectionSelected?.Invoke(null);
        }

        if (EditMode == SkillNodeEditMode.Info)
        {
            m_SelectedNode = node;
            UpdateRelationshipHighlights();
            NodeSelected?.Invoke(node);
            ShowSelectedInfoTooltip(node);
            return;
        }

        if (m_SelectedNode != null)
            return;

        m_SelectedNode = node;
        UpdateRelationshipHighlights();
        NodeSelected?.Invoke(node);
    }

    void OnNodeUnselected(SkillNodeView node)
    {
        if (m_SelectedNode != node)
            return;

        m_SelectedNode = null;
        if (EditMode == SkillNodeEditMode.Info)
        {
            UpdateRelationshipHighlights();
            NodeSelected?.Invoke(null);
            m_InfoTooltip.Hide();
            return;
        }
        SelectFirstNodeForInspector();
    }

    List<SkillNodeView> GetSelectedNodes()
    {
        var selectedNodes = new List<SkillNodeView>();
        foreach (var node in m_NodeViews)
        {
            if (node.IsNodeSelected)
                selectedNodes.Add(node);
        }

        return selectedNodes;
    }

    void SelectFirstNodeForInspector()
    {
        foreach (var node in m_NodeViews)
        {
            if (!node.IsNodeSelected)
                continue;

            m_SelectedNode = node;
            UpdateRelationshipHighlights();
            NodeSelected?.Invoke(node);
            return;
        }

        UpdateRelationshipHighlights();
        NodeSelected?.Invoke(null);
    }

    public void RefreshNodeInfo(SkillNodeView node)
    {
        m_InfoTooltip.Refresh(node);
    }

    void ShowSelectedInfoTooltip(SkillNodeView node)
    {
        var rect = node.GetPosition();
        var position = new Vector2(rect.xMax + 16f, rect.center.y - 24f);
        m_InfoTooltip.Show(node, position, true);
    }

    void ShowHoverInfoTooltip(SkillNodeView node, Vector2 panelPosition)
    {
        var position = contentViewContainer.WorldToLocal(panelPosition) + new Vector2(18f, 18f);
        m_InfoTooltip.Show(node, position, false);
    }

    static SkillNodeView GetNodeFromTarget(VisualElement target)
    {
        return target as SkillNodeView
            ?? target?.GetFirstAncestorOfType<SkillNodeView>();
    }

    void UpdateRelationshipHighlights()
    {
        foreach (var node in m_NodeViews)
            node.SetRelationshipHighlight(false, false);
        foreach (var connection in m_ConnectionViews.Values)
            connection.SetTemporaryColor(null);

        if (m_SelectedNode == null)
            return;

        var selectedGuid = m_SelectedNode.Draft.guid;
        foreach (var connection in m_Draft.connections)
        {
            if (connection.parentNodeGuid == selectedGuid)
            {
                FindNode(connection.childNodeGuid)?.SetRelationshipHighlight(false, true);
                if (m_ConnectionViews.TryGetValue(connection, out var childConnection))
                    childConnection.SetTemporaryColor(k_ChildHighlightColor);
            }
            else if (connection.childNodeGuid == selectedGuid)
            {
                FindNode(connection.parentNodeGuid)?.SetRelationshipHighlight(true, false);
                if (m_ConnectionViews.TryGetValue(connection, out var parentConnection))
                    parentConnection.SetTemporaryColor(k_ParentHighlightColor);
            }
        }
    }

    void AddConnectionView(SkillConnectionDraftData draft)
    {
        var view = new SkillConnectionView(
            draft,
            () => scale,
            () => SnapStep,
            SelectConnection);
        view.PathChanged += RefreshManualConnection;
        view.PathEditCompleted += _ => DraftChanged?.Invoke();
        view.Deselected += OnConnectionUnselected;
        m_ConnectionViews[draft] = view;
        AddElement(view);
        view.SendToBack();
        m_BaseCanvas.SendToBack();
        view.SetMinimumBendPointCount(GetMinimumBendPointCount(draft));
        RefreshManualConnection(view);
        UpdateRelationshipHighlights();
    }

    void BeginConnectionDrag(
        SkillNodeView node,
        SkillNodeAnchor anchor,
        Vector2 panelPosition)
    {
        m_ConnectionStartNode = node;
        m_ConnectionStartAnchor = anchor;
        foreach (var nodeView in m_NodeViews)
            nodeView.SetConnectionDragActive(true);

        m_ConnectionPreview = new SkillConnectionView(
            null,
            () => scale,
            () => SnapStep,
            null,
            true);
        AddElement(m_ConnectionPreview);
        m_ConnectionPreview.SendToBack();
        m_BaseCanvas.SendToBack();
        UpdateConnectionDrag(panelPosition);
    }

    void UpdateConnectionDrag(Vector2 panelPosition)
    {
        if (m_ConnectionPreview == null || m_ConnectionStartNode == null)
            return;

        var pointerPosition = contentViewContainer.WorldToLocal(panelPosition);
        var picked = panel.Pick(panelPosition);
        var targetAnchor = picked as SkillNodeAnchorView
            ?? picked?.GetFirstAncestorOfType<SkillNodeAnchorView>();
        var path = targetAnchor != null && targetAnchor.Node != m_ConnectionStartNode
            ? SmartOrthogonalRouter.Route(
                m_ConnectionStartNode.GetPosition(),
                m_ConnectionStartAnchor,
                targetAnchor.Node.GetPosition(),
                targetAnchor.Anchor)
            : SmartOrthogonalRouter.Preview(
                m_ConnectionStartNode.GetPosition(),
                m_ConnectionStartAnchor,
                pointerPosition);
        m_ConnectionPreview.SetPreviewPath(path);
    }

    void EndConnectionDrag(SkillNodeAnchorView targetAnchor)
    {
        if (m_ConnectionPreview != null)
            RemoveElement(m_ConnectionPreview);
        m_ConnectionPreview = null;

        foreach (var nodeView in m_NodeViews)
            nodeView.SetConnectionDragActive(false);

        if (m_ConnectionStartNode == null
            || targetAnchor == null
            || !SkillConnectionRules.CanCreate(
                m_Draft.connections,
                m_ConnectionStartNode.Draft.guid,
                targetAnchor.Node.Draft.guid))
        {
            m_ConnectionStartNode = null;
            return;
        }

        var draft = new SkillConnectionDraftData
        {
            parentNodeGuid = m_ConnectionStartNode.Draft.guid,
            childNodeGuid = targetAnchor.Node.Draft.guid,
            startAnchor = m_ConnectionStartAnchor,
            endAnchor = targetAnchor.Anchor,
            routingMode = SkillConnectionRoutingMode.Auto
        };
        m_Draft.connections.Add(draft);
        RouteConnection(draft);
        AddConnectionView(draft);
        m_ConnectionStartNode = null;
        DraftChanged?.Invoke();
    }

    void SelectConnection(SkillConnectionView connection, int segmentIndex)
    {
        ClearSelection();
        AddToSelection(connection);
        connection.SelectSegment(segmentIndex);
        m_SelectedNode = null;
        m_SelectedConnection = connection;
        UpdateRelationshipHighlights();
        NodeSelected?.Invoke(null);
        ConnectionSelected?.Invoke(connection);
    }

    void OnConnectionUnselected(SkillConnectionView connection)
    {
        if (m_SelectedConnection != connection)
            return;

        m_SelectedConnection = null;
        ConnectionSelected?.Invoke(null);
    }

    void DeleteConnection(SkillConnectionView connection, bool notify = true)
    {
        if (m_SelectedConnection == connection)
        {
            m_SelectedConnection = null;
            ConnectionSelected?.Invoke(null);
        }

        RemoveConnection(connection.Draft);
        if (notify)
            DraftChanged?.Invoke();
    }

    void RemoveConnection(SkillConnectionDraftData draft)
    {
        if (m_ConnectionViews.TryGetValue(draft, out var view))
        {
            RemoveElement(view);
            m_ConnectionViews.Remove(draft);
        }

        m_Draft.connections.Remove(draft);
        UpdateRelationshipHighlights();
    }

    void SetAutoRoute(SkillConnectionView connection)
    {
        connection.Draft.routingMode = SkillConnectionRoutingMode.Auto;
        connection.Draft.hasCustomBendPointCount = false;
        connection.Draft.bendPointCount = -1;
        RouteConnection(connection.Draft);
        connection.RefreshLayout();
        ConnectionSelected?.Invoke(connection);
        DraftChanged?.Invoke();
    }

    void RouteConnection(SkillConnectionDraftData connection)
    {
        var parent = FindNode(connection.parentNodeGuid);
        var child = FindNode(connection.childNodeGuid);
        if (parent == null || child == null)
            return;

        var minimumPath = SmartOrthogonalRouter.Route(
            parent.GetPosition(),
            connection.startAnchor,
            child.GetPosition(),
            connection.endAnchor);
        var minimum = Mathf.Max(0, minimumPath.Count - 2);
        var path = minimumPath;
        if (connection.hasCustomBendPointCount)
        {
            connection.bendPointCount = SmartOrthogonalRouter.NormalizeRequestedBendPointCount(
                minimum,
                connection.bendPointCount);
            path = SmartOrthogonalRouter.Route(
                parent.GetPosition(),
                connection.startAnchor,
                child.GetPosition(),
                connection.endAnchor,
                connection.bendPointCount);
        }
        connection.pathPoints.Clear();
        connection.pathPoints.AddRange(path);
        connection.bendPointCount = Mathf.Max(0, path.Count - 2);
        if (m_ConnectionViews.TryGetValue(connection, out var view))
            view.SetMinimumBendPointCount(minimum);
    }

    int GetMinimumBendPointCount(SkillConnectionDraftData connection)
    {
        var parent = FindNode(connection.parentNodeGuid);
        var child = FindNode(connection.childNodeGuid);
        if (parent == null || child == null)
            return 0;

        return SmartOrthogonalRouter.GetMinimumBendPointCount(
            parent.GetPosition(),
            connection.startAnchor,
            child.GetPosition(),
            connection.endAnchor);
    }

    public void SetConnectionBendPointCount(SkillConnectionView connection, int bendPointCount)
    {
        if (connection == null || connection.Draft == null)
            return;

        connection.Draft.routingMode = SkillConnectionRoutingMode.Auto;
        connection.Draft.bendPointCount = Mathf.Max(0, bendPointCount);
        connection.Draft.hasCustomBendPointCount = true;
        RouteConnection(connection.Draft);
        connection.RefreshLayout();
        ConnectionSelected?.Invoke(connection);
        DraftChanged?.Invoke();
    }

    void UpdateConnectionsForNode(SkillNodeView node)
    {
        foreach (var connection in m_Draft.connections)
        {
            if (connection.parentNodeGuid != node.Draft.guid
                && connection.childNodeGuid != node.Draft.guid)
            {
                continue;
            }

            if (connection.routingMode == SkillConnectionRoutingMode.Auto)
                RouteConnection(connection);
            else
                UpdateManualEndpoints(connection);

            if (!m_ConnectionViews.TryGetValue(connection, out var view))
                continue;

            view.RefreshLayout();
            RefreshManualConnection(view);
        }
    }

    void UpdateManualEndpoints(SkillConnectionDraftData connection)
    {
        var parent = FindNode(connection.parentNodeGuid);
        var child = FindNode(connection.childNodeGuid);
        if (parent == null || child == null || connection.pathPoints.Count < 4)
        {
            RouteConnection(connection);
            return;
        }

        var points = connection.pathPoints;
        var oldStartLead = points[1];
        var oldStartNext = points[2];
        var oldEndPrevious = points[^3];
        var oldEndLead = points[^2];
        var newStart = SmartOrthogonalRouter.GetAnchorPoint(parent.GetPosition(), connection.startAnchor);
        var newStartLead = newStart
            + SmartOrthogonalRouter.GetAnchorDirection(connection.startAnchor) * SmartOrthogonalRouter.LeadLength;
        var newEnd = SmartOrthogonalRouter.GetAnchorPoint(child.GetPosition(), connection.endAnchor);
        var newEndLead = newEnd
            + SmartOrthogonalRouter.GetAnchorDirection(connection.endAnchor) * SmartOrthogonalRouter.LeadLength;

        points[0] = newStart;
        points[1] = newStartLead;
        if (Mathf.Abs(oldStartLead.y - oldStartNext.y) < 0.01f)
            points[2] = new Vector2(points[2].x, newStartLead.y);
        else
            points[2] = new Vector2(newStartLead.x, points[2].y);

        points[^1] = newEnd;
        points[^2] = newEndLead;
        if (Mathf.Abs(oldEndPrevious.y - oldEndLead.y) < 0.01f)
            points[^3] = new Vector2(points[^3].x, newEndLead.y);
        else
            points[^3] = new Vector2(newEndLead.x, points[^3].y);

        var simplified = SmartOrthogonalRouter.Simplify(points);
        points.Clear();
        points.AddRange(simplified);
    }

    void RefreshManualConnection(SkillConnectionView connection)
    {
        if (connection.Draft != null)
        {
            connection.Draft.bendPointCount = Mathf.Max(0, connection.Draft.pathPoints.Count - 2);
            connection.SetMinimumBendPointCount(GetMinimumBendPointCount(connection.Draft));
        }

        if (connection.Draft == null
            || connection.Draft.routingMode == SkillConnectionRoutingMode.Auto)
        {
            if (m_SelectedConnection == connection)
                ConnectionSelected?.Invoke(connection);
            return;
        }
        if (m_SelectedConnection == connection)
            ConnectionSelected?.Invoke(connection);
    }

    SkillNodeView FindNode(string guid)
    {
        foreach (var node in m_NodeViews)
        {
            if (node.Draft.guid == guid)
                return node;
        }

        return null;
    }


    void OnKeyDown(KeyDownEvent evt)
    {
        var target = evt.target as VisualElement;
        var textField = target as TextField
            ?? target?.GetFirstAncestorOfType<TextField>();
        if (textField != null)
            return;

        if (evt.keyCode == KeyCode.I)
        {
            SetEditMode(EditMode == SkillNodeEditMode.Info
                ? SkillNodeEditMode.Move
                : SkillNodeEditMode.Info);
            evt.StopPropagation();
            return;
        }

        if (EditMode == SkillNodeEditMode.Info)
            return;

        if (evt.ctrlKey && evt.keyCode == KeyCode.D)
        {
            DuplicateSelectedNodes();
            evt.StopPropagation();
            return;
        }

        if (evt.keyCode == KeyCode.Backspace)
        {
            DeleteSelectedElements();
            evt.StopPropagation();
            return;
        }

        if (evt.keyCode != KeyCode.S)
            return;

        SetEditMode(EditMode == SkillNodeEditMode.Move
            ? SkillNodeEditMode.Resize
            : SkillNodeEditMode.Move);
        evt.StopPropagation();
    }

    public void SetEditMode(SkillNodeEditMode mode)
    {
        if (EditMode == mode)
            return;

        ClearSelection();
        m_SelectedNode = null;
        m_SelectedConnection = null;
        m_InfoTooltip.Hide();
        EditMode = mode;

        foreach (var node in m_NodeViews)
            node.SetEditMode(EditMode);

        EnableInClassList("resize-mode", EditMode == SkillNodeEditMode.Resize);
        EnableInClassList("info-mode", EditMode == SkillNodeEditMode.Info);
        UpdateRelationshipHighlights();
        NodeSelected?.Invoke(null);
        ConnectionSelected?.Invoke(null);
        ModeChanged?.Invoke(EditMode);
    }

    void DuplicateSelectedNodes()
    {
        var selectedNodes = GetSelectedNodes();
        if (selectedNodes.Count == 0)
            return;

        var guidMap = new Dictionary<string, string>();
        var duplicatedViews = new List<SkillNodeView>();
        foreach (var node in selectedNodes)
        {
            var duplicate = CloneNodeDraft(node.Draft);
            guidMap[node.Draft.guid] = duplicate.guid;
            m_Draft.nodes.Add(duplicate);
            duplicatedViews.Add(AddNodeView(duplicate));
        }

        var duplicatedConnections = new List<SkillConnectionDraftData>();
        foreach (var connection in m_Draft.connections)
        {
            if (!guidMap.TryGetValue(connection.parentNodeGuid, out var parentGuid)
                || !guidMap.TryGetValue(connection.childNodeGuid, out var childGuid))
            {
                continue;
            }

            duplicatedConnections.Add(CloneConnectionDraft(connection, parentGuid, childGuid));
        }

        foreach (var connection in duplicatedConnections)
        {
            m_Draft.connections.Add(connection);
            AddConnectionView(connection);
        }

        ClearSelection();
        m_SelectedNode = null;
        m_SelectedConnection = null;
        foreach (var node in duplicatedViews)
            AddToSelection(node);
        DraftChanged?.Invoke();
    }

    static SkillNodeDraftData CloneNodeDraft(SkillNodeDraftData source)
    {
        return new SkillNodeDraftData
        {
            guid = Guid.NewGuid().ToString("N"),
            nodeSpriteGuid = source.nodeSpriteGuid,
            nodeSpriteLocalId = source.nodeSpriteLocalId,
            nodeColor = source.nodeColor,
            skillSpriteGuid = source.skillSpriteGuid,
            skillSpriteLocalId = source.skillSpriteLocalId,
            skillColor = source.skillColor,
            skillSpriteSizeRatio = source.skillSpriteSizeRatio,
            price = source.price,
            effects = CloneEffects(source.effects),
            nodeInfo = new SkillNodeInfoDraftData
            {
                mainText = source.nodeInfo.mainText,
                fontGuid = source.nodeInfo.fontGuid,
                description = source.nodeInfo.description,
                statName = source.nodeInfo.statName,
                valueText = source.nodeInfo.valueText
            },
            position = source.position,
            size = source.size
        };
    }

    static List<SkillEffectDraftData> CloneEffects(
        IReadOnlyList<SkillEffectDraftData> source)
    {
        var effects = new List<SkillEffectDraftData>(source.Count);
        foreach (var effect in source)
        {
            effects.Add(new SkillEffectDraftData
            {
                effectType = effect.effectType,
                stat = effect.stat,
                ability = effect.ability,
                abilityProperty = effect.abilityProperty,
                operation = effect.operation,
                value = effect.value
            });
        }

        return effects;
    }

    static SkillConnectionDraftData CloneConnectionDraft(
        SkillConnectionDraftData source,
        string parentGuid,
        string childGuid)
    {
        return new SkillConnectionDraftData
        {
            color = source.color,
            spriteGuid = source.spriteGuid,
            spriteLocalId = source.spriteLocalId,
            repeatSprite = source.repeatSprite,
            parentNodeGuid = parentGuid,
            childNodeGuid = childGuid,
            startAnchor = source.startAnchor,
            endAnchor = source.endAnchor,
            routingMode = source.routingMode,
            bendPointCount = source.bendPointCount,
            hasCustomBendPointCount = source.hasCustomBendPointCount,
            pathPoints = new List<Vector2>(source.pathPoints)
        };
    }

    void DeleteSelectedElements()
    {
        var selectedNodes = new List<SkillNodeView>();
        var selectedConnections = new List<SkillConnectionView>();
        foreach (var selectable in selection)
        {
            if (selectable is SkillNodeView node)
                selectedNodes.Add(node);
            else if (selectable is SkillConnectionView connection)
                selectedConnections.Add(connection);
        }

        if (selectedNodes.Count == 0 && selectedConnections.Count == 0)
            return;

        foreach (var node in selectedNodes)
            DeleteNode(node, false);
        foreach (var connection in selectedConnections)
        {
            if (connection.Draft != null && m_Draft.connections.Contains(connection.Draft))
                DeleteConnection(connection, false);
        }

        m_SelectedNode = null;
        m_SelectedConnection = null;
        UpdateRelationshipHighlights();
        NodeSelected?.Invoke(null);
        ConnectionSelected?.Invoke(null);
        DraftChanged?.Invoke();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (EditMode == SkillNodeEditMode.Info)
        {
            if (m_SelectedNode != null)
                return;

            var node = GetNodeFromTarget(evt.target as VisualElement);
            if (node == null)
                m_InfoTooltip.Hide();
            else
                ShowHoverInfoTooltip(node, evt.position);
            return;
        }

        m_SnapToInteger = evt.ctrlKey;
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        Focus();
        m_SnapToInteger = evt.ctrlKey;
        m_NodeDragStartCenters.Clear();

        var target = evt.target as VisualElement;
        if (EditMode == SkillNodeEditMode.Info)
        {
            var tooltip = target as SkillNodeInfoTooltipView
                ?? target?.GetFirstAncestorOfType<SkillNodeInfoTooltipView>();
            if (tooltip != null)
                return;

            if (evt.button != 0)
                return;

            var infoNode = GetNodeFromTarget(target);
            if (infoNode != null)
            {
                if (!infoNode.IsNodeSelected)
                {
                    ClearSelection();
                    AddToSelection(infoNode);
                }
                else
                {
                    ShowSelectedInfoTooltip(infoNode);
                }
            }
            else
            {
                ClearSelection();
                m_SelectedNode = null;
                UpdateRelationshipHighlights();
                NodeSelected?.Invoke(null);
                m_InfoTooltip.Hide();
            }

            evt.StopImmediatePropagation();
            return;
        }

        var anchor = target as SkillNodeAnchorView
            ?? target?.GetFirstAncestorOfType<SkillNodeAnchorView>();
        m_SnapReferenceNode = anchor?.Node
            ?? target as SkillNodeView
            ?? target?.GetFirstAncestorOfType<SkillNodeView>();

        if (EditMode == SkillNodeEditMode.Resize
            && evt.button == 0
            && evt.ctrlKey
            && anchor == null
            && !target.ClassListContains("resize-handle")
            && m_SnapReferenceNode != null)
        {
            if (m_SnapReferenceNode.IsNodeSelected)
                RemoveFromSelection(m_SnapReferenceNode);
            else
                AddToSelection(m_SnapReferenceNode);
            evt.StopImmediatePropagation();
            return;
        }

        if (m_SnapReferenceNode == null)
            return;

        foreach (var node in m_NodeViews)
        {
            if (node.IsNodeSelected || node == m_SnapReferenceNode)
                m_NodeDragStartCenters[node] = node.GetPosition().center;
        }
    }

    Rect SnapNodePosition(SkillNodeView node, Rect newPosition)
    {
        if (!m_SnapToInteger)
            return newPosition;

        var step = Mathf.Max(1, SnapStep);
        if (m_SnapReferenceNode == null
            || !m_NodeDragStartCenters.TryGetValue(node, out var nodeStart)
            || !m_NodeDragStartCenters.TryGetValue(m_SnapReferenceNode, out var referenceStart))
        {
            var snappedCenter = new Vector2(
                Mathf.Round(newPosition.center.x / step) * step,
                Mathf.Round(newPosition.center.y / step) * step);
            newPosition.position = snappedCenter - newPosition.size * 0.5f;
            return newPosition;
        }

        var dragDelta = newPosition.center - nodeStart;
        var referencePosition = referenceStart + dragDelta;
        var snappedReference = new Vector2(
            Mathf.Round(referencePosition.x / step) * step,
            Mathf.Round(referencePosition.y / step) * step);
        newPosition.position += snappedReference - referencePosition;
        return newPosition;
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        m_SnapToInteger = false;
        m_SnapReferenceNode = null;
        m_NodeDragStartCenters.Clear();
        if (!m_HasPendingTransformChange)
            return;

        m_HasPendingTransformChange = false;
        DraftChanged?.Invoke();
    }

    void OnPointerLeave(PointerLeaveEvent evt)
    {
        if (EditMode == SkillNodeEditMode.Info && m_SelectedNode == null)
            m_InfoTooltip.Hide();
    }

    void CenterInitialView(GeometryChangedEvent evt)
    {
        if (evt.newRect.width <= 0f || evt.newRect.height <= 0f)
            return;

        UpdateViewTransform(
            new Vector3(evt.newRect.width * 0.5f, evt.newRect.height * 0.5f, 0f),
            Vector3.one);
        UnregisterCallback<GeometryChangedEvent>(CenterInitialView);
    }
}

sealed class SkillTreeCanvasElement : GraphElement
{
}

public sealed class SkillNodeView : Node
{
    readonly Image m_NodeImage;
    readonly Image m_SkillImage;
    readonly VisualElement m_RelationshipOutline;
    readonly Func<SkillNodeView, Rect, Rect> m_SnapPosition;
    readonly Func<int> m_SnapStep;
    readonly List<VisualElement> m_ResizeHandles = new();
    readonly List<SkillNodeAnchorView> m_Anchors = new();
    Rect m_Position;
    bool m_Initialized;
    bool m_IsSelected;
    bool m_ConnectionDragActive;
    SkillNodeEditMode m_EditMode;

    public SkillNodeDraftData Draft { get; }
    public bool IsNodeSelected => m_IsSelected;
    public IReadOnlyList<SkillNodeAnchorView> Anchors => m_Anchors;
    public float SnapStep => Mathf.Max(1, m_SnapStep());

    public event Action<SkillNodeView> Selected;
    public event Action<SkillNodeView> Unselected;
    public event Action<SkillNodeView> Changed;

    public SkillNodeView(
        SkillNodeDraftData draft,
        Func<float> zoomScale,
        Func<SkillNodeView, Rect, Rect> snapPosition,
        Func<int> snapStep,
        Func<List<SkillNodeView>> selectedNodes,
        Action<SkillNodeView, SkillNodeAnchor, Vector2> beginConnectionDrag,
        Action<Vector2> updateConnectionDrag,
        Action<SkillNodeAnchorView> endConnectionDrag)
    {
        Draft = draft;
        m_SnapPosition = snapPosition;
        m_SnapStep = snapStep;
        viewDataKey = draft.guid;
        capabilities = Capabilities.Selectable | Capabilities.Movable;
        AddToClassList("skill-node");

        mainContainer.Clear();
        mainContainer.name = "skill-node-main-container";
        mainContainer.AddToClassList("skill-node-main");

        var visual = new VisualElement();
        visual.AddToClassList("skill-node-visual");
        mainContainer.Add(visual);

        m_NodeImage = new Image
        {
            scaleMode = ScaleMode.StretchToFill,
            pickingMode = PickingMode.Ignore
        };
        m_NodeImage.AddToClassList("node-image");
        visual.Add(m_NodeImage);

        m_SkillImage = new Image
        {
            scaleMode = ScaleMode.StretchToFill,
            pickingMode = PickingMode.Ignore
        };
        m_SkillImage.AddToClassList("skill-image");
        visual.Add(m_SkillImage);

        m_RelationshipOutline = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        m_RelationshipOutline.AddToClassList("related-node-outline");
        foreach (var cornerClass in new[]
                 {
                     "related-node-top-left",
                     "related-node-top-right",
                     "related-node-bottom-left",
                     "related-node-bottom-right"
                 })
        {
            var corner = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };
            corner.AddToClassList("related-node-corner");
            corner.AddToClassList(cornerClass);
            m_RelationshipOutline.Add(corner);
        }
        visual.Add(m_RelationshipOutline);

        AddResizeHandle(visual, SkillNodeResizeCorner.TopLeft, zoomScale, selectedNodes);
        AddResizeHandle(visual, SkillNodeResizeCorner.TopRight, zoomScale, selectedNodes);
        AddResizeHandle(visual, SkillNodeResizeCorner.BottomLeft, zoomScale, selectedNodes);
        AddResizeHandle(visual, SkillNodeResizeCorner.BottomRight, zoomScale, selectedNodes);
        AddAnchor(SkillNodeAnchor.Top, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);
        AddAnchor(SkillNodeAnchor.Right, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);
        AddAnchor(SkillNodeAnchor.Bottom, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);
        AddAnchor(SkillNodeAnchor.Left, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);

        RefreshVisuals();
        m_Position = new Rect(draft.position - draft.size * 0.5f, draft.size);
        base.SetPosition(m_Position);
        style.width = draft.size.x;
        style.height = draft.size.y;
        UpdateAnchorPositions();
        m_Initialized = true;
    }

    public override Rect GetPosition()
    {
        return m_Position;
    }

    public override void SetPosition(Rect newPos)
    {
        newPos.width = Mathf.Max(0f, newPos.width);
        newPos.height = Mathf.Max(0f, newPos.height);
        if (m_Initialized && m_EditMode == SkillNodeEditMode.Move)
            newPos = m_SnapPosition(this, newPos);
        m_Position = newPos;
        base.SetPosition(newPos);
        style.width = newPos.width;
        style.height = newPos.height;
        UpdateAnchorPositions();

        if (!m_Initialized)
            return;

        Draft.position = newPos.center;
        Draft.size = newPos.size;
        Changed?.Invoke(this);
    }

    public override void OnSelected()
    {
        base.OnSelected();
        m_IsSelected = true;
        UpdateResizeHandles();
        UpdateAnchors();
        Selected?.Invoke(this);
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
        m_IsSelected = false;
        UpdateResizeHandles();
        UpdateAnchors();
        Unselected?.Invoke(this);
    }

    public void SetEditMode(SkillNodeEditMode mode)
    {
        m_EditMode = mode;
        if (mode == SkillNodeEditMode.Move)
            capabilities |= Capabilities.Movable;
        else
            capabilities &= ~Capabilities.Movable;

        UpdateResizeHandles();
        UpdateAnchors();
    }

    public void SetConnectionDragActive(bool isActive)
    {
        m_ConnectionDragActive = isActive;
        UpdateAnchors();
    }

    public void SetRelationshipHighlight(bool isParent, bool isChild)
    {
        m_RelationshipOutline.EnableInClassList(
            "related-node-outline-active",
            isParent || isChild);
        m_RelationshipOutline.EnableInClassList("related-node-parent", isParent);
        m_RelationshipOutline.EnableInClassList("related-node-child", isChild);
    }

    public void RefreshVisuals()
    {
        var nodeSprite = SkillTreeAssetReferenceUtility.Load<Sprite>(
            Draft.nodeSpriteGuid,
            Draft.nodeSpriteLocalId);
        var skillSprite = SkillTreeAssetReferenceUtility.Load<Sprite>(
            Draft.skillSpriteGuid,
            Draft.skillSpriteLocalId);

        m_NodeImage.sprite = nodeSprite;
        m_NodeImage.tintColor = Draft.nodeColor;
        m_NodeImage.style.backgroundColor = Color.clear;

        m_SkillImage.sprite = skillSprite;
        m_SkillImage.tintColor = Draft.skillColor;
        m_SkillImage.style.backgroundColor = Color.clear;

        var ratio = new Vector2(
            Mathf.Clamp01(Draft.skillSpriteSizeRatio.x),
            Mathf.Clamp01(Draft.skillSpriteSizeRatio.y));
        var horizontalMargin = (1f - ratio.x) * 50f;
        var verticalMargin = (1f - ratio.y) * 50f;
        m_SkillImage.style.left = Length.Percent(horizontalMargin);
        m_SkillImage.style.right = Length.Percent(horizontalMargin);
        m_SkillImage.style.top = Length.Percent(verticalMargin);
        m_SkillImage.style.bottom = Length.Percent(verticalMargin);
    }

    void AddResizeHandle(
        VisualElement visual,
        SkillNodeResizeCorner corner,
        Func<float> zoomScale,
        Func<List<SkillNodeView>> selectedNodes)
    {
        var handle = new VisualElement();
        handle.AddToClassList("resize-handle");
        handle.AddToClassList(corner switch
        {
            SkillNodeResizeCorner.TopLeft => "resize-top-left",
            SkillNodeResizeCorner.TopRight => "resize-top-right",
            SkillNodeResizeCorner.BottomLeft => "resize-bottom-left",
            _ => "resize-bottom-right"
        });
        handle.AddManipulator(new SkillNodeResizeManipulator(
            this,
            corner,
            zoomScale,
            selectedNodes));
        m_ResizeHandles.Add(handle);
        visual.Add(handle);
    }

    void AddAnchor(
        SkillNodeAnchor anchor,
        Action<SkillNodeView, SkillNodeAnchor, Vector2> beginConnectionDrag,
        Action<Vector2> updateConnectionDrag,
        Action<SkillNodeAnchorView> endConnectionDrag)
    {
        var anchorView = new SkillNodeAnchorView(
            this,
            anchor,
            beginConnectionDrag,
            updateConnectionDrag,
            endConnectionDrag);
        m_Anchors.Add(anchorView);
    }

    public void UpdateAnchorPositions()
    {
        foreach (var anchor in m_Anchors)
            anchor.SetNodeRect(m_Position);
    }

    void UpdateResizeHandles()
    {
        var isActive = m_EditMode == SkillNodeEditMode.Resize && m_IsSelected;
        EnableInClassList("resize-node-selected", isActive);
        foreach (var handle in m_ResizeHandles)
            handle.EnableInClassList("resize-handle-active", isActive);
    }

    void UpdateAnchors()
    {
        var isActive = m_EditMode == SkillNodeEditMode.Move
            && (m_IsSelected || m_ConnectionDragActive);
        foreach (var anchor in m_Anchors)
            anchor.EnableInClassList("node-anchor-active", isActive);
    }
}

sealed class SkillNodeResizeManipulator : PointerManipulator
{
    readonly SkillNodeView m_Node;
    readonly SkillNodeResizeCorner m_Corner;
    readonly Func<float> m_ZoomScale;
    readonly Func<List<SkillNodeView>> m_SelectedNodes;
    readonly List<ResizeState> m_ResizeStates = new();
    int m_PointerId;
    Vector2 m_StartPointerPosition;
    Rect m_StartRect;

    public SkillNodeResizeManipulator(
        SkillNodeView node,
        SkillNodeResizeCorner corner,
        Func<float> zoomScale,
        Func<List<SkillNodeView>> selectedNodes)
    {
        m_Node = node;
        m_Corner = corner;
        m_ZoomScale = zoomScale;
        m_SelectedNodes = selectedNodes;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || !target.ClassListContains("resize-handle-active"))
            return;

        m_PointerId = evt.pointerId;
        m_StartPointerPosition = evt.position;
        m_StartRect = m_Node.GetPosition();
        m_ResizeStates.Clear();
        foreach (var node in m_SelectedNodes())
            m_ResizeStates.Add(new ResizeState(node, node.GetPosition()));

        if (m_ResizeStates.Count == 0)
            m_ResizeStates.Add(new ResizeState(m_Node, m_StartRect));

        target.CapturePointer(m_PointerId);
        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!target.HasPointerCapture(m_PointerId))
            return;

        var scale = Mathf.Max(0.01f, m_ZoomScale());
        var delta = ((Vector2)evt.position - m_StartPointerPosition) / scale;
        var resizeScale = SkillNodeResizeMath.CalculateScale(
            m_StartRect,
            m_Corner,
            delta,
            evt.ctrlKey ? m_Node.SnapStep : 0f);

        foreach (var state in m_ResizeStates)
            state.node.SetPosition(SkillNodeResizeMath.ScaleFromOppositeCorner(
                state.rect,
                m_Corner,
                resizeScale,
                evt.ctrlKey ? state.node.SnapStep : 0f));

        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (!target.HasPointerCapture(m_PointerId))
            return;

        target.ReleasePointer(m_PointerId);
        m_ResizeStates.Clear();
        evt.StopPropagation();
    }

    readonly struct ResizeState
    {
        public readonly SkillNodeView node;
        public readonly Rect rect;

        public ResizeState(SkillNodeView node, Rect rect)
        {
            this.node = node;
            this.rect = rect;
        }
    }
}

public static class SkillNodeResizeMath
{
    public static Vector2 CalculateScale(
        Rect anchorRect,
        SkillNodeResizeCorner corner,
        Vector2 delta,
        float snapStep = 0f)
    {
        var widthDelta = corner is SkillNodeResizeCorner.TopLeft or SkillNodeResizeCorner.BottomLeft
            ? -delta.x
            : delta.x;
        var heightDelta = corner is SkillNodeResizeCorner.TopLeft or SkillNodeResizeCorner.TopRight
            ? -delta.y
            : delta.y;
        var width = Mathf.Max(0f, anchorRect.width + widthDelta);
        var height = Mathf.Max(0f, anchorRect.height + heightDelta);
        if (snapStep > 0f)
        {
            width = Mathf.Round(width / snapStep) * snapStep;
            height = Mathf.Round(height / snapStep) * snapStep;
        }
        return new Vector2(width / anchorRect.width, height / anchorRect.height);
    }

    public static Rect ScaleFromOppositeCorner(
        Rect rect,
        SkillNodeResizeCorner corner,
        Vector2 resizeScale,
        float snapStep = 0f)
    {
        var size = new Vector2(
            Mathf.Max(0f, rect.width * resizeScale.x),
            Mathf.Max(0f, rect.height * resizeScale.y));
        if (snapStep > 0f)
        {
            size.x = Mathf.Round(size.x / snapStep) * snapStep;
            size.y = Mathf.Round(size.y / snapStep) * snapStep;
        }

        var position = corner switch
        {
            SkillNodeResizeCorner.TopLeft => new Vector2(rect.xMax - size.x, rect.yMax - size.y),
            SkillNodeResizeCorner.TopRight => new Vector2(rect.xMin, rect.yMax - size.y),
            SkillNodeResizeCorner.BottomLeft => new Vector2(rect.xMax - size.x, rect.yMin),
            _ => rect.position
        };
        return new Rect(position, size);
    }
}

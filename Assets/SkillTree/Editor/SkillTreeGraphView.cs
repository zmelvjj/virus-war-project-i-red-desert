using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public enum SkillNodeEditMode
{
    Move,
    Resize
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

    readonly SkillTreeDraftData m_Draft;
    readonly List<SkillNodeView> m_NodeViews = new();
    readonly Dictionary<SkillConnectionDraftData, SkillConnectionView> m_ConnectionViews = new();
    SkillTreeCanvasElement m_BaseCanvas;
    SkillNodeView m_SelectedNode;
    SkillConnectionView m_SelectedConnection;
    bool m_HasPendingTransformChange;
    SkillNodeView m_ConnectionStartNode;
    SkillNodeAnchor m_ConnectionStartAnchor;
    SkillConnectionView m_ConnectionPreview;

    public event Action<SkillNodeView> NodeSelected;
    public event Action<SkillConnectionView> ConnectionSelected;
    public event Action<SkillNodeView> NodeChanged;
    public event Action DraftChanged;
    public event Action<SkillNodeEditMode> ModeChanged;

    public SkillNodeEditMode EditMode { get; private set; } = SkillNodeEditMode.Move;

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
        RegisterCallback<PointerDownEvent>(_ => Focus(), TrickleDown.TrickleDown);
        RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        RegisterCallback<GeometryChangedEvent>(CenterInitialView);

        foreach (var node in draft.nodes)
            AddNodeView(node);

        foreach (var connection in draft.connections)
        {
            if (connection.pathPoints == null || connection.pathPoints.Count < 2)
                RouteConnection(connection);
            AddConnectionView(connection);
        }
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        var target = evt.target as VisualElement;
        var connection = target as SkillConnectionView
            ?? target?.GetFirstAncestorOfType<SkillConnectionView>();
        if (connection != null && connection.Draft != null)
        {
            evt.menu.AppendAction("Delete Connection", _ => DeleteConnection(connection));
            if (connection.Draft.routingMode == SkillConnectionRoutingMode.Manual)
                evt.menu.AppendAction("Auto Route", _ => SetAutoRoute(connection));
            return;
        }

        var node = target as SkillNodeView ?? target?.GetFirstAncestorOfType<SkillNodeView>();
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

    void AddNodeView(SkillNodeDraftData draft)
    {
        var node = new SkillNodeView(
            draft,
            () => scale,
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
    }

    void DeleteNode(SkillNodeView node)
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
        RemoveElement(node);
        if (wasInspectorNode)
            SelectFirstNodeForInspector();
        DraftChanged?.Invoke();
    }

    void OnNodeSelected(SkillNodeView node)
    {
        if (m_SelectedConnection != null)
        {
            m_SelectedConnection = null;
            ConnectionSelected?.Invoke(null);
        }

        if (m_SelectedNode != null)
            return;

        m_SelectedNode = node;
        NodeSelected?.Invoke(node);
    }

    void OnNodeUnselected(SkillNodeView node)
    {
        if (m_SelectedNode != node)
            return;

        m_SelectedNode = null;
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
            NodeSelected?.Invoke(node);
            return;
        }

        NodeSelected?.Invoke(null);
    }

    void AddConnectionView(SkillConnectionDraftData draft)
    {
        var view = new SkillConnectionView(draft, () => scale, SelectConnection);
        view.PathChanged += RefreshManualConnection;
        view.PathEditCompleted += _ => DraftChanged?.Invoke();
        view.Deselected += OnConnectionUnselected;
        m_ConnectionViews[draft] = view;
        AddElement(view);
        view.SendToBack();
        m_BaseCanvas.SendToBack();
        RefreshManualConnection(view);
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

        m_ConnectionPreview = new SkillConnectionView(null, () => scale, null, true);
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
        m_ConnectionPreview.SetPreviewPath(SmartOrthogonalRouter.Preview(
            m_ConnectionStartNode.GetPosition(),
            m_ConnectionStartAnchor,
            pointerPosition));
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

    void SelectConnection(SkillConnectionView connection)
    {
        ClearSelection();
        AddToSelection(connection);
        m_SelectedConnection = connection;
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

    void DeleteConnection(SkillConnectionView connection)
    {
        if (m_SelectedConnection == connection)
        {
            m_SelectedConnection = null;
            ConnectionSelected?.Invoke(null);
        }

        RemoveConnection(connection.Draft);
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
    }

    void SetAutoRoute(SkillConnectionView connection)
    {
        connection.Draft.routingMode = SkillConnectionRoutingMode.Auto;
        connection.Draft.hasCustomBendPointCount = false;
        connection.Draft.bendPointCount = -1;
        RouteConnection(connection.Draft);
        connection.RefreshLayout();
        connection.SetInvalid(false);
        ConnectionSelected?.Invoke(connection);
        DraftChanged?.Invoke();
    }

    void RouteConnection(SkillConnectionDraftData connection)
    {
        var parent = FindNode(connection.parentNodeGuid);
        var child = FindNode(connection.childNodeGuid);
        if (parent == null || child == null)
            return;

        var obstacles = GetObstacleRects(parent, child);
        var existingConnections = new List<SkillConnectionDraftData>();
        foreach (var existing in m_Draft.connections)
        {
            if (existing != connection)
                existingConnections.Add(existing);
        }

        var path = SmartOrthogonalRouter.Route(
            parent.GetPosition(),
            connection.startAnchor,
            child.GetPosition(),
            connection.endAnchor,
            obstacles,
            existingConnections,
            connection.hasCustomBendPointCount ? connection.bendPointCount : -1);
        connection.pathPoints.Clear();
        connection.pathPoints.AddRange(path);
        connection.bendPointCount = Mathf.Max(0, path.Count - 2);
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
        connection.SetInvalid(false);
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
            connection.Draft.bendPointCount = Mathf.Max(0, connection.Draft.pathPoints.Count - 2);

        if (connection.Draft == null
            || connection.Draft.routingMode == SkillConnectionRoutingMode.Auto)
        {
            connection.SetInvalid(false);
            if (m_SelectedConnection == connection)
                ConnectionSelected?.Invoke(connection);
            return;
        }

        var parent = FindNode(connection.Draft.parentNodeGuid);
        var child = FindNode(connection.Draft.childNodeGuid);
        connection.SetInvalid(SmartOrthogonalRouter.PathIntersectsObstacles(
            connection.Draft.pathPoints,
            GetObstacleRects(parent, child)));
        if (m_SelectedConnection == connection)
            ConnectionSelected?.Invoke(connection);
    }

    List<Rect> GetObstacleRects(SkillNodeView excludedA, SkillNodeView excludedB)
    {
        var obstacles = new List<Rect>();
        foreach (var node in m_NodeViews)
        {
            if (node == excludedA || node == excludedB)
                continue;

            var rect = node.GetPosition();
            rect.xMin -= SmartOrthogonalRouter.ObstaclePadding;
            rect.xMax += SmartOrthogonalRouter.ObstaclePadding;
            rect.yMin -= SmartOrthogonalRouter.ObstaclePadding;
            rect.yMax += SmartOrthogonalRouter.ObstaclePadding;
            obstacles.Add(rect);
        }

        return obstacles;
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
        if (evt.keyCode != KeyCode.S)
            return;

        EditMode = EditMode == SkillNodeEditMode.Move
            ? SkillNodeEditMode.Resize
            : SkillNodeEditMode.Move;

        foreach (var node in m_NodeViews)
            node.SetEditMode(EditMode);

        EnableInClassList("resize-mode", EditMode == SkillNodeEditMode.Resize);
        ModeChanged?.Invoke(EditMode);
        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (!m_HasPendingTransformChange)
            return;

        m_HasPendingTransformChange = false;
        DraftChanged?.Invoke();
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
    readonly List<VisualElement> m_ResizeHandles = new();
    readonly List<SkillNodeAnchorView> m_Anchors = new();
    Rect m_Position;
    bool m_Initialized;
    bool m_IsSelected;
    bool m_IsHovered;
    bool m_ConnectionDragActive;
    SkillNodeEditMode m_EditMode;

    public SkillNodeDraftData Draft { get; }
    public bool IsNodeSelected => m_IsSelected;

    public event Action<SkillNodeView> Selected;
    public event Action<SkillNodeView> Unselected;
    public event Action<SkillNodeView> Changed;

    public SkillNodeView(
        SkillNodeDraftData draft,
        Func<float> zoomScale,
        Func<List<SkillNodeView>> selectedNodes,
        Action<SkillNodeView, SkillNodeAnchor, Vector2> beginConnectionDrag,
        Action<Vector2> updateConnectionDrag,
        Action<SkillNodeAnchorView> endConnectionDrag)
    {
        Draft = draft;
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

        AddResizeHandle(visual, SkillNodeResizeCorner.TopLeft, zoomScale, selectedNodes);
        AddResizeHandle(visual, SkillNodeResizeCorner.TopRight, zoomScale, selectedNodes);
        AddResizeHandle(visual, SkillNodeResizeCorner.BottomLeft, zoomScale, selectedNodes);
        AddResizeHandle(visual, SkillNodeResizeCorner.BottomRight, zoomScale, selectedNodes);
        AddAnchor(visual, SkillNodeAnchor.Top, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);
        AddAnchor(visual, SkillNodeAnchor.Right, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);
        AddAnchor(visual, SkillNodeAnchor.Bottom, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);
        AddAnchor(visual, SkillNodeAnchor.Left, beginConnectionDrag, updateConnectionDrag, endConnectionDrag);

        RegisterCallback<PointerEnterEvent>(_ =>
        {
            m_IsHovered = true;
            UpdateAnchors();
        });
        RegisterCallback<PointerLeaveEvent>(_ =>
        {
            m_IsHovered = false;
            UpdateAnchors();
        });

        RefreshVisuals();
        m_Position = new Rect(draft.position - draft.size * 0.5f, draft.size);
        base.SetPosition(m_Position);
        style.width = draft.size.x;
        style.height = draft.size.y;
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
        m_Position = newPos;
        base.SetPosition(newPos);
        style.width = newPos.width;
        style.height = newPos.height;

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
        VisualElement visual,
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
        visual.Add(anchorView);
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
            && (m_IsSelected || m_IsHovered || m_ConnectionDragActive);
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
            delta);

        foreach (var state in m_ResizeStates)
            state.node.SetPosition(SkillNodeResizeMath.ScaleAroundCenter(
                state.rect,
                resizeScale));

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
        Vector2 delta)
    {
        var widthDelta = corner is SkillNodeResizeCorner.TopLeft or SkillNodeResizeCorner.BottomLeft
            ? -delta.x * 2f
            : delta.x * 2f;
        var heightDelta = corner is SkillNodeResizeCorner.TopLeft or SkillNodeResizeCorner.TopRight
            ? -delta.y * 2f
            : delta.y * 2f;
        var width = Mathf.Max(0f, anchorRect.width + widthDelta);
        var height = Mathf.Max(0f, anchorRect.height + heightDelta);
        return new Vector2(width / anchorRect.width, height / anchorRect.height);
    }

    public static Rect ScaleAroundCenter(Rect rect, Vector2 resizeScale)
    {
        var size = new Vector2(
            Mathf.Max(0f, rect.width * resizeScale.x),
            Mathf.Max(0f, rect.height * resizeScale.y));
        return new Rect(rect.center - size * 0.5f, size);
    }
}

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillConnectionView : GraphElement
{
    const float k_HitThickness = 12f;
    const float k_BoundsPadding = 10f;

    readonly Func<float> m_ZoomScale;
    readonly Action<SkillConnectionView> m_SelectRequested;
    readonly bool m_IsPreview;
    readonly List<Vector2> m_PreviewPoints = new();
    Rect m_PathBounds;
    bool m_IsInvalid;

    public SkillConnectionDraftData Draft { get; }
    public IReadOnlyList<Vector2> Points => Draft != null ? Draft.pathPoints : m_PreviewPoints;

    public event Action<SkillConnectionView> PathChanged;
    public event Action<SkillConnectionView> PathEditCompleted;
    public event Action<SkillConnectionView> Deselected;

    public SkillConnectionView(
        SkillConnectionDraftData draft,
        Func<float> zoomScale,
        Action<SkillConnectionView> selectRequested,
        bool isPreview = false)
    {
        Draft = draft;
        m_ZoomScale = zoomScale;
        m_SelectRequested = selectRequested;
        m_IsPreview = isPreview;
        capabilities = isPreview ? 0 : Capabilities.Selectable | Capabilities.Deletable;
        pickingMode = PickingMode.Ignore;
        AddToClassList("skill-connection");
        if (isPreview)
            AddToClassList("connection-preview");

        generateVisualContent += DrawConnection;
        RefreshLayout();
    }

    public void SetPreviewPath(IReadOnlyList<Vector2> points)
    {
        m_PreviewPoints.Clear();
        m_PreviewPoints.AddRange(points);
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        Clear();
        var points = Points;
        if (points.Count < 2)
            return;

        m_PathBounds = CalculateBounds(points);
        base.SetPosition(m_PathBounds);

        if (!m_IsPreview)
        {
            for (var i = 0; i < points.Count - 1; i++)
                AddSegmentHitTarget(i, points[i] - m_PathBounds.position, points[i + 1] - m_PathBounds.position);
        }

        MarkDirtyRepaint();
    }

    public void SetInvalid(bool isInvalid)
    {
        m_IsInvalid = isInvalid;
        EnableInClassList("connection-invalid", isInvalid);
        MarkDirtyRepaint();
    }

    public override void OnSelected()
    {
        base.OnSelected();
        MarkDirtyRepaint();
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
        MarkDirtyRepaint();
        Deselected?.Invoke(this);
    }

    void AddSegmentHitTarget(int segmentIndex, Vector2 start, Vector2 end)
    {
        var segment = new VisualElement
        {
            pickingMode = PickingMode.Position
        };
        segment.AddToClassList("connection-segment-hit");

        var horizontal = Mathf.Abs(start.y - end.y) < 0.01f;
        if (horizontal)
        {
            segment.style.left = Mathf.Min(start.x, end.x);
            segment.style.top = start.y - k_HitThickness * 0.5f;
            segment.style.width = Mathf.Max(1f, Mathf.Abs(end.x - start.x));
            segment.style.height = k_HitThickness;
        }
        else
        {
            segment.style.left = start.x - k_HitThickness * 0.5f;
            segment.style.top = Mathf.Min(start.y, end.y);
            segment.style.width = k_HitThickness;
            segment.style.height = Mathf.Max(1f, Mathf.Abs(end.y - start.y));
        }

        segment.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button == 0)
                m_SelectRequested?.Invoke(this);
        });

        if (segmentIndex > 0 && segmentIndex < Points.Count - 2)
        {
            segment.AddManipulator(new SkillConnectionSegmentManipulator(
                this,
                segmentIndex,
                m_ZoomScale));
        }

        Add(segment);
    }

    void MoveSegment(int segmentIndex, IReadOnlyList<Vector2> startPoints, Vector2 delta)
    {
        Draft.pathPoints.Clear();
        Draft.pathPoints.AddRange(startPoints);

        var first = Draft.pathPoints[segmentIndex];
        var second = Draft.pathPoints[segmentIndex + 1];
        if (Mathf.Abs(first.y - second.y) < 0.01f)
        {
            first.y += delta.y;
            second.y += delta.y;
        }
        else
        {
            first.x += delta.x;
            second.x += delta.x;
        }

        Draft.pathPoints[segmentIndex] = first;
        Draft.pathPoints[segmentIndex + 1] = second;
        Draft.routingMode = SkillConnectionRoutingMode.Manual;
        PathChanged?.Invoke(this);
        MarkDirtyRepaint();
    }

    void CompleteSegmentEdit()
    {
        RefreshLayout();
        PathEditCompleted?.Invoke(this);
    }

    void DrawConnection(MeshGenerationContext context)
    {
        var points = Points;
        if (points.Count < 2)
            return;

        var painter = context.painter2D;
        painter.lineWidth = selected ? 3.5f : 2.5f;
        painter.strokeColor = m_IsInvalid
            ? new Color(0.9f, 0.25f, 0.22f)
            : m_IsPreview
                ? new Color(0.35f, 0.72f, 0.95f, 0.75f)
                : selected
                    ? new Color(0.35f, 0.72f, 0.95f)
                    : new Color(0.68f, 0.72f, 0.78f);
        painter.BeginPath();
        painter.MoveTo(points[0] - m_PathBounds.position);
        for (var i = 1; i < points.Count; i++)
            painter.LineTo(points[i] - m_PathBounds.position);
        painter.Stroke();
    }

    static Rect CalculateBounds(IReadOnlyList<Vector2> points)
    {
        var min = points[0];
        var max = points[0];
        for (var i = 1; i < points.Count; i++)
        {
            min = Vector2.Min(min, points[i]);
            max = Vector2.Max(max, points[i]);
        }

        min -= Vector2.one * k_BoundsPadding;
        max += Vector2.one * k_BoundsPadding;
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    sealed class SkillConnectionSegmentManipulator : PointerManipulator
    {
        readonly SkillConnectionView m_Connection;
        readonly int m_SegmentIndex;
        readonly Func<float> m_ZoomScale;
        readonly List<Vector2> m_StartPoints = new();
        int m_PointerId;
        Vector2 m_StartPointerPosition;

        public SkillConnectionSegmentManipulator(
            SkillConnectionView connection,
            int segmentIndex,
            Func<float> zoomScale)
        {
            m_Connection = connection;
            m_SegmentIndex = segmentIndex;
            m_ZoomScale = zoomScale;
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
            if (evt.button != 0)
                return;

            m_PointerId = evt.pointerId;
            m_StartPointerPosition = evt.position;
            m_StartPoints.Clear();
            m_StartPoints.AddRange(m_Connection.Draft.pathPoints);
            target.CapturePointer(m_PointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (!target.HasPointerCapture(m_PointerId))
                return;

            var scale = Mathf.Max(0.01f, m_ZoomScale());
            var delta = ((Vector2)evt.position - m_StartPointerPosition) / scale;
            m_Connection.MoveSegment(m_SegmentIndex, m_StartPoints, delta);
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!target.HasPointerCapture(m_PointerId))
                return;

            target.ReleasePointer(m_PointerId);
            m_Connection.CompleteSegmentEdit();
            evt.StopPropagation();
        }
    }
}

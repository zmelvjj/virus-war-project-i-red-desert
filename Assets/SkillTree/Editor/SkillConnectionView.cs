using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillConnectionView : GraphElement
{
    const float k_LineThickness = 3f;
    const float k_HitThickness = 12f;
    const float k_BoundsPadding = 10f;

    readonly Func<float> m_ZoomScale;
    readonly Func<float> m_SnapStep;
    readonly Action<SkillConnectionView, int> m_SelectRequested;
    readonly bool m_IsPreview;
    readonly List<Vector2> m_PreviewPoints = new();
    readonly List<VisualElement> m_SegmentHitTargets = new();
    Rect m_PathBounds;
    bool m_IsSelected;
    bool m_HasTemporaryColor;
    Color m_TemporaryColor;

    public SkillConnectionDraftData Draft { get; }
    public IReadOnlyList<Vector2> Points => Draft != null ? Draft.pathPoints : m_PreviewPoints;
    public int SelectedSegmentIndex { get; private set; } = -1;
    public int MinimumBendPointCount { get; private set; }

    public event Action<SkillConnectionView> PathChanged;
    public event Action<SkillConnectionView> PathEditCompleted;
    public event Action<SkillConnectionView> Deselected;

    public SkillConnectionView(
        SkillConnectionDraftData draft,
        Func<float> zoomScale,
        Func<float> snapStep,
        Action<SkillConnectionView, int> selectRequested,
        bool isPreview = false)
    {
        Draft = draft;
        m_ZoomScale = zoomScale;
        m_SnapStep = snapStep;
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
        m_SegmentHitTargets.Clear();
        var points = Points;
        if (points.Count < 2)
            return;
        if (SelectedSegmentIndex >= points.Count - 1)
            SelectedSegmentIndex = -1;

        m_PathBounds = CalculateBounds(points);
        base.SetPosition(m_PathBounds);

        var sprite = Draft == null
            ? null
            : SkillTreeAssetReferenceUtility.Load<Sprite>(Draft.spriteGuid, Draft.spriteLocalId);
        if (sprite != null)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                AddSpriteSegment(
                    points[i] - m_PathBounds.position,
                    points[i + 1] - m_PathBounds.position,
                    sprite);
            }
        }

        if (!m_IsPreview)
        {
            for (var i = 0; i < points.Count - 1; i++)
                AddSegmentHitTarget(i, points[i] - m_PathBounds.position, points[i + 1] - m_PathBounds.position);
        }

        UpdateSegmentSelection();
        MarkDirtyRepaint();
    }

    void AddSpriteSegment(Vector2 start, Vector2 end, Sprite sprite)
    {
        var length = Vector2.Distance(start, end) + k_LineThickness;
        var midpoint = (start + end) * 0.5f;
        var horizontal = Mathf.Abs(start.x - end.x) >= Mathf.Abs(start.y - end.y);
        var strip = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        strip.style.position = Position.Absolute;
        strip.style.left = midpoint.x - length * 0.5f;
        strip.style.top = midpoint.y - k_LineThickness * 0.5f;
        strip.style.width = length;
        strip.style.height = k_LineThickness;
        strip.style.overflow = Overflow.Hidden;
        if (!horizontal)
            strip.style.rotate = new Rotate(Angle.Degrees(90f));

        if (Draft.repeatSprite)
        {
            var tileLength = Mathf.Max(
                1f,
                k_LineThickness * sprite.rect.width / sprite.rect.height);
            for (var x = 0f; x < length; x += tileLength)
                strip.Add(CreateSpriteImage(sprite, x, tileLength));
        }
        else
        {
            strip.Add(CreateSpriteImage(sprite, 0f, length));
        }

        Add(strip);
    }

    UnityEngine.UIElements.Image CreateSpriteImage(
        Sprite sprite,
        float left,
        float width)
    {
        var image = new UnityEngine.UIElements.Image
        {
            sprite = sprite,
            tintColor = GetDisplayColor(),
            scaleMode = ScaleMode.StretchToFill,
            pickingMode = PickingMode.Ignore
        };
        image.style.position = Position.Absolute;
        image.style.left = left;
        image.style.top = 0f;
        image.style.width = width;
        image.style.height = k_LineThickness;
        return image;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        m_IsSelected = true;
        UpdateSegmentSelection();
        MarkDirtyRepaint();
    }

    public override void OnUnselected()
    {
        base.OnUnselected();
        m_IsSelected = false;
        SelectedSegmentIndex = -1;
        UpdateSegmentSelection();
        MarkDirtyRepaint();
        Deselected?.Invoke(this);
    }

    public void SetMinimumBendPointCount(int minimum)
    {
        MinimumBendPointCount = minimum;
    }

    public void SetTemporaryColor(Color? color)
    {
        var hasColor = color.HasValue;
        if (m_HasTemporaryColor == hasColor
            && (!hasColor || m_TemporaryColor == color.Value))
        {
            return;
        }

        m_HasTemporaryColor = hasColor;
        if (hasColor)
            m_TemporaryColor = color.Value;
        RefreshLayout();
    }

    public bool CanMoveSegment(int segmentIndex)
    {
        return segmentIndex > 0 && segmentIndex < Points.Count - 2;
    }

    public bool IsSegmentHorizontal(int segmentIndex)
    {
        return segmentIndex >= 0
            && segmentIndex < Points.Count - 1
            && Mathf.Abs(Points[segmentIndex].y - Points[segmentIndex + 1].y) < 0.01f;
    }

    public float GetSegmentCoordinate(int segmentIndex)
    {
        return IsSegmentHorizontal(segmentIndex)
            ? Points[segmentIndex].y
            : Points[segmentIndex].x;
    }

    public void SetSelectedSegmentCoordinate(float coordinate)
    {
        if (!CanMoveSegment(SelectedSegmentIndex))
            return;

        var startPoints = new List<Vector2>(Draft.pathPoints);
        var first = startPoints[SelectedSegmentIndex];
        var delta = IsSegmentHorizontal(SelectedSegmentIndex)
            ? new Vector2(0f, coordinate - first.y)
            : new Vector2(coordinate - first.x, 0f);
        MoveSegment(SelectedSegmentIndex, startPoints, delta);
        CompleteSegmentEdit();
    }

    public void SelectSegment(int segmentIndex)
    {
        SelectedSegmentIndex = segmentIndex;
        UpdateSegmentSelection();
    }

    void AddSegmentHitTarget(int segmentIndex, Vector2 start, Vector2 end)
    {
        var segment = new VisualElement
        {
            pickingMode = PickingMode.Position
        };
        segment.AddToClassList("connection-segment-hit");

        var horizontal = Mathf.Abs(start.y - end.y) < 0.01f;
        segment.AddToClassList(horizontal ? "connection-segment-horizontal" : "connection-segment-vertical");
        var highlight = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        highlight.AddToClassList("connection-segment-highlight");
        segment.Add(highlight);
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

        if (CanMoveSegment(segmentIndex))
        {
            segment.AddManipulator(new SkillConnectionSegmentManipulator(
                this,
                segmentIndex,
                m_ZoomScale,
                m_SnapStep));
        }
        else
        {
            segment.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button == 0)
                {
                    RequestSelection(segmentIndex);
                    evt.StopPropagation();
                }
            });
        }

        m_SegmentHitTargets.Add(segment);
        Add(segment);
    }

    void UpdateSegmentSelection()
    {
        for (var i = 0; i < m_SegmentHitTargets.Count; i++)
        {
            m_SegmentHitTargets[i].EnableInClassList(
                "connection-segment-selected",
                m_IsSelected);
        }
    }

    void RequestSelection(int segmentIndex)
    {
        m_SelectRequested?.Invoke(this, segmentIndex);
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

        var hasSprite = Draft != null && !string.IsNullOrEmpty(Draft.spriteGuid);
        if (hasSprite)
            return;

        var painter = context.painter2D;
        painter.lineWidth = 2.5f;
        painter.strokeColor = m_IsPreview
            ? new Color(0.35f, 0.72f, 0.95f, 0.75f)
            : GetDisplayColor();
        painter.BeginPath();
        painter.MoveTo(points[0] - m_PathBounds.position);
        for (var i = 1; i < points.Count; i++)
            painter.LineTo(points[i] - m_PathBounds.position);
        painter.Stroke();
    }

    Color GetDisplayColor()
    {
        if (m_HasTemporaryColor)
            return m_TemporaryColor;
        return Draft?.color ?? new Color(0.68f, 0.72f, 0.78f);
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
        readonly Func<float> m_SnapStep;
        readonly List<Vector2> m_StartPoints = new();
        int m_PointerId;
        Vector2 m_StartPointerPosition;

        public SkillConnectionSegmentManipulator(
            SkillConnectionView connection,
            int segmentIndex,
            Func<float> zoomScale,
            Func<float> snapStep)
        {
            m_Connection = connection;
            m_SegmentIndex = segmentIndex;
            m_ZoomScale = zoomScale;
            m_SnapStep = snapStep;
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

            m_Connection.RequestSelection(m_SegmentIndex);
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
            if (evt.ctrlKey)
            {
                var snapStep = Mathf.Max(1f, m_SnapStep());
                var first = m_StartPoints[m_SegmentIndex];
                var second = m_StartPoints[m_SegmentIndex + 1];
                if (Mathf.Abs(first.y - second.y) < 0.01f)
                {
                    var snapped = Mathf.Round((first.y + delta.y) / snapStep) * snapStep;
                    delta.y = snapped - first.y;
                }
                else
                {
                    var snapped = Mathf.Round((first.x + delta.x) / snapStep) * snapStep;
                    delta.x = snapped - first.x;
                }
            }
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

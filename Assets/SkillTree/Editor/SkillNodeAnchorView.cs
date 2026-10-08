using System;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class SkillNodeAnchorView : VisualElement
{
    readonly Action<SkillNodeView, SkillNodeAnchor, Vector2> m_BeginDrag;
    readonly Action<Vector2> m_UpdateDrag;
    readonly Action<SkillNodeAnchorView> m_EndDrag;
    int m_PointerId;

    public SkillNodeView Node { get; }
    public SkillNodeAnchor Anchor { get; }

    public SkillNodeAnchorView(
        SkillNodeView node,
        SkillNodeAnchor anchor,
        Action<SkillNodeView, SkillNodeAnchor, Vector2> beginDrag,
        Action<Vector2> updateDrag,
        Action<SkillNodeAnchorView> endDrag)
    {
        Node = node;
        Anchor = anchor;
        m_BeginDrag = beginDrag;
        m_UpdateDrag = updateDrag;
        m_EndDrag = endDrag;
        name = $"{anchor}Anchor";
        AddToClassList("node-anchor");
        AddToClassList(anchor switch
        {
            SkillNodeAnchor.Top => "anchor-top",
            SkillNodeAnchor.Right => "anchor-right",
            SkillNodeAnchor.Bottom => "anchor-bottom",
            _ => "anchor-left"
        });

        var dot = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };
        dot.AddToClassList("node-anchor-dot");
        Add(dot);

        RegisterCallback<PointerDownEvent>(OnPointerDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerUpEvent>(OnPointerUp);
    }

    void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0 || !ClassListContains("node-anchor-active"))
            return;

        m_PointerId = evt.pointerId;
        this.CapturePointer(m_PointerId);
        m_BeginDrag?.Invoke(Node, Anchor, evt.position);
        evt.StopPropagation();
    }

    void OnPointerMove(PointerMoveEvent evt)
    {
        if (!this.HasPointerCapture(m_PointerId))
            return;

        m_UpdateDrag?.Invoke(evt.position);
        evt.StopPropagation();
    }

    void OnPointerUp(PointerUpEvent evt)
    {
        if (!this.HasPointerCapture(m_PointerId))
            return;

        this.ReleasePointer(m_PointerId);
        var picked = panel.Pick(evt.position);
        var targetAnchor = picked as SkillNodeAnchorView
            ?? picked?.GetFirstAncestorOfType<SkillNodeAnchorView>();
        m_EndDrag?.Invoke(targetAnchor);
        evt.StopPropagation();
    }
}

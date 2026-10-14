using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class SkillTreeNodeUI : MonoBehaviour, IPointerEnterHandler, 
                                    IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    [SerializeField] string nodeGuid;
    [SerializeField] UnityEngine.UI.Image nodeBackgroundImage;
    [SerializeField] UnityEngine.UI.Image skillImage;
    [SerializeField, Range(0f, 1f)] float pressedBrightness = 0.72f;
    [SerializeField, Min(0f)] float colorTransitionDuration = 0.12f;

    Color m_NodeColor;
    Color m_SkillColor;
    SkillNodeState m_CurrentState;
    Coroutine m_ColorTransition;
    bool m_IsPressed;

    public static event Action<string> NodePointerDown;
    public static event Action<string> NodePointerUp;
    public static event Action<string> NodePointerEntered;
    public static event Action<string> NodePointerExited;

    public string NodeGuid => nodeGuid;

    public void Initialize(
        string guid,
        UnityEngine.UI.Image backgroundImage,
        UnityEngine.UI.Image iconImage)
    {
        nodeGuid = guid;
        nodeBackgroundImage = backgroundImage;
        skillImage = iconImage;
        CaptureNormalColors();
    }

    void Awake()
    {
        CaptureNormalColors();
    }

    public void ApplyState(SkillNodeState state)
    {
        m_CurrentState = state;
        if (state == SkillNodeState.Locked)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        ApplyStateColors();
    }

    public void SetGrayscale(bool grayscale)
    {
        nodeBackgroundImage.color = grayscale ? ToGrayscale(m_NodeColor) : m_NodeColor;
        skillImage.color = grayscale ? ToGrayscale(m_SkillColor) : m_SkillColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        NodePointerEntered?.Invoke(nodeGuid);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        NodePointerExited?.Invoke(nodeGuid);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left
            || m_CurrentState == SkillNodeState.Unlocked)
            return;

        m_IsPressed = true;
        NodePointerDown?.Invoke(nodeGuid);
        StartColorTransition(
            Darken(GetStateNodeColor()),
            Darken(GetStateSkillColor()));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || !m_IsPressed)
            return;

        m_IsPressed = false;
        if (m_CurrentState == SkillNodeState.Unlocked)
        {
            StartColorTransition(GetStateNodeColor(), GetStateSkillColor());
            return;
        }

        var pressedNodeColor = nodeBackgroundImage.color;
        var pressedSkillColor = skillImage.color;
        var releasedOnNode = RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)transform,
            eventData.position,
            eventData.pressEventCamera);

        if (releasedOnNode)
            NodePointerUp?.Invoke(nodeGuid);

        nodeBackgroundImage.color = pressedNodeColor;
        skillImage.color = pressedSkillColor;
        StartColorTransition(GetStateNodeColor(), GetStateSkillColor());
    }

    void CaptureNormalColors()
    {
        m_NodeColor = nodeBackgroundImage.color;
        m_SkillColor = skillImage.color;
    }

    void ApplyStateColors()
    {
        var nodeColor = GetStateNodeColor();
        var iconColor = GetStateSkillColor();
        if (m_IsPressed)
        {
            nodeColor = Darken(nodeColor);
            iconColor = Darken(iconColor);
        }

        SetColors(nodeColor, iconColor);
    }

    Color GetStateNodeColor()
    {
        return m_CurrentState == SkillNodeState.Closed
            ? ToGrayscale(m_NodeColor)
            : m_NodeColor;
    }

    Color GetStateSkillColor()
    {
        return m_CurrentState == SkillNodeState.Closed
            ? ToGrayscale(m_SkillColor)
            : m_SkillColor;
    }

    Color Darken(Color color)
    {
        return new Color(
            color.r * pressedBrightness,
            color.g * pressedBrightness,
            color.b * pressedBrightness,
            color.a);
    }

    void StartColorTransition(Color nodeTarget, Color skillTarget)
    {
        if (m_ColorTransition != null)
            StopCoroutine(m_ColorTransition);

        if (colorTransitionDuration <= 0f)
        {
            SetColors(nodeTarget, skillTarget);
            m_ColorTransition = null;
            return;
        }

        m_ColorTransition = StartCoroutine(TransitionColors(nodeTarget, skillTarget));
    }

    IEnumerator TransitionColors(Color nodeTarget, Color skillTarget)
    {
        var nodeStart = nodeBackgroundImage.color;
        var skillStart = skillImage.color;
        var elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / colorTransitionDuration);
            t = t * t * (3f - 2f * t);
            SetColors(
                Color.LerpUnclamped(nodeStart, nodeTarget, t),
                Color.LerpUnclamped(skillStart, skillTarget, t));
            yield return null;
        }

        SetColors(nodeTarget, skillTarget);
        m_ColorTransition = null;
    }

    void SetColors(Color nodeColor, Color skillColor)
    {
        nodeBackgroundImage.color = nodeColor;
        skillImage.color = skillColor;
    }

    static Color ToGrayscale(Color color)
    {
        var value = color.grayscale;
        return new Color(value, value, value, color.a);
    }
}

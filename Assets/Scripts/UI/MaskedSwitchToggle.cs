using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Toggle))]
public sealed class MaskedSwitchToggle : MonoBehaviour
{
    [SerializeField] RectTransform onMask;
    [SerializeField] RectTransform handle;
    [SerializeField, Min(0f)] float animationDuration = 0.18f;

    Toggle m_Toggle;
    RectTransform m_RectTransform;
    float m_Progress;
    float m_TargetProgress;

    void Awake()
    {
        m_Toggle = GetComponent<Toggle>();
        m_RectTransform = (RectTransform)transform;
        m_Progress = m_Toggle.isOn ? 1f : 0f;
        m_TargetProgress = m_Progress;
        ApplyVisuals();
    }

    void OnEnable()
    {
        m_Toggle.onValueChanged.AddListener(HandleValueChanged);
    }

    void OnDisable()
    {
        m_Toggle.onValueChanged.RemoveListener(HandleValueChanged);
    }

    void Update()
    {
        if (Mathf.Approximately(m_Progress, m_TargetProgress))
            return;

        m_Progress = animationDuration <= 0f
            ? m_TargetProgress
            : Mathf.MoveTowards(
                m_Progress,
                m_TargetProgress,
                Time.unscaledDeltaTime / animationDuration);
        ApplyVisuals();
    }

    void OnRectTransformDimensionsChange()
    {
        if (m_RectTransform != null)
            ApplyVisuals();
    }

    void HandleValueChanged(bool isOn)
    {
        m_TargetProgress = isOn ? 1f : 0f;
    }

    void ApplyVisuals()
    {
        var trackWidth = m_RectTransform.rect.width;
        var handleTravel = Mathf.Max(
            0f,
            (trackWidth - handle.rect.width) * 0.5f);

        var handlePosition = handle.anchoredPosition;
        handlePosition.x = Mathf.Lerp(-handleTravel, handleTravel, m_Progress);
        handle.anchoredPosition = handlePosition;

        onMask.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            trackWidth * m_Progress);
    }
}

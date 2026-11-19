using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Scrollbar))]
public sealed class MaskedScrollbarBackground : MonoBehaviour
{
    [SerializeField] RectTransform fillMask;
    [SerializeField] RectTransform handle;

    Scrollbar m_Scrollbar;
    RectTransform m_RectTransform;

    void Awake()
    {
        m_Scrollbar = GetComponent<Scrollbar>();
        m_RectTransform = (RectTransform)transform;
        UpdateMask();
    }

    void OnEnable()
    {
        m_Scrollbar.onValueChanged.AddListener(HandleValueChanged);
        UpdateMask();
    }

    void OnDisable()
    {
        m_Scrollbar.onValueChanged.RemoveListener(HandleValueChanged);
    }

    void OnRectTransformDimensionsChange()
    {
        if (m_RectTransform != null)
            UpdateMask();
    }

    void HandleValueChanged(float value)
    {
        UpdateMask();
    }

    void UpdateMask()
    {
        var handleWorldCenter = handle.TransformPoint(handle.rect.center);
        var handleLocalCenter = m_RectTransform.InverseTransformPoint(handleWorldCenter);
        var fillWidth = Mathf.Clamp(
            handleLocalCenter.x - m_RectTransform.rect.xMin,
            0f,
            m_RectTransform.rect.width);

        fillMask.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            fillWidth);
    }
}

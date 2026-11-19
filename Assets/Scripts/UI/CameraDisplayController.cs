using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnityEngine.UI.RawImage))]
public sealed class CameraDisplayController : MonoBehaviour
{
    UnityEngine.UI.RawImage m_RawImage;

    void Awake()
    {
        m_RawImage = GetComponent<UnityEngine.UI.RawImage>();
    }

    void OnEnable()
    {
        if (GameSettings.HasInstance)
            GameSettings.Instance.OnCameraVisibleChanged += ApplyVisibility;
    }

    void Start()
    {
        if (GameSettings.HasInstance)
            ApplyVisibility(GameSettings.Instance.CameraVisible);
    }

    void OnDisable()
    {
        if (GameSettings.HasInstance)
            GameSettings.Instance.OnCameraVisibleChanged -= ApplyVisibility;
    }

    void ApplyVisibility(bool visible)
    {
        m_RawImage.enabled = visible;
    }
}

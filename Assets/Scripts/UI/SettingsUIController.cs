using UnityEngine;

[DisallowMultipleComponent]
public sealed class SettingsUIController : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Toggle cameraToggle;
    [SerializeField] UnityEngine.UI.Toggle damageDisplayToggle;

    void OnEnable()
    {
        AttachSettings();
        SyncFromSettings();
    }

    void Start()
    {
        AttachSettings();
        SyncFromSettings();
    }

    void OnDisable()
    {
        if (!GameSettings.HasInstance)
            return;

        GameSettings.Instance.OnCameraVisibleChanged -= HandleCameraChanged;
        GameSettings.Instance.OnDamageDisplayVisibleChanged -= HandleDamageChanged;
    }

    void AttachSettings()
    {
        if (!GameSettings.HasInstance)
            return;

        GameSettings.Instance.OnCameraVisibleChanged -= HandleCameraChanged;
        GameSettings.Instance.OnDamageDisplayVisibleChanged -= HandleDamageChanged;
        GameSettings.Instance.OnCameraVisibleChanged += HandleCameraChanged;
        GameSettings.Instance.OnDamageDisplayVisibleChanged += HandleDamageChanged;
    }

    void SyncFromSettings()
    {
        if (!GameSettings.HasInstance)
            return;

        cameraToggle.SetIsOnWithoutNotify(GameSettings.Instance.CameraVisible);
        damageDisplayToggle.SetIsOnWithoutNotify(
            GameSettings.Instance.DamageDisplayVisible);
    }

    void HandleCameraChanged(bool visible)
    {
        cameraToggle.SetIsOnWithoutNotify(visible);
    }

    void HandleDamageChanged(bool visible)
    {
        damageDisplayToggle.SetIsOnWithoutNotify(visible);
    }
}

using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameSettings : MonoBehaviour
{
    [SerializeField] bool cameraVisible = true;
    [SerializeField] bool damageDisplayVisible = true;

    public static GameSettings Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public bool CameraVisible => cameraVisible;
    public bool DamageDisplayVisible => damageDisplayVisible;

    public event Action<bool> OnCameraVisibleChanged;
    public event Action<bool> OnDamageDisplayVisibleChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetCameraVisible(bool value)
    {
        if (cameraVisible == value)
            return;

        cameraVisible = value;
        OnCameraVisibleChanged?.Invoke(value);
    }

    public void SetDamageDisplayVisible(bool value)
    {
        if (damageDisplayVisible == value)
            return;

        damageDisplayVisible = value;
        OnDamageDisplayVisibleChanged?.Invoke(value);
    }

    public void ToggleCameraVisible()
    {
        SetCameraVisible(!cameraVisible);
    }

    public void ToggleDamageDisplayVisible()
    {
        SetDamageDisplayVisible(!damageDisplayVisible);
    }

    public void Apply(SettingsSaveData data)
    {
        SetCameraVisible(data.cameraVisible);
        SetDamageDisplayVisible(data.damageDisplayVisible);
    }
}

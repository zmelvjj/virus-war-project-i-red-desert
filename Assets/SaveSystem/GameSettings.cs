using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameSettings : MonoBehaviour
{
    [SerializeField] bool cameraVisible = true;
    [SerializeField] bool damageDisplayVisible = true;
    [SerializeField, Range(0f, 1f)] float soundVolume = 1f;

    public static GameSettings Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    public bool CameraVisible => cameraVisible;
    public bool DamageDisplayVisible => damageDisplayVisible;
    public float SoundVolume => soundVolume;

    public event Action<bool> OnCameraVisibleChanged;
    public event Action<bool> OnDamageDisplayVisibleChanged;
    public event Action<float> OnSoundVolumeChanged;

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

    public void SetSoundVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (soundVolume == value)
            return;

        soundVolume = value;
        OnSoundVolumeChanged?.Invoke(value);
    }

    public void ToggleDamageDisplayVisible()
    {
        SetDamageDisplayVisible(!damageDisplayVisible);
    }

    public void Apply(SettingsSaveData data)
    {
        SetCameraVisible(data.cameraVisible);
        SetDamageDisplayVisible(data.damageDisplayVisible);
        SetSoundVolume(data.soundVolume);
    }
}

using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Scrollbar))]
public sealed class SoundVolumeScrollbar : MonoBehaviour
{
    Scrollbar m_Scrollbar;
    float m_InitialVolume;

    void Awake()
    {
        m_Scrollbar = GetComponent<Scrollbar>();
    }

    void OnEnable()
    {
        m_InitialVolume = GameSettings.Instance.SoundVolume;
        m_Scrollbar.value = m_InitialVolume;
        m_Scrollbar.onValueChanged.AddListener(SetVolume);
    }

    void OnDisable()
    {
        m_Scrollbar.onValueChanged.RemoveListener(SetVolume);
        if (GameSettings.HasInstance
            && GameSettings.Instance.SoundVolume != m_InitialVolume
            && SaveManager.HasInstance)
        {
            SaveManager.Instance.Save();
        }
    }

    void SetVolume(float volume)
    {
        GameSettings.Instance.SetSoundVolume(volume);
    }
}

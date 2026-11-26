using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum GameSound
{
    Attack,
    MonsterHit,
    MonsterDeath,
    Lightning,
    Shockwave,
    Ignite
}

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class GameSoundPlayer : MonoBehaviour
{
    [SerializeField] AudioClip[] attack;
    [SerializeField] AudioClip[] monsterHit;
    [SerializeField] AudioClip[] monsterDeath;
    [SerializeField] AudioClip[] lightning;
    [SerializeField] AudioClip[] shockwave;
    [SerializeField] AudioClip[] ignite;

    static event Action<GameSound> PlayRequested;

    AudioSource m_AudioSource;
    GameSettings m_Settings;

    public static void Request(GameSound sound)
    {
        PlayRequested?.Invoke(sound);
    }

    void Awake()
    {
        m_AudioSource = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        PlayRequested += Play;
    }

    void Start()
    {
        m_Settings = GameSettings.Instance;
        if (m_Settings == null)
            return;

        m_Settings.OnSoundVolumeChanged += SetVolume;
        SetVolume(m_Settings.SoundVolume);
    }

    void OnDisable()
    {
        PlayRequested -= Play;
    }

    void OnDestroy()
    {
        if (m_Settings != null)
            m_Settings.OnSoundVolumeChanged -= SetVolume;
    }

    void SetVolume(float volume)
    {
        m_AudioSource.volume = volume;
    }

    void Play(GameSound sound)
    {
        var clips = sound switch
        {
            GameSound.Attack => attack,
            GameSound.MonsterHit => monsterHit,
            GameSound.MonsterDeath => monsterDeath,
            GameSound.Lightning => lightning,
            GameSound.Shockwave => shockwave,
            GameSound.Ignite => ignite,
            _ => null
        };

        if (clips != null && clips.Length > 0 && m_AudioSource.volume > 0f)
            m_AudioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}

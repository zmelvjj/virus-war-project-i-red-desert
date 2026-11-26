using UnityEngine;
using Unity.Cinemachine;

public class Shake : MonoBehaviour
{
    public static Shake Instance { get; private set; }

    CinemachineBasicMultiChannelPerlin m_Noise;
    float m_RestingAmplitude;
    float m_ShakeTimer;
    float m_ShakeTimeTotal;
    float m_ShakeIntensity;

    void Awake()
    {
        m_Noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
    }

    void OnEnable()
    {
        Instance = this;
    }

    void OnDisable()
    {
        if (m_ShakeTimer > 0f)
        {
            m_Noise.AmplitudeGain = m_RestingAmplitude;
            m_ShakeTimer = 0f;
        }

        if (Instance == this)
            Instance = null;
    }

    public void ShakeCamera(float intensity, float time)
    {
        if (m_ShakeTimer <= 0f)
            m_RestingAmplitude = m_Noise.AmplitudeGain;

        m_ShakeTimer = Mathf.Max(m_ShakeTimer, time);
        m_ShakeTimeTotal = m_ShakeTimer;
        m_ShakeIntensity = intensity;
        m_Noise.AmplitudeGain = intensity;
    }

    void Update()
    {
        if (m_ShakeTimer <= 0f)
            return;

        m_ShakeTimer = Mathf.Max(0f, m_ShakeTimer - Time.deltaTime);
        m_Noise.AmplitudeGain = Mathf.Lerp(
            m_RestingAmplitude,
            m_ShakeIntensity,
            m_ShakeTimer / m_ShakeTimeTotal);
    }
}

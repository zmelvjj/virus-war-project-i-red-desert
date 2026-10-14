using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem))]
public sealed class PooledParticleVfx : MonoBehaviour
{
    ParticleVfxPoolManager m_Owner;
    ParticleSystem m_ParticleSystem;
    Coroutine m_ReturnRoutine;
    bool m_IsPlaying;

    public ParticleSystem SourcePrefab { get; private set; }

    public void Initialize(
        ParticleVfxPoolManager owner,
        ParticleSystem sourcePrefab,
        ParticleSystem particleSystem)
    {
        m_Owner = owner;
        SourcePrefab = sourcePrefab;
        m_ParticleSystem = particleSystem;

        var main = m_ParticleSystem.main;
        main.stopAction = ParticleSystemStopAction.Callback;
    }

    public void Play(Vector3 position, Quaternion rotation)
    {
        StopReturnRoutine();
        transform.SetPositionAndRotation(position, rotation);
        gameObject.SetActive(true);
        m_ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        m_ParticleSystem.Clear(true);
        m_IsPlaying = true;
        m_ParticleSystem.Play(true);
    }

    public void PrepareForPool()
    {
        StopReturnRoutine();
        m_IsPlaying = false;
        m_ParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        m_ParticleSystem.Clear(true);
        gameObject.SetActive(false);
    }

    void OnParticleSystemStopped()
    {
        if (!m_IsPlaying || m_ReturnRoutine != null)
            return;

        m_ReturnRoutine = StartCoroutine(ReturnWhenAllParticlesStopped());
    }

    IEnumerator ReturnWhenAllParticlesStopped()
    {
        while (m_ParticleSystem.IsAlive(true))
            yield return null;

        m_ReturnRoutine = null;
        m_IsPlaying = false;
        m_Owner.Return(this);
    }

    void StopReturnRoutine()
    {
        if (m_ReturnRoutine == null)
            return;

        StopCoroutine(m_ReturnRoutine);
        m_ReturnRoutine = null;
    }
}

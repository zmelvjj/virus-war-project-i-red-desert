using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PooledParticleVfx : MonoBehaviour
{
    ParticleVfxPoolManager m_Owner;
    ParticleSystem[] m_ParticleSystems;
    Coroutine m_ReturnRoutine;
    Transform m_FollowTarget;
    Vector3 m_FollowOffset;
    bool m_IsPlaying;

    public GameObject SourcePrefab { get; private set; }

    public void Initialize(
        ParticleVfxPoolManager owner,
        GameObject sourcePrefab)
    {
        m_Owner = owner;
        SourcePrefab = sourcePrefab;
        m_ParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
    }

    public void Play(
        Vector3 position,
        Quaternion rotation,
        float scaleMultiplier,
        Transform followTarget,
        float duration)
    {
        StopReturnRoutine();
        transform.SetPositionAndRotation(position, rotation);
        transform.localScale = SourcePrefab.transform.localScale * scaleMultiplier;
        m_FollowTarget = followTarget;
        m_FollowOffset = followTarget != null
            ? position - followTarget.position
            : Vector3.zero;
        gameObject.SetActive(true);

        foreach (var particleSystem in m_ParticleSystems)
        {
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(false);
            particleSystem.Play(false);
        }

        m_IsPlaying = true;
        m_ReturnRoutine = duration > 0f
            ? StartCoroutine(ReturnAfterDuration(duration))
            : StartCoroutine(ReturnWhenAllParticlesStopped());
    }

    public void PrepareForPool()
    {
        StopReturnRoutine();
        m_IsPlaying = false;
        m_FollowTarget = null;

        foreach (var particleSystem in m_ParticleSystems)
        {
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Clear(false);
        }

        gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (m_IsPlaying && m_FollowTarget != null)
            transform.position = m_FollowTarget.position + m_FollowOffset;
    }

    IEnumerator ReturnWhenAllParticlesStopped()
    {
        while (AreParticlesAlive())
            yield return null;

        m_ReturnRoutine = null;
        m_IsPlaying = false;
        m_Owner.Return(this);
    }

    IEnumerator ReturnAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);

        foreach (var particleSystem in m_ParticleSystems)
            particleSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        while (AreParticlesAlive())
            yield return null;

        m_ReturnRoutine = null;
        m_IsPlaying = false;
        m_Owner.Return(this);
    }

    bool AreParticlesAlive()
    {
        foreach (var particleSystem in m_ParticleSystems)
        {
            if (particleSystem.IsAlive(false))
                return true;
        }

        return false;
    }

    void StopReturnRoutine()
    {
        if (m_ReturnRoutine == null)
            return;

        StopCoroutine(m_ReturnRoutine);
        m_ReturnRoutine = null;
    }
}

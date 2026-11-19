using System.Collections;
using UnityEngine;

public sealed class HitSpeedBoostMonsterTrait : MonoBehaviour, IMonsterTrait, IMonsterDamageTakenHandler
{
    const float k_SpeedMultiplier = 1.35f;
    const float k_BoostDuration = 2f;

    MonsterNavMeshMovement m_Movement;
    Coroutine m_BoostRoutine;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
        m_Movement = entity.GetComponent<MonsterNavMeshMovement>();
    }

    public void OnDamageTaken(int damage)
    {
        if (m_Movement == null)
            return;

        if (m_BoostRoutine != null)
            StopCoroutine(m_BoostRoutine);

        m_Movement.SetSpeedMultiplier(k_SpeedMultiplier);
        m_BoostRoutine = StartCoroutine(ResetSpeedAfterDelay());
    }

    IEnumerator ResetSpeedAfterDelay()
    {
        yield return new WaitForSeconds(k_BoostDuration);
        m_Movement.SetSpeedMultiplier(1f);
        m_BoostRoutine = null;
    }

    void OnDisable()
    {
        if (m_Movement != null)
            m_Movement.SetSpeedMultiplier(1f);
    }
}

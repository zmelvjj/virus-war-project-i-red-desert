using System.Collections;
using UnityEngine;

public sealed class HitSpeedBoostMonsterTrait : MonoBehaviour, IMonsterTrait, IMonsterDamageTakenHandler
{
    const float k_SpeedMultiplier = 4f;
    const float k_BoostDuration = 2f;

    MonsterNavMeshMovement m_Movement;
    Coroutine m_BoostRoutine;
    AnimatorControl m_ctr;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
        m_Movement = entity.GetComponent<MonsterNavMeshMovement>();
        m_ctr = gameObject.GetComponent<AnimatorControl>();
    }

    public void OnDamageTaken(int damage)
    {
        if (m_Movement == null)
            return;

        if (m_BoostRoutine != null)
            return;

        m_BoostRoutine = StartCoroutine(ResetSpeedAfterDelay());
    }

    IEnumerator ResetSpeedAfterDelay()
    {
        m_ctr.AnimeOn();

        yield return new WaitForSeconds(0.5f); //애니메이션 대기
        m_Movement.SetSpeedMultiplier(k_SpeedMultiplier);

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

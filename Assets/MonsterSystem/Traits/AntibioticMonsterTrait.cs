using System.Collections;
using System;
using UnityEngine;

public sealed class AntibioticMonsterTrait : MonoBehaviour, IMonsterTrait
{
    const float k_ExplosionDelay = 3f;
    const float k_MaxStaminaPenalty = 5f;

    MonsterEntity m_Entity;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
        m_Entity = entity;
        StartCoroutine(ExplodeAfterDelay());
    }

    IEnumerator ExplodeAfterDelay()
    {
        var elapsed = 0f;
        while (elapsed < k_ExplosionDelay)
        {
            if (m_Entity.IsDead)
                yield break;

            if (GameFlowManager.IsMainGame)
                elapsed += Time.deltaTime;
            yield return null;
        }

        if (m_Entity.IsDead)
            yield break;

        // 폭발 VFX는 프리팹이 준비되면 이 지점에서 재생한다.
        if (PlayerStats.HasInstance)
        {
            PlayerStats.Instance.AddPersistentModifier(new StatModifier(
                $"Antibiotic.MaxStamina.{Guid.NewGuid():N}",
                PlayerStatType.MaxStamina,
                SkillModifierOperation.Flat,
                -k_MaxStaminaPenalty));
        }

        m_Entity.Kill(MonsterDeathCause.Antibiotic);
        Destroy(m_Entity.gameObject);
    }
}

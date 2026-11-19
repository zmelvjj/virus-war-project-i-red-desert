using UnityEngine;

public sealed class SplitMonsterTrait : MonoBehaviour, IMonsterTrait, IMonsterDeathHandler
{
    const float k_SplitChance = 0.25f;
    const float k_ChildRatio = 0.5f;
    const float k_SpawnOffset = 0.5f;

    MonsterEntity m_Entity;
    MonsterTraitBuildContext m_Context;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
        m_Entity = entity;
        m_Context = context;
    }

    public void OnMonsterDied(MonsterDeathCause cause)
    {
        if (cause != MonsterDeathCause.Damage || Random.value >= k_SplitChance)
            return;

        m_Entity.SuppressDeathReward();
        var parent = m_Entity.transform.parent;
        var childHp = Mathf.Max(1, Mathf.CeilToInt(m_Entity.MaxHp * k_ChildRatio));
        for (var i = 0; i < 2; i++)
        {
            var direction = i == 0 ? Vector3.left : Vector3.right;
            var child = MonsterFactory.Instance
                .Create(m_Entity.Definition)
                .At(m_Entity.transform.position + direction * k_SpawnOffset)
                .Within(m_Context.MovementCenter, m_Context.MovementRange)
                .WithHp(childHp)
                .WithRewardMultiplier(k_ChildRatio)
                .WithScaleMultiplier(k_ChildRatio)
                .WithoutTrait(MonsterTraitType.Split)
                .Build();
            child.transform.parent = parent;

            var specialStageMonster = m_Entity.GetComponent<SpecialStageMonster>();
            if (specialStageMonster != null
                && SpecialStageManager.HasInstance)
            {
                SpecialStageManager.Instance.RegisterSpawnedMonster(
                    child,
                    specialStageMonster.SpecialStageId);
            }
        }
    }
}

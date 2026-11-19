using UnityEngine;

public sealed class StaminaDropMonsterTrait : MonoBehaviour, IMonsterTrait, IMonsterDeathHandler
{
    const float k_StaminaRecovery = 10f;

    public void Initialize(MonsterEntity entity, MonsterTraitBuildContext context)
    {
    }

    public void OnMonsterDied(MonsterDeathCause cause)
    {
        if (cause == MonsterDeathCause.Damage && GameFlowManager.HasInstance)
            GameFlowManager.Instance.StaminaController.Restore(k_StaminaRecovery);
    }
}

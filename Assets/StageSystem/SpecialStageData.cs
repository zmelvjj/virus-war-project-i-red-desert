using UnityEngine;

[CreateAssetMenu(
    fileName = "SpecialStageData",
    menuName = "Stage/Special Stage Data")]
public sealed class SpecialStageData : ScriptableObject
{
    [SerializeField] string specialStageId;
    [SerializeField, Min(0)] int requiredReachedStage;
    [SerializeField, Min(1)] int spawnCount = 1;
    [SerializeField] MonsterDefinition monsterDefinition;
    [SerializeField, Min(0)] int rewardDNA;

    public string SpecialStageId => specialStageId;
    public int RequiredReachedStage => requiredReachedStage;
    public int SpawnCount => spawnCount;
    public MonsterDefinition MonsterDefinition => monsterDefinition;
    public int RewardDNA => rewardDNA;
}

using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "SpecialStageData",
    menuName = "Stage/Special Stage Data")]
public sealed class SpecialStageData : ScriptableObject
{
    [SerializeField] string specialStageId;
    [SerializeField, Min(0)] int requiredReachedStage;
    [SerializeField, Min(1)] int spawnCount = 1;
    [SerializeField] MonsterDefinition monsterDefinition;
    [FormerlySerializedAs("rewardDNA")]
    [SerializeField, Min(0)] int minimumRewardDNA;
    [SerializeField, Min(0)] int maximumRewardDNA;

    public string SpecialStageId => specialStageId;
    public int RequiredReachedStage => requiredReachedStage;
    public int SpawnCount => spawnCount;
    public MonsterDefinition MonsterDefinition => monsterDefinition;

    public int RollRewardDNA()
    {
        const int rewardStep = 10;
        var minimum = Mathf.Max(0, minimumRewardDNA) / rewardStep;
        var maximum = Mathf.Max(minimum, maximumRewardDNA / rewardStep);
        return Random.Range(minimum, maximum + 1) * rewardStep;
    }
}

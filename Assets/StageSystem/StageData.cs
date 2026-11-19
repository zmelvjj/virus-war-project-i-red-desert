using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class MonsterSpawnEntry
{
    [SerializeField] MonsterDefinition monsterDefinition;
    [SerializeField, Min(0f)] float spawnWeight = 1f;

    public MonsterDefinition MonsterDefinition => monsterDefinition;
    public float SpawnWeight => spawnWeight;
}

[CreateAssetMenu(
    fileName = "StageData",
    menuName = "Stage/Stage Data")]
public sealed class StageData : ScriptableObject
{
    [SerializeField, Min(0)] int stageIndex;
    [SerializeField, Min(0)] int requiredKillCount;
    [SerializeField] List<MonsterSpawnEntry> monsterSpawnEntries = new();

    public int StageIndex => stageIndex;
    public int RequiredKillCount => requiredKillCount;
    public IReadOnlyList<MonsterSpawnEntry> MonsterSpawnEntries => monsterSpawnEntries;
}

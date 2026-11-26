using System;
using System.Collections.Generic;

[Serializable]
public sealed class GameSaveData
{
    public int version = 1;
    public int currentDNA;
    public PlayerSaveData player = new();
    public SkillTreeSaveData skillTree = new();
    public SettingsSaveData settings = new();
    public StageProgressSaveData stageProgress;
    public SpecialStageProgressSaveData specialStageProgress = new();
    public AugmentSaveData augments = new();
}

[Serializable]
public sealed class AugmentSaveData
{
    public List<AugmentType> acquiredAugments = new();
}

[Serializable]
public struct StageProgressSaveData
{
    public int stageIndex;
    public int killCount;
}

[Serializable]
public sealed class PlayerSaveData
{
    public List<PersistentStatModifierSaveData> persistentModifiers = new();
}

[Serializable]
public sealed class PersistentStatModifierSaveData
{
    public string sourceId;
    public PlayerStatType statType;
    public SkillModifierOperation operation;
    public float value;
}

[Serializable]
public sealed class SkillTreeSaveData
{
    public List<string> unlockedNodeGuids = new();
}

[Serializable]
public sealed class SettingsSaveData
{
    public bool cameraVisible = true;
    public bool damageDisplayVisible = true;
    public float soundVolume = 1f;
}

[Serializable]
public sealed class SpecialStageProgressSaveData
{
    public List<string> resolvedSpecialStageIds = new();
    public string pendingSpecialStageId;
}

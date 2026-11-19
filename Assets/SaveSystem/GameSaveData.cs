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
    public SpecialStageProgressSaveData specialStageProgress = new();
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
}

[Serializable]
public sealed class SpecialStageProgressSaveData
{
    public List<string> resolvedSpecialStageIds = new();
    public string pendingSpecialStageId;
}

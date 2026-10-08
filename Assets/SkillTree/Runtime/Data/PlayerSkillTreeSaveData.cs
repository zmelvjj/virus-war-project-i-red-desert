using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlayerSkillTreeSaveData
{
    [SerializeField] List<string> unlockedNodeGuids = new();

    public List<string> UnlockedNodeGuids => unlockedNodeGuids;
}

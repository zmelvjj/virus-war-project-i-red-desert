using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SaveManager : MonoBehaviour
{
    const int k_CurrentVersion = 1;
    const string k_SaveFileName = "game_save.json";

    GameSaveData m_CurrentData;
    bool m_IsApplyingData;
    bool m_SkillTreeApplied;
    bool m_HasLoaded;

    GameSettings m_GameSettings;
    PlayerStats m_PlayerStats;
    SkillTreeManager m_SkillTreeManager;
    GameFlowManager m_GameFlowManager;
    DNAWallet m_AppliedWallet;
    PlayerStats m_AppliedPlayerStats;

    public static SaveManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;
    public bool IsLoaded => m_HasLoaded;
    public SpecialStageProgressSaveData SpecialStageProgress =>
        m_CurrentData.specialStageProgress;

    public string SavePath => Path.Combine(
        Application.persistentDataPath,
        k_SaveFileName);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshRuntimeEvents();
        Load();
    }

    void OnApplicationQuit()
    {
        Save();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        DetachRuntimeEvents();

        if (Instance == this)
            Instance = null;
    }

    public void Save()
    {
        if (m_IsApplyingData || !m_HasLoaded)
            return;

        m_CurrentData = CollectRuntimeData();
        WriteData(m_CurrentData);
    }

    public void Load()
    {
        var createdDefault = !File.Exists(SavePath);
        m_CurrentData = createdDefault
            ? CreateDefaultSave()
            : ReadData();
        Normalize(m_CurrentData);

        m_HasLoaded = true;
        m_IsApplyingData = true;
        m_SkillTreeApplied = false;
        m_AppliedWallet = null;
        m_AppliedPlayerStats = null;

        ApplyLoadedDataToAvailableSystems();
        m_GameSettings?.Apply(m_CurrentData.settings);

        m_IsApplyingData = false;
        TryApplySkillTree();

        if (createdDefault)
            WriteData(m_CurrentData);
    }

    public GameSaveData CreateDefaultSave()
    {
        return new GameSaveData
        {
            version = k_CurrentVersion,
            currentDNA = 0,
            player = new PlayerSaveData(),
            skillTree = new SkillTreeSaveData(),
            settings = new SettingsSaveData
            {
                cameraVisible = true,
                damageDisplayVisible = true
            },
            specialStageProgress = new SpecialStageProgressSaveData()
        };
    }

    GameSaveData CollectRuntimeData()
    {
        var data = CreateDefaultSave();
        data.currentDNA = DNAWallet.HasInstance
            ? DNAWallet.Instance.CurrentDNA
            : m_CurrentData.currentDNA;

        if (PlayerStats.HasInstance)
        {
            foreach (var modifier in PlayerStats.Instance.PersistentModifiers)
            {
                data.player.persistentModifiers.Add(
                    new PersistentStatModifierSaveData
                    {
                        sourceId = modifier.SourceNodeGuid,
                        statType = modifier.Stat,
                        operation = modifier.Operation,
                        value = modifier.Value
                    });
            }
        }
        else
        {
            data.player.persistentModifiers.AddRange(
                m_CurrentData.player.persistentModifiers);
        }

        if (SkillTreeManager.HasInstance
            && SkillTreeManager.Instance.IsInitialized)
        {
            foreach (var nodeData in SkillTreeManager.Instance.TreeData.Nodes)
            {
                if (SkillTreeManager.Instance.GetState(nodeData.Guid)
                    == SkillNodeState.Unlocked)
                {
                    data.skillTree.unlockedNodeGuids.Add(nodeData.Guid);
                }
            }
        }
        else
        {
            data.skillTree.unlockedNodeGuids.AddRange(
                m_CurrentData.skillTree.unlockedNodeGuids);
        }

        if (GameSettings.HasInstance)
        {
            data.settings.cameraVisible = GameSettings.Instance.CameraVisible;
            data.settings.damageDisplayVisible =
                GameSettings.Instance.DamageDisplayVisible;
        }
        else
        {
            data.settings.cameraVisible = m_CurrentData.settings.cameraVisible;
            data.settings.damageDisplayVisible =
                m_CurrentData.settings.damageDisplayVisible;
        }

        if (RunManager.HasInstance && RunManager.Instance.IsInitialized)
        {
            data.specialStageProgress.resolvedSpecialStageIds.AddRange(
                RunManager.Instance.ResolvedSpecialStageIds);
            data.specialStageProgress.pendingSpecialStageId =
                RunManager.Instance.PendingSpecialStageId;
        }
        else
        {
            data.specialStageProgress.resolvedSpecialStageIds.AddRange(
                m_CurrentData.specialStageProgress.resolvedSpecialStageIds);
            data.specialStageProgress.pendingSpecialStageId =
                m_CurrentData.specialStageProgress.pendingSpecialStageId;
        }

        return data;
    }

    GameSaveData ReadData()
    {
        try
        {
            var json = File.ReadAllText(SavePath);
            return JsonUtility.FromJson<GameSaveData>(json)
                ?? CreateDefaultSave();
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[SaveManager] Save load failed: {exception.Message}");
            return CreateDefaultSave();
        }
    }

    void WriteData(GameSaveData data)
    {
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[SaveManager] Save write failed: {exception.Message}");
        }
    }

    void TryApplySkillTree()
    {
        if (!m_HasLoaded
            || m_SkillTreeApplied
            || m_SkillTreeManager == null
            || !m_SkillTreeManager.IsInitialized)
        {
            return;
        }

        m_IsApplyingData = true;
        if (SkillEffectSystem.HasInstance)
            SkillEffectSystem.Instance.ResetSkillEffects();

        m_SkillTreeManager.RestoreUnlockedNodes(
            m_CurrentData.skillTree.unlockedNodeGuids);
        m_SkillTreeApplied = true;
        m_IsApplyingData = false;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshRuntimeEvents();
        if (!m_HasLoaded)
            return;

        m_IsApplyingData = true;
        ApplyLoadedDataToAvailableSystems();
        m_IsApplyingData = false;
        TryApplySkillTree();
    }

    void ApplyLoadedDataToAvailableSystems()
    {
        if (DNAWallet.HasInstance && m_AppliedWallet != DNAWallet.Instance)
        {
            m_AppliedWallet = DNAWallet.Instance;
            m_AppliedWallet.SetDNA(m_CurrentData.currentDNA);
        }

        if (m_PlayerStats == null || m_AppliedPlayerStats == m_PlayerStats)
            return;

        var modifiers = new List<StatModifier>();
        foreach (var savedModifier in m_CurrentData.player.persistentModifiers)
        {
            modifiers.Add(new StatModifier(
                savedModifier.sourceId,
                savedModifier.statType,
                savedModifier.operation,
                savedModifier.value));
        }

        m_AppliedPlayerStats = m_PlayerStats;
        m_PlayerStats.RestorePersistentModifiers(modifiers);
    }

    void RefreshRuntimeEvents()
    {
        var gameSettings = GameSettings.HasInstance
            ? GameSettings.Instance
            : null;
        if (m_GameSettings != gameSettings)
        {
            if (m_GameSettings != null)
            {
                m_GameSettings.OnCameraVisibleChanged -= HandleSettingChanged;
                m_GameSettings.OnDamageDisplayVisibleChanged -= HandleSettingChanged;
            }

            m_GameSettings = gameSettings;
            if (m_GameSettings != null)
            {
                m_GameSettings.OnCameraVisibleChanged += HandleSettingChanged;
                m_GameSettings.OnDamageDisplayVisibleChanged += HandleSettingChanged;
            }
        }

        var playerStats = PlayerStats.HasInstance
            ? PlayerStats.Instance
            : null;
        if (m_PlayerStats != playerStats)
        {
            if (m_PlayerStats != null)
                m_PlayerStats.PersistentModifiersChanged -= HandlePersistentChanged;

            m_PlayerStats = playerStats;
            m_AppliedPlayerStats = null;
            if (m_PlayerStats != null)
                m_PlayerStats.PersistentModifiersChanged += HandlePersistentChanged;
        }

        var skillTreeManager = SkillTreeManager.HasInstance
            ? SkillTreeManager.Instance
            : null;
        if (m_SkillTreeManager != skillTreeManager)
        {
            if (m_SkillTreeManager != null)
            {
                m_SkillTreeManager.Initialized -= TryApplySkillTree;
                m_SkillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;
            }

            m_SkillTreeManager = skillTreeManager;
            m_SkillTreeApplied = false;
            if (m_SkillTreeManager != null)
            {
                m_SkillTreeManager.Initialized += TryApplySkillTree;
                m_SkillTreeManager.OnNodeUnlocked += HandleNodeUnlocked;
            }
        }

        var gameFlowManager = GameFlowManager.HasInstance
            ? GameFlowManager.Instance
            : null;
        if (m_GameFlowManager == gameFlowManager)
            return;

        if (m_GameFlowManager != null)
            m_GameFlowManager.OnGameOver -= Save;

        m_GameFlowManager = gameFlowManager;
        if (m_GameFlowManager != null)
            m_GameFlowManager.OnGameOver += Save;
    }

    void DetachRuntimeEvents()
    {
        if (m_GameSettings != null)
        {
            m_GameSettings.OnCameraVisibleChanged -= HandleSettingChanged;
            m_GameSettings.OnDamageDisplayVisibleChanged -= HandleSettingChanged;
        }

        if (m_PlayerStats != null)
            m_PlayerStats.PersistentModifiersChanged -= HandlePersistentChanged;

        if (m_SkillTreeManager != null)
        {
            m_SkillTreeManager.Initialized -= TryApplySkillTree;
            m_SkillTreeManager.OnNodeUnlocked -= HandleNodeUnlocked;
        }

        if (m_GameFlowManager != null)
            m_GameFlowManager.OnGameOver -= Save;

        m_GameSettings = null;
        m_PlayerStats = null;
        m_SkillTreeManager = null;
        m_GameFlowManager = null;
    }

    void HandleSettingChanged(bool value)
    {
        Save();
    }

    void HandlePersistentChanged()
    {
        Save();
    }

    void HandleNodeUnlocked(SkillNodeData nodeData)
    {
        Save();
    }

    static void Normalize(GameSaveData data)
    {
        data.version = data.version <= 0 ? k_CurrentVersion : data.version;
        data.player ??= new PlayerSaveData();
        data.player.persistentModifiers ??= new List<PersistentStatModifierSaveData>();
        data.skillTree ??= new SkillTreeSaveData();
        data.skillTree.unlockedNodeGuids ??= new List<string>();
        data.settings ??= new SettingsSaveData();
        data.specialStageProgress ??= new SpecialStageProgressSaveData();
        data.specialStageProgress.resolvedSpecialStageIds ??= new List<string>();
    }
}

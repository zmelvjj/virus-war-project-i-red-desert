using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StageProgressUI : MonoBehaviour
{
    [SerializeField] UnityEngine.UI.Slider progressBar;
    [SerializeField] TMP_Text progressText;

    StageManager m_StageManager;

    public void Initialize(
        UnityEngine.UI.Slider slider,
        TMP_Text text)
    {
        progressBar = slider;
        progressText = text;
    }

    void OnEnable()
    {
        AttachManager();
    }

    void Start()
    {
        AttachManager();
        RefreshCurrentState();
    }

    void OnDisable()
    {
        if (m_StageManager != null)
        {
            m_StageManager.OnStageChanged -= HandleStageChanged;
            m_StageManager.OnStageProgressChanged -= HandleProgressChanged;
        }

        m_StageManager = null;
    }

    void AttachManager()
    {
        if (!StageManager.HasInstance
            || m_StageManager == StageManager.Instance)
        {
            return;
        }

        if (m_StageManager != null)
        {
            m_StageManager.OnStageChanged -= HandleStageChanged;
            m_StageManager.OnStageProgressChanged -= HandleProgressChanged;
        }

        m_StageManager = StageManager.Instance;
        m_StageManager.OnStageChanged += HandleStageChanged;
        m_StageManager.OnStageProgressChanged += HandleProgressChanged;
    }

    void RefreshCurrentState()
    {
        if (m_StageManager == null || m_StageManager.CurrentStageData == null)
            return;

        HandleProgressChanged(
            m_StageManager.CurrentKillCount,
            m_StageManager.NextRequiredKillCount);
    }

    void HandleStageChanged(StageData stage)
    {
        RefreshCurrentState();
    }

    void HandleProgressChanged(int current, int required)
    {
        if (required <= 0)
        {
            progressBar.value = 1f;
            progressText.text = "MAX";
            return;
        }

        progressBar.value = Mathf.Clamp01((float)current / required);
        progressText.text = $"{current} / {required}";
    }
}

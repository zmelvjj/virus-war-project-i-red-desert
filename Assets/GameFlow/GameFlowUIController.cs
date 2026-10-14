using UnityEngine;

[DisallowMultipleComponent]
public sealed class GameFlowUIController : MonoBehaviour
{
    [SerializeField] GameStaminaController staminaController;
    [SerializeField] GameObject mainGameHud;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] GameObject skillTreeRoot;
    [SerializeField] GameObject upgradeRestartButtonRoot;
    // TEMP: Upgrade 화면 전용 UI 정리가 끝날 때까지 UI/RawImage를 직접 숨긴다.
    [SerializeField] GameObject temporaryUpgradeHiddenRawImage;
    [SerializeField] UnityEngine.UI.Slider staminaBar;
    [SerializeField] UnityEngine.UI.Button restartButton;
    [SerializeField] UnityEngine.UI.Button upgradeButton;
    [SerializeField] UnityEngine.UI.Button upgradeRestartButton;

    void OnEnable()
    {
        staminaController.StaminaChanged += HandleStaminaChanged;
        restartButton.onClick.AddListener(HandleRestartClicked);
        upgradeButton.onClick.AddListener(HandleUpgradeClicked);
        upgradeRestartButton.onClick.AddListener(HandleUpgradeRestartClicked);
    }

    void Start()
    {
        HandleStaminaChanged(
            staminaController.CurrentStamina,
            staminaController.CurrentMaxStamina);
    }

    void OnDisable()
    {
        staminaController.StaminaChanged -= HandleStaminaChanged;
        restartButton.onClick.RemoveListener(HandleRestartClicked);
        upgradeButton.onClick.RemoveListener(HandleUpgradeClicked);
        upgradeRestartButton.onClick.RemoveListener(HandleUpgradeRestartClicked);
    }

    public void ApplyState(GameFlowState state)
    {
        mainGameHud.SetActive(state != GameFlowState.Upgrade);
        gameOverPanel.SetActive(state == GameFlowState.GameOver);
        skillTreeRoot.SetActive(state == GameFlowState.Upgrade);
        upgradeRestartButtonRoot.SetActive(state == GameFlowState.Upgrade);
        temporaryUpgradeHiddenRawImage.SetActive(state != GameFlowState.Upgrade);
    }

    void HandleStaminaChanged(float current, float maximum)
    {
        staminaBar.value = current / maximum;
    }

    void HandleRestartClicked()
    {
        GameFlowManager.Instance.RestartRun();
    }

    void HandleUpgradeClicked()
    {
        GameFlowManager.Instance.OpenUpgrade();
    }

    void HandleUpgradeRestartClicked()
    {
        GameFlowManager.Instance.RestartRun();
    }
}

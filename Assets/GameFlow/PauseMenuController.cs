using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class PauseMenuController : MonoBehaviour
{
    [SerializeField] GameObject menuRoot;
    [SerializeField] GameObject menuButtonsRoot;
    [SerializeField] GameObject settingsRoot;
    [SerializeField] UnityEngine.UI.Button continueButton;
    [SerializeField] UnityEngine.UI.Button settingsButton;
    [SerializeField] UnityEngine.UI.Button quitButton;

    GameFlowManager m_GameFlowManager;
    PauseMenuPage m_CurrentPage;

    enum PauseMenuPage
    {
        Main,
        Settings
    }

    void Start()
    {
        m_GameFlowManager = GameFlowManager.Instance;
        m_GameFlowManager.OnPauseChanged += HandlePauseChanged;
        continueButton.onClick.AddListener(HandleContinueClicked);
        settingsButton.onClick.AddListener(HandleSettingsClicked);
        quitButton.onClick.AddListener(HandleQuitClicked);
        HandlePauseChanged(GameFlowManager.IsPaused);
    }

    void Update()
    {
        if (Keyboard.current == null
            || !Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            return;
        }

        if (!GameFlowManager.IsPaused)
        {
            if (GameFlowManager.IsMainGameState)
                m_GameFlowManager.SetPaused(true);

            return;
        }

        if (m_CurrentPage == PauseMenuPage.Settings)
            ShowMainMenu();
        else
            m_GameFlowManager.ResumeGame();
    }

    void OnDestroy()
    {
        if (m_GameFlowManager != null)
            m_GameFlowManager.OnPauseChanged -= HandlePauseChanged;

        continueButton.onClick.RemoveListener(HandleContinueClicked);
        settingsButton.onClick.RemoveListener(HandleSettingsClicked);
        quitButton.onClick.RemoveListener(HandleQuitClicked);
    }

    void HandlePauseChanged(bool paused)
    {
        menuRoot.SetActive(paused);
        if (paused)
            ShowMainMenu();
    }

    void ShowMainMenu()
    {
        m_CurrentPage = PauseMenuPage.Main;
        menuButtonsRoot.SetActive(true);
        settingsRoot.SetActive(false);
    }

    void ShowSettings()
    {
        m_CurrentPage = PauseMenuPage.Settings;
        menuButtonsRoot.SetActive(false);
        settingsRoot.SetActive(true);
    }

    void HandleContinueClicked()
    {
        m_GameFlowManager.ResumeGame();
    }

    void HandleSettingsClicked()
    {
        ShowSettings();
    }

    void HandleQuitClicked()
    {
        m_GameFlowManager.ResumeGame();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

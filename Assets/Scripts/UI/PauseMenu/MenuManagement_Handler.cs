using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuManagement_Handler : MonoBehaviour {
    public static MenuManagement_Handler Instance;

    [Header("Screens")]
    [SerializeField] private GameObject m_mainPauseScreen;
    [SerializeField] private GameObject m_settingsScreen;

    [Header("References")]
    [SerializeField] private UI_SettingsVideo m_videoSettings;
    [SerializeField] private UI_SettingsTabHandler m_tabHandler;

    [Header("Buttons")]
    [SerializeField] private Button m_resume;
    [SerializeField] private Button m_settings;
    [SerializeField] private Button m_mainMenu;
    [SerializeField] private Button m_returnToPause;

    [Header("Pop up")]
    [SerializeField] private PopUp m_popUpWindow;

    private Player_PauseHandler playerReference;
    private GameObject currentScreen;

    private const string MainMenuTitleMessage = "Warning";
    private const string MainMenuMessage = "Are you sure you want to go back to the main menu? This will end the lobby. <color=red>All your unsaved data will be lost!</color>";
    private const string AcceptMessage = "Accept";
    private const string CancelMessage = "Cancel";

    #region Setup
    private void Awake() {
        Instance = this;
    }

    private void Start() {
        SetupButtons();
    }

    private void SetupButtons() {
        m_resume.onClick.RemoveAllListeners();
        m_settings.onClick.RemoveAllListeners();
        m_mainMenu.onClick.RemoveAllListeners();
        m_returnToPause.onClick.RemoveAllListeners();

        m_resume.onClick.AddListener(CloseMainPauseMenu);
        m_settings.onClick.AddListener(OpenSettingsScreen);
        m_mainMenu.onClick.AddListener(GoBackToMainMenu);
        m_returnToPause.onClick.AddListener(OpenPauseScreen);
    }

    public void SetupPlayerReference(Player_PauseHandler reference) => playerReference = reference;
    #endregion

    private void ChangeScreen(GameObject newScreen) {
        if (currentScreen != null)
            currentScreen.SetActive(false);
        currentScreen = newScreen;

        newScreen.SetActive(true);
    }

    public void OpenMainPauseMenu() {   
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Singleton.Instance.GameEvents.OnGamePaused?.Invoke();

        m_settingsScreen.SetActive(false);
        OpenPauseScreen();
    }

    private void OpenPauseScreen() => ChangeScreen(m_mainPauseScreen);

    private void OpenSettingsScreen() {
        ChangeScreen(m_settingsScreen);
        m_tabHandler.OnSettingsScreensOpened();
    }

    private void CloseMainPauseMenu() => playerReference.ResumeGame();

    #region Settings
    public void ClosePauseMenu(UnityAction OnSaved) {
        OnSaved += SaveSettingsOnClose;
        m_videoSettings.HasChangesToApply(OnSaved); 
    }

    private void SaveSettingsOnClose() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        m_videoSettings.OnSettingsClosed();
        m_tabHandler.OnSettingsScreensClosed();

        Singleton.Instance.GameEvents.OnGameResumed?.Invoke();
    }
    #endregion

    #region Main menu
    private void GoBackToMainMenu() {
        m_popUpWindow.Setup(MainMenuTitleMessage, MainMenuMessage, 
            AcceptMessage, CancelMessage, 
            DisconnectPlayersAndGoToMenu, m_popUpWindow.ClosePopUp);
        m_popUpWindow.OpenPopUp();
    }

    private void DisconnectPlayersAndGoToMenu() {

    }
    #endregion
}

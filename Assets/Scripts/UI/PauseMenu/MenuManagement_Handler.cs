using DelightStudio.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuManagement_Handler : MonoBehaviour {
    public static MenuManagement_Handler Instance;

    [Header("Screens")]
    [SerializeField] private GameObject m_mainPauseScreen;
    [SerializeField] private GameObject m_settingsScreen;
    [SerializeField] private GameObject m_tutorialScreen;
    [SerializeField] private GameObject m_statisticsScreen;
    [SerializeField] private GameObject m_inventoryScreen;
    [SerializeField] private GameObject m_bestiaryScreen;

    [Header("References")]
    [SerializeField] private UI_SettingsVideo m_videoSettings;
    [SerializeField] private UI_SettingsTabHandler m_tabHandler;
    [SerializeField] private UI_Drawing m_notes;

    [Header("Buttons")]
    [SerializeField] private Button m_resume;
    [SerializeField] private Button m_settings;
    [SerializeField] private Button m_tutorial;
    [SerializeField] private Button m_statistics;
    [SerializeField] private Button m_inventory;
    [SerializeField] private Button m_bestiary;
    [SerializeField] private Button m_mainMenu;
    [SerializeField] private Button[] m_returnToPause;

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
        DisableAllScreens();
    }

    private void SetupButtons() {
        m_resume.onClick.RemoveAllListeners();
        m_settings.onClick.RemoveAllListeners();
        m_tutorial.onClick.RemoveAllListeners();
        m_statistics.onClick.RemoveAllListeners();
        m_mainMenu.onClick.RemoveAllListeners();
        m_inventory.onClick.RemoveAllListeners();
        m_bestiary.onClick.RemoveAllListeners();

        m_resume.onClick.AddListener(CloseMainPauseMenu);
        m_settings.onClick.AddListener(OpenSettingsScreen);
        m_tutorial.onClick.AddListener(OpenTutorialScreen);
        m_statistics.onClick.AddListener(OpenStatisticsScreen);
        m_inventory.onClick.AddListener(OpenInventoryScreen);
        m_bestiary.onClick.AddListener(OpenBestiaryScreen);
        m_mainMenu.onClick.AddListener(GoBackToMainMenu);

        foreach (var btn in m_returnToPause) {
            Button thisButton = btn;
            thisButton.onClick.AddListener(OpenPauseScreen);
        }        
    }

    private void OnDestroy() {
        foreach (var btn in m_returnToPause) {
            Button thisButton = btn;
            thisButton.onClick.RemoveAllListeners();
        }
    }

    private void DisableAllScreens() {
        m_mainPauseScreen.SetActive(true);
        m_settingsScreen.SetActive(false);
        m_tutorialScreen.SetActive(false);
        m_statisticsScreen.SetActive(false);
        m_inventoryScreen.SetActive(true);
        m_bestiaryScreen.SetActive(false);
    }

    public void SetupPlayerReference(Player_PauseHandler reference) => playerReference = reference;
    #endregion

    private void ChangeScreen(GameObject newScreen) {
        if (currentScreen == m_mainPauseScreen)
            m_notes.OnNotesEnabled(false);

        if (currentScreen != null)
            currentScreen.SetActive(false);
        currentScreen = newScreen;

        newScreen.SetActive(true);

        Singleton.Instance.GameEvents.OnScreenSwitch?.Invoke();
    }

    public void OpenMainPauseMenu() {   
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Singleton.Instance.GameEvents.OnGamePaused?.Invoke();

        OpenPauseScreen();
    }

    public void OpenInventoryMenu() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Singleton.Instance.GameEvents.OnInventoryOpened?.Invoke();

        OpenInventoryScreen();
    }

    private void OpenInventoryScreen() => ChangeScreen(m_inventoryScreen);

    private void OpenPauseScreen() {
        ChangeScreen(m_mainPauseScreen);
        m_notes.OnNotesEnabled(true);
    }

    private void OpenSettingsScreen() {
        ChangeScreen(m_settingsScreen);
        m_tabHandler.OnSettingsScreensOpened();
    }

    private void OpenTutorialScreen() {
        ChangeScreen(m_tutorialScreen);
    }

    private void OpenStatisticsScreen() {
        ChangeScreen(m_statisticsScreen);
    }

    private void OpenBestiaryScreen() {
        ChangeScreen(m_bestiaryScreen);
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

        Singleton.Instance.GameEvents.OnScreenSwitch?.Invoke();

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

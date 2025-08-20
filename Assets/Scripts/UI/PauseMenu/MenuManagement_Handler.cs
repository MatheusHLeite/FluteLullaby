using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MenuManagement_Handler : MonoBehaviour {
    private InputHandler Input;

    [Header("Menus")]
    [SerializeField] private GameObject m_pauseMenu;
    [SerializeField] private GameObject m_inventoryMenu;

    [Header("References")]
    [SerializeField] private UI_SettingsVideo m_videoSettings;

    [Header("Screens")]
    [SerializeField] private GameObject m_mainPauseScreen;
    [SerializeField] private GameObject m_settingsScreen;
    [SerializeField] private GameObject m_inventoryScreen;

    [Header("Buttons")]
    [SerializeField] private Button m_resume;
    [SerializeField] private Button m_settings;
    [SerializeField] private Button m_mainMenu;

    [Header("Pop up")]
    [SerializeField] private PopUp m_popUpWindow;

    [Header("Events")]
    [SerializeField] private UnityEvent OnWindowClose;    
    [SerializeField] private UnityEvent OnSettingsWindowClose;
    [SerializeField] private UnityEvent OnSettingsWindowOpened;

    private bool windowOpened;
    private GameObject actualMenu;
    private GameObject actualScreen;

    private const string MainMenuTitleMessage = "Warning";
    private const string MainMenuMessage = "Are you sure you want to go back to the main menu? This will end the lobby. <color=red>All your unsaved data will be lost!</color>";
    private const string AcceptMessage = "Accept";
    private const string CancelMessage = "Cancel";

    private void Awake() {
        Input = Singleton.Instance.InputHandler;

        SetupButtons();
    }

    private void Start() {      
        m_pauseMenu.SetActive(false);
        m_settingsScreen.SetActive(false);
        m_inventoryScreen.SetActive(false);
    }

    private void SetupButtons() {
        m_resume.onClick.RemoveAllListeners();
        m_settings.onClick.RemoveAllListeners();
        m_mainMenu.onClick.RemoveAllListeners();

        m_resume.onClick.AddListener(CloseMainPauseMenu);
        m_settings.onClick.AddListener(OpenSettingsScreen);
        m_mainMenu.onClick.AddListener(GoBackToMainMenu);
    }

    private void OpenMenu(GameObject menu) {
        windowOpened = true;
        actualMenu = menu;

        menu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMenu(GameObject menu) {
        OnWindowClose?.Invoke();

        windowOpened = false;

        menu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ChangeScreen(GameObject previousScreen, GameObject newScreen) {
        previousScreen.SetActive(false);
        newScreen.SetActive(true);

        actualScreen = newScreen;
    }

    private void OpenInventoryMenu() {
        OpenMenu(m_inventoryMenu);
        Singleton.Instance.GameEvents.OnInventoryOpened?.Invoke();
    }

    private void CloseInventoryMenu() {
        CloseMenu(m_inventoryMenu);
        Singleton.Instance.GameEvents.OnGameResumed?.Invoke();
    }

    private void OpenMainPauseMenu() {
        OpenMenu(m_pauseMenu);
        Singleton.Instance.GameEvents.OnGamePaused?.Invoke();
    }

    private void CloseMainPauseMenu() {
        CloseMenu(m_pauseMenu);
        Singleton.Instance.GameEvents.OnGameResumed?.Invoke();
    }

    private void OpenSettingsScreen() {
        ChangeScreen(m_mainPauseScreen, m_settingsScreen);
        OnSettingsWindowOpened?.Invoke();
    }

    private void OnGoBackToMainPauseMenuActionCalled() {
        m_videoSettings.HasChangesToApply(GoBackToMainPauseMenu);
    }

    private void GoBackToMainPauseMenu() {
        m_videoSettings.OnSettingsClosed();
        ChangeScreen(actualScreen, m_mainPauseScreen);
        OnSettingsWindowClose?.Invoke();
    }

    private void GoBackToMainMenu() {
        m_popUpWindow.Setup(MainMenuTitleMessage, MainMenuMessage, AcceptMessage, CancelMessage, DisconnectPlayersAndGoToMenu, m_popUpWindow.ClosePopUp);
        m_popUpWindow.OpenPopUp();
    }

    private void DisconnectPlayersAndGoToMenu() {

    }

    private void HandleInputs() {
        if (Input.Pause) {
            if (!windowOpened) {
                OpenMainPauseMenu();
                return;
            }

            if (actualMenu == m_pauseMenu)  {
                if (actualScreen == m_mainPauseScreen) {
                    CloseMainPauseMenu();
                    return;
                }

                if (actualScreen == m_settingsScreen) {
                    OnGoBackToMainPauseMenuActionCalled();
                    return;
                }
            }
            else if (actualMenu == m_inventoryMenu)  {
                CloseInventoryMenu();
                return;
            }
        }

        if (Input.Inventory) {
            if (!windowOpened) {
                OpenInventoryMenu();
                return;
            }

            if (actualMenu == m_inventoryMenu) {
                CloseInventoryMenu();
            }
        }
    }

    private void Update() {
        HandleInputs();
    }
}

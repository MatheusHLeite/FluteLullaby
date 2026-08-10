using DelightStudio.UI;
using UnityEngine;

public class Player_PauseHandler : MonoBehaviour {
    private InputHandler Input;
    private Player_CameraMovementSystem Camera;

    [Header("Setup")]
    [SerializeField] private GameObject m_diary;
    [SerializeField] private Animator m_handsAnimator;

    private const string DIARY_ON_TRIGGER = "Pause";
    private const string DIARY_ON_BOOL = "Paused";

    bool isPaused => GameManager.GetGameState() == GameState.Paused;
    bool isResumed => GameManager.GetGameState() == GameState.Resumed;
    bool isInventoryOpened => GameManager.GetGameState() == GameState.InventoryOpened;

    #region Initialization
    private void Awake() {
        Input = Singleton.Instance.InputHandler;
        Camera = GetComponent<Player_CameraMovementSystem>();
    }

    private void Start() {
        PauseInteractionProcessor.Instance.SetPlayerCamera(Camera.GetPlayerCamera);
        MenuManagement_Handler.Instance.SetupPlayerReference(this);
    }
    #endregion

    private void HandleDiaryAnimation(bool putOnAnimation) {
        m_handsAnimator.SetBool(DIARY_ON_BOOL, putOnAnimation);

        if (putOnAnimation)
            m_handsAnimator.SetTrigger(DIARY_ON_TRIGGER);        
    }

    #region Pause
    private void HandlePause() {
        if (isInventoryOpened) {
            HandleInventory();
            return; 
        }

        if (isResumed) 
            PauseGame();        
        else if (isPaused) 
            ResumeGame();        
    }

    private void PauseGame() {
        MenuManagement_Handler.Instance.OpenMainPauseMenu();
        HandleDiaryAnimation(true);
    }
    #endregion

    #region Inventory
    private void HandleInventory() {
        if (isPaused)
            return;

        if (isResumed)
            OpenInventory();
        else if (isInventoryOpened)
            ResumeGame();
    }

    private void OpenInventory() {
        HandleDiaryAnimation(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Singleton.Instance.GameEvents.OnInventoryOpened?.Invoke();
    }
    #endregion

    public void ResumeGame() {
        MenuManagement_Handler.Instance.ClosePauseMenu(() => {
            HandleDiaryAnimation(false);
        });
    }

    private void Update() {
        if (Input.Pause) {
            HandlePause();            
            return;
        }

        if (Input.Inventory) {
            HandleInventory();
            return;
        }
    }
}

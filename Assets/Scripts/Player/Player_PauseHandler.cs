using DelightStudio.UI;
using UnityEngine;

public class Player_PauseHandler : MonoBehaviour {
    private InputHandler Input;
    private Player_CameraMovementSystem Camera;

    [Header("Setup")]
    [SerializeField] private DiaryPageSurface m_diaryCollider;
    [SerializeField] private GameObject m_diaryVisual;
    [SerializeField] private Animator m_handsAnimator;

    private float minTime;
    private float cooldownTime;

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

    public void InitializeNetwork(bool isOwner) {
        SetDiaryVisibility(false);

        if (!isOwner) return;

        PauseInteractionProcessor.Instance.SetPlayerReferences(Camera.GetPlayerCamera, m_diaryCollider);
        MenuManagement_Handler.Instance.SetupPlayerReference(this);
    }
    #endregion

    private void HandleDiaryAnimation(bool putOnAnimation) {
        m_handsAnimator.SetBool(DIARY_ON_BOOL, putOnAnimation);

        

        if (putOnAnimation) {
            m_handsAnimator.SetTrigger(DIARY_ON_TRIGGER);
            SetDiaryVisibility(true);
        }      
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

        SetCooldown();
    }

    private void PauseGame() {
        cooldownTime = 0.2f;

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

        SetCooldown();
    }

    private void OpenInventory() {
        cooldownTime = 0.2f;

        MenuManagement_Handler.Instance.OpenInventoryMenu();
        HandleDiaryAnimation(true);
    }
    #endregion

    public void ResumeGame() {
        cooldownTime = 0.355f;

        MenuManagement_Handler.Instance.ClosePauseMenu(() => {
            HandleDiaryAnimation(false);
        });
    }

    public void SetDiaryVisibility(bool visible) => m_diaryVisual.SetActive(visible);

    private void SetCooldown() {
        minTime = Time.time + cooldownTime;
    }

    public void Tick(bool isOwner) {
        if (!isOwner) return;

        if (Time.time < minTime) return;

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

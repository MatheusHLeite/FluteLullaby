using DelightStudio.UI;
using UnityEngine;

namespace DelightStudio.Player {
    public class Player_PauseHandler : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private DiaryPageSurface m_diaryCollider;
        [SerializeField] private GameObject m_diaryVisual;

        private InputHandler Input;
        private Player_CameraMovementSystem Camera;
        private Player_FlaskManager FlaskManager;
        private Player_HealthSystem HealthSystem;
        private Player_AnimationSystem AnimationSystem;

        private float minTime;
        private float cooldownTime;

        bool isPaused => GameManager.GetGameState() == GameState.Paused;
        bool isResumed => GameManager.GetGameState() == GameState.Resumed;
        bool isInventoryOpened => GameManager.GetGameState() == GameState.InventoryOpened;

        #region Initialization
        private void Awake() {
            Input = Singleton.Instance.InputHandler;
            Camera = GetComponent<Player_CameraMovementSystem>();
            FlaskManager = GetComponent<Player_FlaskManager>();
            HealthSystem = GetComponent<Player_HealthSystem>();
            AnimationSystem = GetComponent<Player_AnimationSystem>();
        }

        public void InitializeNetwork(bool isOwner) {
            SetDiaryVisibility(false);

            if (!isOwner) return;

            PauseInteractionProcessor.Instance.SetPlayerReferences(Camera.GetFirstPersonCamera, m_diaryCollider);
            MenuManagement_Handler.Instance.SetupPlayerReference(this);
        }
        #endregion

        private void HandleDiaryAnimation(bool putOnAnimation) {
            AnimationSystem.OnDiaryOpened(putOnAnimation);
            Camera.ResetHandMovementEffect();

            if (!putOnAnimation)
                return;

            SetDiaryVisibility(true);
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
            if (FlaskManager.IsDrinking)
                return;

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
            if (FlaskManager.IsDrinking)
                return;

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
            if (!isOwner) 
                return;

            if (Time.time < minTime || HealthSystem.IsDead)
                return;

            if (Input.Pause) {
                HandlePause();
                return;
            }

            if (Input.Inventory) {
                HandleInventory();
            }
        }
    }
}
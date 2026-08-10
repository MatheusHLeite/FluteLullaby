using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class PauseInteractionProcessor : MonoBehaviour {
        public static PauseInteractionProcessor Instance;

        [Header("Setup")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private RenderTexture m_renderTexture;

        [Header("Screens")]
        [SerializeField] private GameObject m_pauseMenu;
        [SerializeField] private GameObject m_inventoryMenu;

        private GraphicRaycaster graphicRaycaster;
        private Camera playerCamera;

        private bool interacting;

        void Awake() {
            Instance = this;

            graphicRaycaster = m_canvas.GetComponent<GraphicRaycaster>();

            Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
            Singleton.Instance.GameEvents.OnInventoryOpened.AddListener(OnInventoryOpened);
            Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
            Singleton.Instance.GameEvents.OnInventoryOpened.RemoveListener(OnInventoryOpened);
            Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
        }

        public void SetPlayerCamera(Camera cam) {
            playerCamera = cam;            
        }

        private void OnGamePaused() => SelectCurrentScreen(true);

        private void OnInventoryOpened() => SelectCurrentScreen(false);

        private void OnGameResumed() => SetScreenState(false);


        public void SetScreenState(bool isPaused) => interacting = isPaused;

        public void SelectCurrentScreen(bool isPauseMenu) {
            SetScreenState(true);

            m_pauseMenu.SetActive(isPauseMenu);
            m_inventoryMenu.SetActive(!isPauseMenu);
        }

        #region Interaction
        void ProcessInteraction(Vector2 screenPosition) {
            Ray ray = playerCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit))
                return;

            Vector2 uv = hit.textureCoord;

            Vector2 rtPosition = new Vector2(
                uv.x * m_renderTexture.width,
                uv.y * m_renderTexture.height
            );

            PointerEventData pointerData = new PointerEventData(EventSystem.current);

            pointerData.position = rtPosition;
            pointerData.button = PointerEventData.InputButton.Left;

            List<RaycastResult> results = new();

            graphicRaycaster.Raycast(pointerData, results);

            foreach (RaycastResult result in results) {
                ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
            }
        }

        void Update() {
            if (!interacting) return;

            if (Mouse.current.leftButton.wasPressedThisFrame)
                ProcessInteraction(Mouse.current.position.ReadValue());
        }
        #endregion
    }
}
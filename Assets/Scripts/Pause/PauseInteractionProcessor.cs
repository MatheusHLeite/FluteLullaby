using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class PauseInteractionProcessor : MonoBehaviour {
        public static PauseInteractionProcessor Instance;

        [Header("UI")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private Camera m_canvasCamera;

        [Header("Book")]
        [SerializeField] private LayerMask m_bookLayer;

        [Header("Screens")]
        [SerializeField] private GameObject m_pauseMenu;
        [SerializeField] private GameObject m_inventoryMenu;

        private Camera playerCamera;
        private DiaryPageSurface pageSurface;
        private GraphicRaycaster graphicRaycaster;

        private bool interacting;

        private GameObject currentPointerObject; 
        private GameObject pressedObject;

        public static PointerEventData pointerData;

        #region Initialization
        private void Awake() {
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
        #endregion

        #region Events
        private void OnGamePaused() {
            SelectCurrentScreen(true);
        }

        private void OnInventoryOpened() {
            SelectCurrentScreen(false);
        }

        private void OnGameResumed() {
           SetScreenState(false);
        }
        #endregion

        #region Sets
        public void SetPlayerReferences(Camera cam, DiaryPageSurface surface) {
            pageSurface = surface;
            playerCamera = cam;
        }

        public void SetScreenState(bool isPaused) {
            interacting = isPaused;

            if (!isPaused)
                ClearPointer();
        }

        public void SelectCurrentScreen(bool isPauseMenu) {
            SetScreenState(true);

            m_pauseMenu.SetActive(isPauseMenu);
            m_inventoryMenu.SetActive(!isPauseMenu);
        }
        #endregion

        #region Cleanup

        private void ClearHover(PointerEventData pointerData) {
            if (currentPointerObject == null)
                return;

            if (pointerData == null) {
                pointerData =
                    new PointerEventData(
                        EventSystem.current
                    );
            }

            ExecuteEvents.Execute(
                currentPointerObject,
                pointerData,
                ExecuteEvents.pointerExitHandler
            );

            currentPointerObject = null;
        }

        private void ClearPress() {
            pressedObject = null;
        }

        private void ClearPointer() {
            currentPointerObject = null;
            pressedObject = null;
        }
        #endregion

        #region Input
        private void ProcessInput()  {
            if (Mouse.current == null)
                return;

            Vector2 screenPosition = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame)            
                ProcessPointerDown(screenPosition);            

            ProcessPointerMove(screenPosition);

            if (Mouse.current.leftButton.wasReleasedThisFrame)            
                ProcessPointerUp(screenPosition);            
        }
        #endregion

        #region Pointer
        private void ProcessPointerMove(Vector2 screenPosition) {
            if (!TryGetPointerData(screenPosition, out pointerData, out List<RaycastResult> results)) {
                ClearHover(pointerData);
                return;
            }

            GameObject newObject = results.Count > 0 ? results[0].gameObject : null;

            if (newObject != currentPointerObject) {
                if (currentPointerObject != null)
                    ExecuteEvents.Execute(currentPointerObject, pointerData, ExecuteEvents.pointerExitHandler);

                currentPointerObject = newObject;

                if (currentPointerObject != null)
                    ExecuteEvents.Execute(currentPointerObject, pointerData, ExecuteEvents.pointerEnterHandler);
            }

            if (pressedObject != null) { 
                pointerData.button = PointerEventData.InputButton.Left;

                ExecuteEvents.Execute(pressedObject, pointerData, ExecuteEvents.dragHandler);
            }
        }

        private void ProcessPointerDown(Vector2 screenPosition) {
            if (!TryGetPointerData(screenPosition, out PointerEventData pointerData, out List<RaycastResult> results)) 
                return;
            
            if (results.Count == 0)
                return;

            GameObject target = results[0].gameObject;
           
            pointerData.button = PointerEventData.InputButton.Left;

            pressedObject = target;

            pointerData.pointerPress = target;
            pointerData.rawPointerPress = target;

            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
        }

        private void ProcessPointerUp(Vector2 screenPosition) {
            if (!TryGetPointerData(screenPosition, out PointerEventData pointerData, out List<RaycastResult> results)) {
                ClearPress();
                return;
            }

            GameObject currentObject =results.Count > 0 ? results[0].gameObject : null;

            pointerData.button = PointerEventData.InputButton.Left;

            if (pressedObject != null) {
                ExecuteEvents.Execute(pressedObject, pointerData, ExecuteEvents.pointerUpHandler);

                if (currentObject == pressedObject)
                    ExecuteEvents.Execute(pressedObject, pointerData, ExecuteEvents.pointerClickHandler);
            }

            ClearPress();
        }
        #endregion

        #region Raycast
        private bool TryGetPointerData(Vector2 mouseScreenPosition, out PointerEventData pointerData, out List<RaycastResult> results) {
            pointerData = null;
            results = null;
            Ray ray = playerCamera.ScreenPointToRay(mouseScreenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, m_bookLayer, QueryTriggerInteraction.Ignore))            
                return false;            

            if (!pageSurface.TryGetNormalizedPosition(hit.point, out Vector2 normalizedPosition)) 
                return false;            

            RectTransform canvasRect = m_canvas.GetComponent<RectTransform>();
            Rect rect = canvasRect.rect;
            Vector2 canvasLocalPosition = 
                new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, normalizedPosition.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPosition.y));

            Vector3 worldPosition = canvasRect.TransformPoint(canvasLocalPosition);
            Vector2 canvasScreenPosition = m_canvasCamera.WorldToScreenPoint(worldPosition);

            pointerData = new PointerEventData(EventSystem.current) {
                position = canvasScreenPosition,
                button = PointerEventData.InputButton.Left
            };

            results = new List<RaycastResult>();

            graphicRaycaster.Raycast(pointerData, results);
            return true;
        }
        #endregion

        private void Update() {
            if (!interacting || playerCamera == null)
                return;

            ProcessInput();
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class PauseInteractionProcessor : MonoBehaviour {
        public static PauseInteractionProcessor Instance;

        [Header("References")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private Camera m_canvasCamera;
        [SerializeField] private UI_Drawing m_drawing;

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

        private GameObject dragObject;
        private bool isDragging;
        private Vector2 pressPosition;
        private const float DragThreshold = 5f;

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

            m_drawing.Setup(pageSurface, playerCamera, m_bookLayer);
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

            if (pointerData == null) 
                pointerData = new PointerEventData(EventSystem.current);            

            ExecuteEvents.Execute(currentPointerObject, pointerData, ExecuteEvents.pointerExitHandler);
            currentPointerObject = null;
        }

        private void ClearPress() {
            pressedObject = null;
            dragObject = null;
            isDragging = false;
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

        private void DebugUIElementClicked(List<RaycastResult> results) {
            GameObject target = results[0].gameObject;

            string uiLog = $"<color=green>[UI Click] Clique na UI registrado!</color>\n";
            uiLog += $"Alvo principal (O que foi efetivamente clicado): <b>{target.name}</b>\n";
            uiLog += "Fila de elementos da UI sob o ponteiro (do mais à frente pro fundo):\n";
            for (int i = 0; i < results.Count; i++)            
                uiLog += $"  {i}. {results[i].gameObject.name}\n";   
            
            Debug.Log(uiLog);
        }

        #region Pointer
        private void ProcessPointerMove(Vector2 screenPosition) {
            if (!TryGetPointerData(screenPosition, out pointerData, out List<RaycastResult> results)) {
                ClearHover(pointerData);
                return;
            }

            if (results.Count > 0)
                pointerData.pointerCurrentRaycast = results[0];

            GameObject newObject = results.Count > 0 ? results[0].gameObject : null;

            if (newObject != currentPointerObject) {
                if (currentPointerObject != null)
                    ExecuteEvents.Execute(currentPointerObject, pointerData, ExecuteEvents.pointerExitHandler);

                currentPointerObject = newObject;

                if (currentPointerObject != null)
                    ExecuteEvents.Execute(currentPointerObject, pointerData, ExecuteEvents.pointerEnterHandler);
            }

            if (pressedObject == null)
                return;

            pointerData.button = PointerEventData.InputButton.Left;

            if (!isDragging) {
                float distance = Vector2.Distance(screenPosition, pressPosition);

                if (distance < DragThreshold)
                    return;

                isDragging = true;
                dragObject = pressedObject;

                pointerData.pointerDrag = dragObject;

                ExecuteEvents.Execute(dragObject, pointerData, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(dragObject, pointerData, ExecuteEvents.beginDragHandler);
            }

            pointerData.pointerDrag = dragObject;
            ExecuteEvents.Execute(dragObject, pointerData, ExecuteEvents.dragHandler);
        }

        private void ProcessPointerDown(Vector2 screenPosition) {
            if (!TryGetPointerData(screenPosition, out PointerEventData pointerData, out List<RaycastResult> results)) 
                return;
            
            if (results.Count == 0)
                return;

            GameObject target = results[0].gameObject;

            DebugUIElementClicked(results);

            pressedObject = target;
            pressPosition = screenPosition;

            pointerData.button = PointerEventData.InputButton.Left;
            pointerData.pointerPress = target;
            pointerData.rawPointerPress = target;

            ExecuteEvents.Execute(target, pointerData, ExecuteEvents.pointerDownHandler);
        }

        private void ProcessPointerUp(Vector2 screenPosition) {
            if (!TryGetPointerData(screenPosition, out PointerEventData pointerData, out List<RaycastResult> results)) {
                if (isDragging && dragObject != null && pointerData != null) 
                    ExecuteEvents.Execute(dragObject, pointerData, ExecuteEvents.endDragHandler);                

                ClearPress();
                return;
            }

            if (results.Count > 0)
                pointerData.pointerCurrentRaycast = results[0];

            GameObject currentObject =results.Count > 0 ? results[0].gameObject : null;

            pointerData.button = PointerEventData.InputButton.Left;

            if (pressedObject != null) {
                pointerData.pointerPress = pressedObject;
                pointerData.rawPointerPress = pressedObject;

                if (isDragging) {
                    pointerData.pointerDrag = dragObject;

                    ExecuteEvents.Execute(dragObject, pointerData, ExecuteEvents.endDragHandler);
                }
                else{
                    ExecuteEvents.Execute(pressedObject, pointerData, ExecuteEvents.pointerUpHandler );

                    if (currentObject == pressedObject) 
                        ExecuteEvents.Execute(pressedObject, pointerData, ExecuteEvents.pointerClickHandler);                    
                }
            }

            ClearPress();
        }
        #endregion

        #region Raycast
        private bool TryGetPointerData(Vector2 mouseScreenPosition, out PointerEventData pointerData, out List<RaycastResult> results) {
            pointerData = null;
            results = null;

            if (playerCamera == null || pageSurface == null || m_canvas == null || m_canvasCamera == null)
                return false;

            Ray ray = playerCamera.ScreenPointToRay(mouseScreenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, m_bookLayer, QueryTriggerInteraction.Ignore))
                return false;

            if (!pageSurface.TryGetNormalizedPosition(hit, out Vector2 normalizedPosition))
                return false;

            RectTransform canvasRect = m_canvas.GetComponent<RectTransform>();
            Rect rect = canvasRect.rect;

            Vector2 canvasLocalPosition = new Vector2(
                Mathf.Lerp(rect.xMin, rect.xMax, normalizedPosition.x),
                Mathf.Lerp(rect.yMin, rect.yMax, normalizedPosition.y)
            );

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
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_DragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler {
    private Transform originalParent;
    private Transform newTransform;

    private CanvasGroup canvasGroup;
    private Canvas mainCanvas;

    private UI_InventoryItem inventoryItem;

    private bool isDragging;
    private bool isReloading;

    private int index;
    private int quantity;

    private Item_SO currentItem;
    private UI_Slot lastSlot;

    private ItemData itemData;

    private RectTransform canvasRect;
    private Camera canvasCamera;

    private Vector3 dragOffset;
    private Vector2 lastPointerPosition;

    private bool isShowingPopUp;

    #region Rotation and position
    private Vector3 pointerPosition;

    public float rotationAmount = 55f;
    public float rotationSmooth = 17f;
    public float movementSmooth = 15f;

    private float deltaX;
    private float targetRotation;
    private float currentRotation;
    private Vector3 lastMousePosition;
    #endregion

    public void SetIndexAndQuantity(int index, ItemData itemData, UI_Slot slot) { 
        this.index = index;
        this.itemData = itemData;
        this.quantity = itemData.quantity; 
        lastSlot = slot;

        canvasGroup = GetComponent<CanvasGroup>();
        mainCanvas = GetComponentInParent<Canvas>();

        canvasRect = mainCanvas.GetComponent<RectTransform>();
        canvasCamera = mainCanvas.worldCamera;

        inventoryItem = GetComponent<UI_InventoryItem>();
        currentItem = inventoryItem.GetCurrentItem();

        Singleton.Instance.GameEvents.OnWeaponReload.AddListener(OnWeaponReload);
        Singleton.Instance.GameEvents.OnGameResumed.AddListener(HideTooltip);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnWeaponReload.RemoveListener(OnWeaponReload);
        Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(HideTooltip);
    }

    private void OnWeaponReload(bool isReloading, WeaponClass weapons) => this.isReloading = isReloading;

    public UI_Slot GetActualSlot() => lastSlot;

    public ItemData GetItemData() => itemData;

    public void UpdateQuantity(int quantity) { this.quantity = quantity; }

    public void OnBeginDrag(PointerEventData eventData) {
        if (isReloading) return;

        HideTooltip();

        Singleton.Instance.GameEvents.OnDragBegun?.Invoke(index);

        originalParent = transform.parent;
        transform.SetParent(mainCanvas.transform);

        canvasGroup.blocksRaycasts = false;
        isDragging = true;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            eventData.position,
            canvasCamera,
            out Vector3 worldPosition)) {
            dragOffset = transform.position - worldPosition;
            pointerPosition = transform.position;
        }

        lastMousePosition = Input.mousePosition;
        lastPointerPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) {
        if (canvasRect == null)
            return;

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            canvasRect,
            eventData.position,
            canvasCamera,
            out Vector3 worldPosition)) {
            pointerPosition = worldPosition + dragOffset;
        }

        deltaX = eventData.position.x - lastPointerPosition.x;
        targetRotation = Mathf.Clamp(
            deltaX * 0.5f,
            -rotationAmount,
            rotationAmount
        );

        lastPointerPosition = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData) {
        newTransform = originalParent;

        if (eventData.pointerCurrentRaycast.gameObject != null) {
            if (eventData.pointerCurrentRaycast.gameObject.TryGetComponent(out UI_Slot slot) && slot.emptySlot) {
                HandleSlotDrop(slot);
            }
            if (eventData.pointerCurrentRaycast.gameObject.TryGetComponent(out UI_DragDropHandler otherItem)) {
                HandleSlotSwap(otherItem);
                return;
            }
        }

        HideTooltip();
        OnItemDropped();
    }

    public void OnPointerClick(PointerEventData eventData) {
        bool shiftPressed =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (eventData.button == PointerEventData.InputButton.Right && shiftPressed) {
            HandleItemSplit(itemData);
            return;
        }

        if (!isShowingPopUp || TooltipSystem.CurrentTooltipItem != currentItem) {         
            TooltipSystem.ShowInventoryTooltip(currentItem); 
            isShowingPopUp = true;
        }
        else
            HideTooltip();
    }

    private void HideTooltip() {
        TooltipSystem.HideInventoryTooltip();
        isShowingPopUp = false;
    }

    private void HandleItemSplit(ItemData item) {
        if (isReloading) 
            return;
        if (quantity <= 1)
            return;
        if (InventoryManager.IsInventoryFull()) {
            Debug.Log("<color=red>Inventory is full</color>");
            return;
        }

        Singleton.Instance.GameEvents.OnItemSplit?.Invoke(item, quantity);
    }

    private void HandleSlotDrop(UI_Slot slot) {
        newTransform = slot.transform;

        if (lastSlot != null && lastSlot != slot) lastSlot.ClearSlot();
        lastSlot = slot;

        slot.OnInventoryItemSlotChanged(itemData, quantity, index);
        index = slot.GetIndex();
    }

    private void HandleSlotSwap(UI_DragDropHandler otherItem) {
        if (otherItem.currentItem.id == currentItem.id) {
            Singleton.Instance.SaveManager.OnItemStackUpdated(itemData, otherItem.GetItemData());
            lastSlot.ClearSlot();
            Destroy(gameObject);
            return; 
        }

        UI_Slot otherSlot = otherItem.GetActualSlot();

        var savedTransform = lastSlot.transform;
        var savedItem = otherItem.itemData;
        var savedQuantity = otherItem.quantity;
        var savedThisIndex = otherItem.index;
        var savedNewIndex = lastSlot.GetIndex();

        otherItem.newTransform = savedTransform;
        lastSlot.OnInventoryItemSlotChanged(savedItem, savedQuantity, savedThisIndex);
        otherItem.lastSlot = lastSlot;
        otherItem.index = savedNewIndex;

        newTransform = otherSlot.transform;
        otherSlot.OnInventoryItemSlotChanged(itemData, quantity, index);
        lastSlot = otherSlot;
        index = otherSlot.GetIndex();

        otherItem.OnItemDropped();
        OnItemDropped();
    }

    public void OnItemDropped() {
        transform.SetParent(newTransform);

        canvasGroup.blocksRaycasts = true;
        isDragging = false;

        transform.rotation = Quaternion.identity;
    }

    private void LateUpdate() {
        if (!isDragging) return;

        transform.position = Vector3.Lerp(transform.position, pointerPosition, Time.deltaTime * movementSmooth);
        currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSmooth);
        transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);
    }
}

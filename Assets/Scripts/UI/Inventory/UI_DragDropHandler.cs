using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_DragDropHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {
    private Transform originalParent;
    private Transform newTransform;

    private Image image;
    private Canvas mainCanvas;

    private Item_SO currentItem;

    private bool isDragging;

    private int index;
    private int quantity;

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

    private void Awake() {
        image = GetComponentInChildren<Image>();
        mainCanvas = GetComponentInParent<Canvas>();

        currentItem = GetComponent<UI_InventoryItem>().GetCurrentItem();
    }

    public void SetIndexAndQuantity(int index, int quantity) { this.index = index; this.quantity = quantity; }

    public void OnBeginDrag(PointerEventData eventData) {
        originalParent = transform.parent;

        transform.SetParent(mainCanvas.transform);
        image.raycastTarget = false;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData) => pointerPosition = eventData.position;

    public void OnEndDrag(PointerEventData eventData) {
        newTransform = originalParent;
        if (eventData.pointerCurrentRaycast.gameObject != null && eventData.pointerCurrentRaycast.gameObject.TryGetComponent(out UI_Slot slot)) {
            newTransform = slot.transform;
            slot.OnInventoryItemSlotChanged(currentItem, quantity, index);
            index = slot.GetIndex();
        }

        transform.SetParent(newTransform);
        image.raycastTarget = true;
        isDragging = false;

        transform.rotation = Quaternion.identity;
    }

    private void LateUpdate() {
        if (!isDragging) return;

        transform.position = Vector3.Lerp(transform.position, pointerPosition, Time.deltaTime * movementSmooth);

        deltaX = Input.mousePosition.x - lastMousePosition.x;
        targetRotation = Mathf.Clamp(deltaX * 0.5f, -rotationAmount, rotationAmount);

        currentRotation = Mathf.Lerp(currentRotation, targetRotation, Time.deltaTime * rotationSmooth);
        transform.rotation = Quaternion.Euler(0f, 0f, -currentRotation);
        lastMousePosition = Input.mousePosition;
    }
}

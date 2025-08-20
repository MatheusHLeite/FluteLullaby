using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

public class UI_Slot : MonoBehaviour { 
    private int index;
    private Image thisImage;

    [ReadOnly] public bool emptySlot;
    [ReadOnly] public string id;
    [ReadOnly] public string uniqueId;

    private ItemData currentItem;
    public ItemData GetItem() => currentItem;

    #region Index
    public void SetIndex(int setIndex) { 
        index = setIndex;
        emptySlot = true;

        thisImage = GetComponent<Image>();
    }

    public int GetIndex() => index;
    #endregion

    #region Slot management
    public void ClearSlot() {
        emptySlot = true;
        id = string.Empty;
        uniqueId = string.Empty;

        currentItem = null;
        thisImage.raycastTarget = true;
    }

    public void SetupSlot(ItemData item) {
        emptySlot = false;
        id = item.id;
        uniqueId = item.uniqueId;

        currentItem = item;
        thisImage.raycastTarget = false;
    }
    #endregion

    public void OnInventoryItemSlotChanged(ItemData item, int quantity, int index) {
        SetupSlot(item);

        Singleton.Instance.SaveManager.OnInventoryItemUpdated(id, uniqueId, this.index, quantity);
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged?.Invoke(item, index, this.index);
    }
}

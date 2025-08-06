using UnityEngine;

public class UI_Slot : MonoBehaviour {
    public bool emptySlot;
    private int index;

    public void SetIndex(int setIndex) => index = setIndex;
    public int GetIndex() => index;

    public void OnInventoryItemSlotChanged(Item_SO item, int quantity, int index) {       
        Singleton.Instance.SaveManager.SaveItemData(item.id, this.index, quantity);
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged?.Invoke(item, index, this.index);
    }
}

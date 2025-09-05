using UnityEngine;

public class InventoryManager : MonoBehaviour {
    private bool[] _itemsOnSlots;

    private void Awake() {
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(OnPlayerSpawned);        
        Singleton.Instance.GameEvents.OnInventoryItemAdded.AddListener(OnInventoryItemCollected);        
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged.AddListener(OnSlotItemChanged);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.AddListener(OnInventoryItemRemoved);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(OnPlayerSpawned);             
        Singleton.Instance.GameEvents.OnInventoryItemAdded.RemoveListener(OnInventoryItemCollected);        
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged.RemoveListener(OnSlotItemChanged);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.RemoveListener(OnInventoryItemRemoved);        

        _itemsOnSlots = null;
    }

    private void OnPlayerSpawned(Player_Manager player) {
        _itemsOnSlots = new bool[UI_InventoryManager._quickSlots.Count];

        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;

        for (int i = 0; i < data.acquiredItems.Count; i++) {
            Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(data.acquiredItems[i], data.acquiredItems[i].index);
        }

        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(0);
    }

    public ItemData GetItemFromSlot(int index) => UI_InventoryManager._allSlots[index].GetItem();

    public int GetEmptySlotIndex(int startIndex) {
        for (int i = startIndex; i < UI_InventoryManager._allSlots.Count; i++) {
            if (UI_InventoryManager._allSlots[i].emptySlot)
                return i;
        }
        return -1;
    }

    public static bool IsInventoryFull() {
        for (int i = 0; i < UI_InventoryManager._allSlots.Count; i++) 
            if (UI_InventoryManager._allSlots[i].emptySlot) return false;     
        return true;
    }

    public bool CanPickUpItem(Item_SO item) {
        ItemData thisItem = Singleton.Instance.SaveManager.GetItemFromInventory(item.id);
        if ((item.m_itemType == ItemType.MeleeWeapon || item.m_itemType == ItemType.Firearm) && Singleton.Instance.SaveManager.PlayerData.acquiredItems.Contains(thisItem)) return false;
        return true;
    }

    #region Inventory management
    private void OnInventoryItemCollected(ItemData item, int index) {                
        if (index < _itemsOnSlots.Length) _itemsOnSlots[index] = true;
    }

    private void OnInventoryItemRemoved(ItemData item, int index) {   
        if (index < _itemsOnSlots.Length) _itemsOnSlots[index] = false;
    }

    private void OnSlotItemChanged(ItemData item, int prevIndex, int newIndex) {
        if (prevIndex < _itemsOnSlots.Length) _itemsOnSlots[prevIndex] = false;
        if (newIndex < _itemsOnSlots.Length) _itemsOnSlots[newIndex] = true;    
    }
    #endregion
}

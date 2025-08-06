using System.Collections.Generic;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class InventoryManager : MonoBehaviour {
    private List<UI_Slot> m_slots;
    private List<Item_SO> _inventoryItems = new List<Item_SO>();

    private Item_SO[] _itemsOnSlots;

    private void Awake() {
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(OnPlayerSpawned);        
        Singleton.Instance.GameEvents.OnInventoryItemAdded.AddListener(OnInventoryItemCollected);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.AddListener(OnInventoryItemRemoved);
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged.AddListener(OnSlotItemChanged);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(OnPlayerSpawned);             
        Singleton.Instance.GameEvents.OnInventoryItemAdded.RemoveListener(OnInventoryItemCollected);
        Singleton.Instance.GameEvents.OnInventoryItemRemoved.RemoveListener(OnInventoryItemRemoved);
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged.RemoveListener(OnSlotItemChanged);

        _inventoryItems = null;
        _itemsOnSlots = null;
    }

    private void OnPlayerSpawned() {
        m_slots = UI_InventoryManager._quickSlots;
        _inventoryItems = new List<Item_SO>(UI_InventoryManager.GetAllSlotsCount());
        _itemsOnSlots = new Item_SO[UI_InventoryManager.GetSlotsCount()];

        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;

        for (int i = 0; i < data.acquiredWeapons.Count; i++) {
            Item_SO item = Singleton.Instance.GameManager.GetItemByID(data.acquiredWeapons[i].id);
            ItemData itemData = Singleton.Instance.SaveManager.GetItemData(data.acquiredWeapons[i].id);

            Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(item, itemData.index); // TODO CHANGE
        }

        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(0);
    }

    #region Inventory management
    public void AddItem(Item_SO item, int index = -1) {
        if (index > 0) {
            Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(item, index);
            return;
        }

        for (int i = 0; i < m_slots.Count; i++) {
            if (m_slots[i].emptySlot) {
                Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(item, i);
                break;
            }
        }

        Debug.Log("Inventário cheio!");
    }

    public bool IsInventoryFull() {
        for (int i = 0; i < m_slots.Count; i++) {
            if (m_slots[i].emptySlot) return false;
        }
        return true;
    }

    private void OnInventoryItemCollected(Item_SO item, int index) {                
        _inventoryItems.Add(item);
        if (index < _itemsOnSlots.Length) _itemsOnSlots[index] = item;
    }

    private void OnInventoryItemRemoved(int index) {   
        _inventoryItems.Remove(_inventoryItems[index]);
        if (index < _itemsOnSlots.Length) _itemsOnSlots[index] = null;
    }

    private void OnSlotItemChanged(Item_SO item, int prevIndex, int newIndex) {
        if (prevIndex < _itemsOnSlots.Length) _itemsOnSlots[prevIndex] = null;
        if (newIndex < _itemsOnSlots.Length) _itemsOnSlots[newIndex] = item;    
    }
    #endregion

    public bool CanPickUpItem(Item_SO item) {
        if ((item.m_itemType == ItemType.MeleeWeapon || item.m_itemType == ItemType.Firearm) && _inventoryItems.Contains(item)) return false;
        return true;
    }

    public Item_SO GetItemFromSlot(int index) => _itemsOnSlots[index];

    public Item_SO[] GetItemSlots() => _itemsOnSlots;

    public List<Item_SO> Inventory() => _inventoryItems;
}

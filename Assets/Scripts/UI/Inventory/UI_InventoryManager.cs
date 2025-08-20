using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class UI_InventoryManager : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private UI_InventoryItem m_inventoryItemPrefab;
    [SerializeField] private List<UI_Slot> m_slots = new List<UI_Slot>();
    [SerializeField] private List<UI_Slot> m_quickSlots = new List<UI_Slot>();
    
    public static List<UI_Slot> _allSlots;
    public static List<UI_Slot> _quickSlots;

    private UI_Slot _lastSlot;
    private float _slotChangeTime = 0.125f;

    private Vector3 _increasedSlotSize => Vector3.one + new Vector3(0.15f, 0.15f, 0.15f);

    #region Initialization
    private void Awake() {
        for (int i = 0; i < m_slots.Count; i++) m_slots[i].SetIndex(i);

        _allSlots = m_slots;
        _quickSlots = m_quickSlots;
        
        Singleton.Instance.GameEvents.OnInventoryItemAdded.AddListener(OnInventoryItemAdded);
        Singleton.Instance.GameEvents.OnItemDropped.AddListener(OnSlotItemDropped);
        Singleton.Instance.GameEvents.OnSlotSelected.AddListener(OnSlotSelected);
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged.AddListener(OnSlotChanged);        
        Singleton.Instance.GameEvents.OnItemSaved.AddListener(AddItem);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnInventoryItemAdded.RemoveListener(OnInventoryItemAdded);
        Singleton.Instance.GameEvents.OnItemDropped.RemoveListener(OnSlotItemDropped);
        Singleton.Instance.GameEvents.OnSlotSelected.RemoveListener(OnSlotSelected);
        Singleton.Instance.GameEvents.OnInventoryItemSlotChanged.RemoveListener(OnSlotChanged);
        Singleton.Instance.GameEvents.OnItemSaved.RemoveListener(AddItem);

        _allSlots = null;
        _quickSlots = null;        
    }
    #endregion

    #region UI
    private void OnSlotSelected(int index)  {
        if (_lastSlot == null) _lastSlot = m_quickSlots[0];

        _lastSlot.transform.DOScale(Vector3.one, _slotChangeTime);
        m_quickSlots[index].transform.DOScale(_increasedSlotSize, _slotChangeTime);

        _lastSlot = m_quickSlots[index];
    }

    public void AddItem(ItemData itemData) {
        if (itemData.index > 0) {
            Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(itemData, itemData.index);
            return;
        }

        for (int i = 0; i < m_slots.Count; i++) {
            if (m_slots[i].emptySlot) {
                Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(itemData, i);
                break;
            }
        }
    }

    private void OnInventoryItemAdded(ItemData item, int index) {
        UI_InventoryItem slot = Instantiate(m_inventoryItemPrefab, m_slots[index].transform);
        slot.SetItem(item, index, m_slots[index]);

        if (index < m_quickSlots.Count) {
            UI_InventoryItem quickSlot = Instantiate(m_inventoryItemPrefab, m_quickSlots[index].transform);
            quickSlot.SetItem(item, index, m_quickSlots[index]);
        }

        m_slots[index].SetupSlot(item);
    }

    private void OnSlotItemDropped(int index) {
        if (m_slots[index].transform.childCount > 0) Destroy(m_slots[index].transform.GetChild(0).gameObject);

        if (index < m_quickSlots.Count && m_quickSlots[index].transform.childCount > 0) 
            Destroy(m_quickSlots[index].transform.GetChild(0).gameObject);       

        m_slots[index].emptySlot = true;
    }

    private void OnSlotChanged(ItemData item, int prevIndex, int newIndex) {
        if (prevIndex < m_quickSlots.Count && m_quickSlots[prevIndex].transform.childCount > 0) Destroy(m_quickSlots[prevIndex].transform.GetChild(0).gameObject);

        if (newIndex < m_quickSlots.Count) {
            UI_InventoryItem quickSlot = Instantiate(m_inventoryItemPrefab, m_quickSlots[newIndex].transform);
            quickSlot.SetItem(item, newIndex, m_quickSlots[newIndex]);
        }

        Singleton.Instance.GameEvents.OnQuickSlotItemUpdated?.Invoke(prevIndex, newIndex);
    }
    #endregion
}
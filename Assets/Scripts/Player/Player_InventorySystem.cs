using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_InventorySystem : NetworkBehaviour {
    private Player_InteractionSystem Interaction;

    [Header("Hands")]
    [SerializeField] private Transform m_rightHand;
    [SerializeField] private Transform m_thirdPersonRightHand;

    [Header("Setup")]
    [SerializeField] private int m_maxSlots;

    private Item_SO[] _itemsOnSlots;

    private List<Item_SO> _inventoryItems = new List<Item_SO>(6);

    private GameObject itemOnTPHand;
    private GameObject itemOnHand;

    private void Awake() {
        Interaction = GetComponent<Player_InteractionSystem>();

        _inventoryItems = new List<Item_SO>(m_maxSlots);
        _itemsOnSlots = new Item_SO[UI_PlayerHUD.GetSlotsCount()];
    }

    public override void OnDestroy() {
        _itemsOnSlots = null;
    }

    #region Network Initialization
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Singleton.Instance.GameEvents.OnSlotItemDropped.AddListener(OnSlotItemDropped);
            Singleton.Instance.GameEvents.OnSlotItemCollected.AddListener(OnItemCollected);
            Singleton.Instance.GameEvents.OnSlotSelected.AddListener(OnSlotSelected);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner)
        {
            Singleton.Instance.GameEvents.OnSlotItemDropped.RemoveListener(OnSlotItemDropped);
            Singleton.Instance.GameEvents.OnSlotItemCollected.RemoveListener(OnItemCollected);
            Singleton.Instance.GameEvents.OnSlotSelected.RemoveListener(OnSlotSelected);
        }
    }
    #endregion

    #region Get
    public Item_SO[] GetItemSlots() => _itemsOnSlots;

    public Transform GetRightHand() => m_rightHand;

    public List<Item_SO> Inventory() => _inventoryItems;
    #endregion

    #region Slot handle
    private void OnSlotSelected(int index) {
        if (itemOnHand != null) {
            Destroy(itemOnHand);
            DespawnItemOnHandServerRpc();
        }

        if (_itemsOnSlots[index] != null) {
            itemOnHand = Instantiate(_itemsOnSlots[index].m_onHandItemPrefab, m_rightHand);
            SpawnItemOnHandServerRpc(_itemsOnSlots[index].id);
        }

        Singleton.Instance.GameEvents.OnActualSlotItem?.Invoke(_itemsOnSlots[index] != null ? _itemsOnSlots[index] : null);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemOnHandServerRpc(string id) => SpawnItemOnHandClientRpc(id);

    [ClientRpc]
    private void SpawnItemOnHandClientRpc(string id) {
        if (IsOwner) return;
        itemOnTPHand = Instantiate(Singleton.Instance.GameManager.GetItemByID(id).m_thirdPersonItemPrefab, m_thirdPersonRightHand);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemOnHandServerRpc() => DespawnItemOnHandClientRpc();

    [ClientRpc]
    private void DespawnItemOnHandClientRpc() {
        if (IsOwner) return;
        Destroy(itemOnTPHand);
    }

    private void OnItemCollected(Item_SO item, int index) {
        OnNewInventoryItemCollected(item);
        _itemsOnSlots[index] = item; //[TODO] Mudar para adicionar o item no inventario se index for -1       
    }

    private void OnSlotItemDropped(int index) {
        OnInventoryItemRemoved(_itemsOnSlots[index]);

        if (m_rightHand.childCount > 0)
            Destroy(m_rightHand.GetChild(0).gameObject);

        _itemsOnSlots[index] = null;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemServerRpc(string id, Vector3 pos) {
        if (!IsServer) return;
        GameObject item = Instantiate(Singleton.Instance.GameManager.GetItemByID(id).m_collectibleItemPrefab, pos, Quaternion.LookRotation(pos));
        item.GetComponent<NetworkObject>().Spawn(true);
    }
    #endregion

    #region Inventory Handle
    private void OnNewInventoryItemCollected(Item_SO item) {        
        _inventoryItems.Add(item);
    }

    private void OnInventoryItemRemoved(Item_SO item) {
        RemoveInventoryItem(item.id);
        _inventoryItems.Remove(item);
    }

    private void RemoveInventoryItem(string id) {
        SpawnItemServerRpc(id, Interaction.GetTargetAim());
    }
    #endregion
}

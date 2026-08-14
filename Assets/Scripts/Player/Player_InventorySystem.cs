using Unity.Netcode;
using UnityEngine;

public class Player_InventorySystem : NetworkBehaviour {
    private Player_InteractionSystem Interaction;
    private Player_CombatSystem Combat;
    private Player_CameraMovementSystem CameraMovement;
    private Player_AnimationSystem Animator;

    [Header("Hands")]
    [SerializeField] private Transform m_rightHand;
    [SerializeField] private Transform m_thirdPersonRightHand;

    private Item_Interactor _currentHandItem;
    private IWeapon _currentWeaponEquipped;

    #region Private upgradable/modifiable variables
    private float changeWeaponSpeed = 0.4f;
    #endregion

    #region Private
    private Interactor itemOnTPHand;

    private Item_SO currentItem;
    private ItemData equippedItem;

    private int previousIndex;
    #endregion

    #region Initialization
    private void Awake() {
        Interaction = GetComponent<Player_InteractionSystem>();
        Combat = GetComponent<Player_CombatSystem>();
        CameraMovement = GetComponent<Player_CameraMovementSystem>();
        Animator = GetComponent<Player_AnimationSystem>();
    }
    #endregion

    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnItemDropped.AddListener(OnSlotItemDropped);
        Singleton.Instance.GameEvents.OnSlotSelected.AddListener(OnSlotSelected);
        Singleton.Instance.GameEvents.OnDragBegun.AddListener(i => previousIndex = i);
        Singleton.Instance.GameEvents.OnQuickSlotItemUpdated.AddListener(OnQuickSlotItemUpdated);
    }

    public void DeinitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnItemDropped.RemoveListener(OnSlotItemDropped);
        Singleton.Instance.GameEvents.OnSlotSelected.RemoveListener(OnSlotSelected);
        Singleton.Instance.GameEvents.OnDragBegun.RemoveListener(i => previousIndex = i);
        Singleton.Instance.GameEvents.OnQuickSlotItemUpdated.RemoveListener(OnQuickSlotItemUpdated);
    }
    #endregion

    #region Get
    public Transform GetRightHand() => m_rightHand;
    #endregion

    

    #region Slot handle
    private void OnSlotSelected(int index, bool isCollecting) {
        bool hasPreviousItem =
            currentItem != null;

        ItemData currentItemData = Singleton.Instance.InventoryManager.GetItemFromSlot(index);
        currentItem = Singleton.Instance.GameManager.GetItemByID(currentItemData == null ? "" : currentItemData.itemBaseId);

        bool itemCollectedIsWeapon =
            currentItem != null &&
            currentItem.m_itemType == (ItemType.MeleeWeapon | ItemType.Firearm);

        if (isCollecting && (_currentHandItem != null || 
            (_currentHandItem == null && !itemCollectedIsWeapon)))
            Animator.OnCollect();

        if (equippedItem == currentItemData) return;
        equippedItem = currentItemData;

        Combat.SetCanSwitch(false);
        CameraMovement.PlayWeaponSwitchAnimation();

        Singleton.Instance.GameEvents.OnActualSlotItemSet?.Invoke(currentItem, hasPreviousItem);
    }

    public void OnDrawAnimationStarted() {
        DespawnItemOnHandRpc();

        if (currentItem != null && currentItem.m_itemType == (ItemType.MeleeWeapon | ItemType.Firearm))
            SpawnItemOnHandRpc(equippedItem);

        Combat.SetWeapon(_currentWeaponEquipped, currentItem);
    }
    
    [Rpc(SendTo.Server)]
    private void SpawnItemOnHandRpc(ItemData itemData) {
        Item_SO itemBase = Singleton.Instance.GameManager.GetItemByID(itemData.itemBaseId);
        Vector3 finalPos = m_rightHand.position + (m_rightHand.rotation * itemBase.m_itemPositionOffset);
        Quaternion rotOffset = Quaternion.Euler(itemBase.m_itemRotationOffset);
        Quaternion finalRot = m_rightHand.rotation * rotOffset;
        GameObject instantiableItem = Instantiate(itemBase.m_itemPrefab.gameObject, finalPos, finalRot);
        ulong targetClient = OwnerClientId;

        NetworkObject netObj = instantiableItem.GetComponent<NetworkObject>();
        netObj.SpawnWithOwnership(targetClient);

        if (instantiableItem.TryGetComponent(out Item_Interactor itemCollectable)) {
            //itemCollectable.SetItemData(itemData);
            itemCollectable.SetAsHandItem(targetClient);
        }

        SetHandItemClientRpc(netObj.NetworkObjectId, itemData, targetClient);
    }

    [ClientRpc]
    private void SetHandItemClientRpc(ulong netObjId, ItemData itemData, ulong playerId) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netObjId, out var netObj))
            return;

        _currentHandItem = netObj.GetComponent<Item_Interactor>();
        if (_currentHandItem.TryGetComponent(out IWeapon weapon))
            _currentWeaponEquipped = weapon;

        //_currentHandItem.SetItemData(itemData);
        _currentHandItem.SetAsHandItem(playerId);
    }

    [Rpc(SendTo.Server)]
    private void DespawnItemOnHandRpc() {
        if (!IsServer) return;
        if (_currentHandItem == null) return;

        _currentHandItem.NetworkObject.Despawn();
        _currentHandItem = null;
    }

    private void OnQuickSlotItemUpdated(int previousIndex, int nextIndex) {
        if (previousIndex == Interaction.ActualSlotSelected) OnSlotSelected(previousIndex, false);
        if (nextIndex == Interaction.ActualSlotSelected) OnSlotSelected(nextIndex, false);
    }

    private void OnSlotItemDropped(int index) {
        string itemId = Singleton.Instance.InventoryManager.GetItemFromSlot(index).itemBaseId;
        ItemData data = Singleton.Instance.SaveManager.GetItemFromInventory(itemId);

        SpawnItemServerRpc(itemId, Interaction.GetTargetAim());
        DespawnItemOnHandRpc();

        currentItem = null;
        equippedItem = null;

        Animator.OnDrop();

        Singleton.Instance.GameEvents.OnInventoryItemRemoved?.Invoke(data, index);
    }
    #endregion

    #region Network visuals
    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemOnHandServerRpc(string id) => SpawnItemOnHandClientRpc(id);

    [ClientRpc]
    private void SpawnItemOnHandClientRpc(string id) {
        if (IsOwner) return;
        itemOnTPHand = Instantiate(Singleton.Instance.GameManager.GetItemByID(id).m_itemPrefab, m_thirdPersonRightHand);
        itemOnTPHand.SetThirdPersonViewOnly();        
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemServerRpc(string id, Vector3 pos) {
        if (!IsServer) return;
        Interactor item = Instantiate(Singleton.Instance.GameManager.GetItemByID(id).m_itemPrefab, pos, Quaternion.LookRotation(pos));
        item.GetComponent<NetworkObject>().Spawn(true);
    }
    #endregion
}
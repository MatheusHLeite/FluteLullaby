using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_InventorySystem : NetworkBehaviour {
    private Player_InteractionSystem Interaction;
    private Player_CombatSystem Combat;

    [Header("Hands")]
    [SerializeField] private Transform m_rightHand;
    [SerializeField] private Transform m_thirdPersonRightHand;

    private Interactor itemOnTPHand;
    private GameObject itemOnTPHandRef;
    private GameObject itemOnHand;

    #region Initialization
    private void Awake() {
        Interaction = GetComponent<Player_InteractionSystem>();
        Combat = GetComponent<Player_CombatSystem>();
    }
    #endregion

    #region Network Initialization
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSlotItemDropped.AddListener(OnSlotItemDropped);
            Singleton.Instance.GameEvents.OnSlotItemCollected.AddListener(OnItemCollected);
            Singleton.Instance.GameEvents.OnSlotSelected.AddListener(OnSlotSelected);
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSlotItemDropped.RemoveListener(OnSlotItemDropped);
            Singleton.Instance.GameEvents.OnSlotItemCollected.RemoveListener(OnItemCollected);
            Singleton.Instance.GameEvents.OnSlotSelected.RemoveListener(OnSlotSelected);            
        }
    }
    #endregion

    #region Get
    public Transform GetRightHand() => m_rightHand;
    #endregion

    #region Slot handle
    private void OnSlotSelected(int index) {
        if (itemOnHand != null) {
            Destroy(itemOnHand);
            DespawnItemOnHandServerRpc();
        }

        Item_SO item = Singleton.Instance.InventoryManager.GetItemFromSlot(index);

        if (item != null) {
            itemOnHand = Instantiate(item.m_onHandItemPrefab, m_rightHand);
            SetupItemOHand(item);
            SpawnItemOnHandServerRpc(item.id);
        }

        Singleton.Instance.GameEvents.OnActualSlotItem?.Invoke(item);
    }

    private void SetupItemOHand(Item_SO item) {
        if (itemOnHand.transform.TryGetComponent(out Weapon_Firearm firearm)) 
            firearm.SetupWeapon(item, Combat, Singleton.Instance.SaveManager.GetWeaponDataByID(item.id));
        if (itemOnHand.transform.TryGetComponent(out Weapon_Melee melee))
            melee.SetupWeapon(item, Combat);
    }

    private void OnItemCollected(Item_SO item, int index) {
        Singleton.Instance.GameEvents.OnInventoryItemAdded?.Invoke(item, index);
    }

    private void OnSlotItemDropped(int index) {
        Singleton.Instance.GameEvents.OnInventoryItemRemoved?.Invoke(index);

        string id = Singleton.Instance.InventoryManager.GetItemFromSlot(index).id;
        SpawnItemServerRpc(id, Interaction.GetTargetAim());

        if (m_rightHand.childCount > 0)
            Destroy(m_rightHand.GetChild(0).gameObject);
    }
    #endregion

    #region Network visuals
    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemOnHandServerRpc(string id) => SpawnItemOnHandClientRpc(id);

    [ClientRpc]
    private void SpawnItemOnHandClientRpc(string id) {
        if (IsOwner) return;
        itemOnTPHand = Instantiate(Singleton.Instance.GameManager.GetItemByID(id).m_collectibleItemPrefab, m_thirdPersonRightHand);
        itemOnTPHandRef = itemOnTPHand.gameObject;
        itemOnTPHand.SetThirdPersonViewOnly();        
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnItemOnHandServerRpc() => DespawnItemOnHandClientRpc();

    [ClientRpc]
    private void DespawnItemOnHandClientRpc() {
        if (IsOwner) return;
        Destroy(itemOnTPHandRef);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnItemServerRpc(string id, Vector3 pos) {
        if (!IsServer) return;
        Interactor item = Instantiate(Singleton.Instance.GameManager.GetItemByID(id).m_collectibleItemPrefab, pos, Quaternion.LookRotation(pos));
        item.GetComponent<NetworkObject>().Spawn(true);
    }
    #endregion
}
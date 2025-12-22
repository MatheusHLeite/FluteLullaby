using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class Player_InventorySystem : NetworkBehaviour {
    private Player_InteractionSystem Interaction;
    private Player_CombatSystem Combat;
    private Player_CameraMovementSystem CameraMovement;

    [Header("Hands")]
    [SerializeField] private Transform m_rightHand;
    [SerializeField] private Transform m_thirdPersonRightHand;

    #region Private upgradable/modifiable variables
    private float changeWeaponSpeed = 0.4f;
    #endregion

    #region Private
    private Transform weaponHolder;
    private Interactor itemOnTPHand;
    private GameObject itemOnTPHandRef;
    private GameObject itemOnHand;

    private ItemData equippedItem;

    private int previousIndex;
    #endregion

    #region Initialization
    private void Awake() {
        Interaction = GetComponent<Player_InteractionSystem>();
        Combat = GetComponent<Player_CombatSystem>();
        CameraMovement = GetComponent<Player_CameraMovementSystem>();

        weaponHolder = m_rightHand.parent;
    }
    #endregion

    #region Network Initialization
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnItemDropped.AddListener(OnSlotItemDropped);            
            Singleton.Instance.GameEvents.OnSlotSelected.AddListener(OnSlotSelected);
            Singleton.Instance.GameEvents.OnDragBegun.AddListener(i => previousIndex = i);
            Singleton.Instance.GameEvents.OnQuickSlotItemUpdated.AddListener(OnQuickSlotItemUpdated);
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnItemDropped.RemoveListener(OnSlotItemDropped);
            Singleton.Instance.GameEvents.OnSlotSelected.RemoveListener(OnSlotSelected);
            Singleton.Instance.GameEvents.OnDragBegun.RemoveListener(i => previousIndex = i);
            Singleton.Instance.GameEvents.OnQuickSlotItemUpdated.RemoveListener(OnQuickSlotItemUpdated);
        }
    }
    #endregion

    #region Get
    public Transform GetRightHand() => m_rightHand;
    #endregion

    #region Slot handle
    private void OnSlotSelected(int index) {
        ItemData itemData = Singleton.Instance.InventoryManager.GetItemFromSlot(index);
        Item_SO item = Singleton.Instance.GameManager.GetItemByID(itemData == null ? "" : itemData.id);

        if (equippedItem == itemData) return;
        equippedItem = itemData;

        Combat.SetCanSwitch(false);

        CameraMovement.PlayWeaponSwitchAnimation();

        weaponHolder.DOLocalRotate(new Vector3(45, 0, 0), changeWeaponSpeed).SetEase(Ease.InBack).OnComplete(() => {
            if (itemOnHand != null) {
                Destroy(itemOnHand);
                DespawnItemOnHandServerRpc();
            }

            if (item != null && item.m_itemType != ItemType.Ammo) {
                itemOnHand = Instantiate(item.m_onHandItemPrefab, m_rightHand);
                SetupItemOHand(item);
                SpawnItemOnHandServerRpc(item.id);
            }

            Singleton.Instance.GameEvents.OnActualSlotItemSet?.Invoke(item);

            weaponHolder.DOLocalRotate(Vector3.zero, changeWeaponSpeed / 1.75f).SetDelay(0.15f).SetEase(Ease.OutBack).OnComplete(() => {
                Combat.SetCanSwitch(true);
            });
        });
    }

    private void OnQuickSlotItemUpdated(int previousIndex, int nextIndex) {
        if (previousIndex == Interaction.ActualSlotSelected) OnSlotSelected(previousIndex);
        if (nextIndex == Interaction.ActualSlotSelected) OnSlotSelected(nextIndex);
    }

    private void SetupItemOHand(Item_SO item) {
        if (itemOnHand.transform.TryGetComponent(out Weapon_Firearm firearm)) 
            firearm.SetupWeapon(item, Combat);
        if (itemOnHand.transform.TryGetComponent(out Weapon_Melee melee))
            melee.SetupWeapon(item, Combat);
    }

    private void OnSlotItemDropped(int index) {
        string itemId = Singleton.Instance.InventoryManager.GetItemFromSlot(index).id;
        ItemData data = Singleton.Instance.SaveManager.GetItemFromInventory(itemId);

        SpawnItemServerRpc(itemId, Interaction.GetTargetAim());

        if (index == Interaction.ActualSlotSelected || index == previousIndex && m_rightHand.childCount > 0)        
            Destroy(m_rightHand.GetChild(0).gameObject);

        Singleton.Instance.GameEvents.OnInventoryItemRemoved?.Invoke(data, index);
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
using DelightStudio.Data;
using DelightStudio.Item;
using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Item_Interactor : Interactor {
    [BoxGroup("Item setup"), SerializeField] private Item_SO m_item;
    [PropertySpace(20)]
    [BoxGroup("Item setup"), SerializeField] private bool m_randomizeAmount;
    [PropertySpace(5)]
    [BoxGroup("Item setup"), SerializeField, MinValue(1), MaxValue(nameof(m_maxAmount)), HideIf(nameof(m_randomizeAmount))] private int m_amount = 1;
    [BoxGroup("Item setup"), SerializeField, ShowIf(nameof(m_randomizeAmount))] private GameObject[] m_itemVisuals;
    [BoxGroup("Item setup"), SerializeField] private GameObject m_itemBox;

    [Header("Setup")]
    [SerializeField] private MonoBehaviour[] scriptsToDisableOnHand;
    [SerializeField] private MonoBehaviour[] scriptsToEnableOnHand;

    private int m_slotIndex;
    private int m_index;

    private bool displayItem;
    private int m_maxAmount => m_randomizeAmount ? m_itemVisuals.Length : 50;

    private NetworkObject _object;

    private Rigidbody rb;
    private Collider[] colliders;
    private NetworkTransform networkTransform;
    private NetworkRigidbody networkRigidbody;    

    private Transform followTarget;
    private bool lockRandomize;

    private Item_Highlight highlightReference;

    protected void Awake() {
        _object = GetComponent<NetworkObject>();
        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();
        networkTransform = GetComponent<NetworkTransform>();
        networkRigidbody = GetComponent<NetworkRigidbody>();

        if (lockRandomize) 
            return;

        var itemH = Singleton.Instance.GameManager.GetItemHightLight();

        highlightReference = Instantiate(itemH, transform);
        highlightReference.Setup(m_item.m_itemRarity);
        highlightReference.SetOnHandItem(displayItem);

        if (m_randomizeAmount) 
            SetAmount(Random.Range(1, m_itemVisuals.Length));
    }

    #region Set
    public void SetItemAmount(int amount) {
        lockRandomize = true;

        SetAmount(amount);
    }
    
    private void SetAmount(int amount) {
        amount = Mathf.Max(amount, 0);
        int maxVisuals = m_itemVisuals.Length;
        bool tooManyItems = amount > maxVisuals;

        if (m_itemBox != null)
            m_itemBox.SetActive(tooManyItems);
        for (int i = 0; i < maxVisuals; i++)
            m_itemVisuals[i].SetActive(i < amount && !tooManyItems);

        m_amount = amount;

        if (amount <= 0)
            RequestDespawnServerRpc();
    }

    public void SetAs3DView() {
        lockRandomize = true;

        if (highlightReference != null)
            Destroy(highlightReference.gameObject);

        for (int i = 0; i < m_itemVisuals.Length; i++)
            m_itemVisuals[i].SetActive(true);
    }

    public void SetAsHandItem(ulong playerId) {
        rb.isKinematic = true;
        displayItem = true;

        networkRigidbody.enabled = false;
        networkTransform.enabled = false;
        RemoveColliders();

        highlightReference.SetOnHandItem(true);

        foreach (var s in scriptsToDisableOnHand)
            s.enabled = false;

        foreach (var s in scriptsToEnableOnHand)
            s.enabled = true;

        if (!Player_InteractionSystem.Players.TryGetValue(playerId, out var player))
            return;

        bool isLocalPlayer = playerId == NetworkManager.Singleton.LocalClientId;

        Weapon currentWeapon = m_item as Weapon;
        if (currentWeapon != null)
            followTarget = currentWeapon.m_handSide == HandSide.Right ? player.GetRightPlayerHand : player.GetLeftPlayerHand;
        else
            followTarget = player.GetRightPlayerHand;

        if (isLocalPlayer)
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("FirstPersonElement"));
    }

    void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
    #endregion

    public override void Interact(Player_InteractionSystem interactor) {
        if (displayItem) return;

        if (!Singleton.Instance.InventoryManager.CanPickUpItem(m_item)) {
            print("<color=yellow>Cannot pick up item: {reason}</color>");
            return; 
        }
        if (InventoryManager.IsInventoryFull()) {
            Debug.Log("<color=red>Inventory is full</color>");
            return; 
        }

        bool isQuickSlotItem = m_item.m_itemType == ItemType.MeleeWeapon || m_item.m_itemType == ItemType.Firearm;

        m_index = isQuickSlotItem ? 0 : UI_InventoryManager._quickSlots.Count;
        m_slotIndex = Singleton.Instance.InventoryManager.GetEmptySlotIndex(m_index);

        Singleton.Instance.GameEvents.OnItemCollected?.Invoke(m_item, m_slotIndex, m_amount, false);

        base.Interact(interactor);

        RequestDespawnServerRpc();
    }

    public override void OnHoverOverItem(bool isOnTarget) {
        if (displayItem) return;
        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_item.m_itemName : "");

        base.OnHoverOverItem(isOnTarget);
    }

    private void RemoveColliders(bool shouldDestroy = true) {
        foreach (var c in colliders) {
            if (shouldDestroy) Destroy(c);
            else c.isTrigger = true;
        }
    }

    [Rpc(SendTo.Server)]
    public void RequestDespawnServerRpc(RpcParams rpcParams = default) {
        if (!IsSpawned) return;
        NetworkObject.Despawn(true);
    }

    #region Update
    private void HandleItemTrack() {
        if (!followTarget) 
            return;

        Vector3 itemOffset = m_item.m_itemPositionOffset;
        Vector3 finalPos = followTarget.position + (followTarget.rotation * itemOffset);

        Quaternion rotOffset = Quaternion.Euler(m_item.m_itemRotationOffset);
        Quaternion finalRot = followTarget.rotation * rotOffset;

        transform.SetPositionAndRotation(finalPos, finalRot);
    }

    private void LateUpdate() {
        HandleItemTrack();
    }
    #endregion
}

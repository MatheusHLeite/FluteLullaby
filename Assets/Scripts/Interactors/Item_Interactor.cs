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

        var itemH = Singleton.Instance.GameManager.GetItemHightLight();

        highlightReference = Instantiate(itemH, transform);
        highlightReference.Setup(m_item.m_itemRarity);
        highlightReference.SetOnHandItem(displayItem);

        if (lockRandomize) 
            return;

        if (m_randomizeAmount) {
            m_amount = Random.Range(1, m_itemVisuals.Length);

            for (int i = 0; i < m_itemVisuals.Length; i++) 
                m_itemVisuals[i].SetActive(false);
            for (int i = 0; i < m_amount; i++)
                m_itemVisuals[i].SetActive(true);
        }
    }

    public void SetAs3DView() {
        lockRandomize = true;

        for (int i = 0; i < m_itemVisuals.Length; i++)
            m_itemVisuals[i].SetActive(true);
    }

    public override void OnHoverOverItem(bool isOnTarget) {
        if (displayItem) return;
        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_item.m_itemName : "");

        base.OnHoverOverItem(isOnTarget);
    }

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

        bool isQuickSlotItem = m_item.m_itemType != (ItemType.MeleeWeapon | ItemType.Firearm);

        m_index = isQuickSlotItem ? UI_InventoryManager._quickSlots.Count : 0;
        m_slotIndex = Singleton.Instance.InventoryManager.GetEmptySlotIndex(m_index);

        Singleton.Instance.GameEvents.OnItemCollected?.Invoke(m_item, m_slotIndex, m_amount, false);

        base.Interact(interactor);

        RequestDespawnServerRpc();
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

        bool isLocalPlayer =
            playerId == NetworkManager.Singleton.LocalClientId;

        followTarget = player.GetRightPlayerHand;

        if (isLocalPlayer)
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("FirstPersonElement"));
    }

    void SetLayerRecursively(GameObject obj, int layer) {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
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

    private void LateUpdate() {
        if (!followTarget) return;

        Vector3 itemOffset = m_item.m_itemPositionOffset;
        Vector3 finalPos = followTarget.position + (followTarget.rotation * itemOffset);

        Quaternion rotOffset = Quaternion.Euler(m_item.m_itemRotationOffset);
        Quaternion finalRot = followTarget.rotation * rotOffset;

        transform.SetPositionAndRotation(finalPos, finalRot);
    }
}

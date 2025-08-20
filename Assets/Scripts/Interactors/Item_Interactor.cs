using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public class Item_Interactor : Interactor {
    [BoxGroup("Item setup"), SerializeField] private Item_SO m_item;
    [PropertySpace(20)]
    [BoxGroup("Item setup"), SerializeField] private bool m_randomizeAmount;
    [PropertySpace(5)]
    [BoxGroup("Item setup"), SerializeField, MinValue(1), MaxValue(nameof(m_maxAmount)), HideIf(nameof(m_randomizeAmount))] private int m_amount = 1;
    [BoxGroup("Item setup"), SerializeField, ShowIf(nameof(m_randomizeAmount))] private GameObject[] m_itemVisuals;

    private int m_slotIndex;
    private int m_index;
    private int m_maxAmount => m_randomizeAmount ? m_itemVisuals.Length : 50;

    private NetworkObject _object;

    protected override void Awake() {
        base.Awake();

        _object = GetComponent<NetworkObject>();

        if (m_randomizeAmount) {
            m_amount = Random.Range(1, m_itemVisuals.Length);

            for (int i = 0; i < m_itemVisuals.Length; i++) m_itemVisuals[i].SetActive(false);
            for (int i = 0; i < m_amount; i++) m_itemVisuals[i].SetActive(true);
        }
    }

    public override void OnHoverOverItem(bool isOnTarget) {
        base.OnHoverOverItem(isOnTarget);

        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_item.m_itemName : "");
    }

    public override void Interact(Player_InteractionSystem interactor) {
        if (!Singleton.Instance.InventoryManager.CanPickUpItem(m_item)) {
            print("<color=yellow>Cannot pick up item: {reason}</color>");
            return; 
        }
        if (InventoryManager.IsInventoryFull()) {
            Debug.Log("<color=red>Inventory is full</color>");
            return; 
        }

        m_index = m_item.m_itemType != ItemType.MeleeWeapon && m_item.m_itemType != ItemType.Firearm ? 
            UI_InventoryManager._quickSlots.Count : 0;
        m_slotIndex = Singleton.Instance.InventoryManager.GetEmptySlotIndex(m_index);

        Singleton.Instance.GameEvents.OnItemCollected?.Invoke(m_item, m_slotIndex, m_amount, false);

        if (IsOwner || IsClient)
            DespawnObjectServerRpc();

        base.Interact(interactor);
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void DespawnObjectServerRpc()
    {
        if (!IsServer) return;

        if (_object != null && _object.IsSpawned)
            _object.Despawn(true);
    }
}

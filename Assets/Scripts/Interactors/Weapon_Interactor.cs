using Unity.Netcode;
using UnityEngine;

public class Weapon_Interactor : NetworkBehaviour, IInteractable {
    [SerializeField] private Item_SO m_weapon;

    [Header("Visual")]
    [SerializeField] private Renderer m_itemVisual;
    [SerializeField] private Material m_outlineMaterial;

    [Header("Setup")]
    [SerializeField] private float m_outlineWidth = 1.075f;

    private MaterialPropertyBlock propBlock;
    private Material outlineInstance;

    private NetworkObject _object;

    private void Awake() {
        _object = GetComponent<NetworkObject>();

        SetMaterials();
    }

    public override void OnDestroy() {
        if (outlineInstance) Destroy(outlineInstance);
    }

    private void SetMaterials() {
        propBlock = new MaterialPropertyBlock();
        Material[] currentMaterials = m_itemVisual.materials;
        if (!System.Array.Exists(currentMaterials, m => m.name.Contains(m_outlineMaterial.name))) {
            Material[] newMats = new Material[currentMaterials.Length + 1];
            for (int i = 0; i < currentMaterials.Length; i++)
                newMats[i] = currentMaterials[i];

            if (!outlineInstance)
                outlineInstance = new Material(m_outlineMaterial);
            newMats[^1] = outlineInstance;

            m_itemVisual.materials = newMats;
        }

        m_itemVisual.GetPropertyBlock(propBlock);
        propBlock.SetFloat("_OutlineScale", 0);
        m_itemVisual.SetPropertyBlock(propBlock);
    }

    public void OnHoverOverItem(bool isOnTarget) {
        propBlock.SetFloat("_OutlineScale", isOnTarget ? m_outlineWidth : 0);
        m_itemVisual.SetPropertyBlock(propBlock);

        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_weapon.m_itemName : "");
    }

    public void Interact(Player_InteractionSystem interactor) {
        if (interactor.GetPlayerInventory().Contains(m_weapon)) return;

        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke("");
        Singleton.Instance.GameEvents.OnSlotItemCollected?.Invoke(m_weapon, interactor.ActualSlotSelected);
        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(interactor.ActualSlotSelected);

        if (IsOwner || IsClient) 
            DespawnObjectServerRpc();                
    }

    [ServerRpc(RequireOwnership = false)]
    private void DespawnObjectServerRpc() {
        if (!IsServer) return;

        if (_object != null && _object.IsSpawned)
            _object.Despawn(true);       
    }
}

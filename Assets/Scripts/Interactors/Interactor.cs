using Unity.Netcode;
using UnityEngine;

public class Interactor : NetworkBehaviour, IInteractable {
    [Header("Visual")]    
    [SerializeField] private Material m_outlineMaterial;
    [Space(10)]
    [SerializeField] private GameObject m_thirdPersonVisual;
    [SerializeField] private GameObject m_onGroundVisual;

    private float m_outlineWidth = 1.075f;

    private Renderer[] m_itemVisual;

    private Collider _collider;
    private Rigidbody _rigidbody;
    private ClientNetworkTransform _clientNetworkTransform;

    private MaterialPropertyBlock propBlock;
    private Material outlineInstance;

    private NetworkObject _object;

    private void Awake() {
        _object = GetComponent<NetworkObject>();
        _collider = GetComponent<Collider>();
        _clientNetworkTransform = GetComponent<ClientNetworkTransform>();
        _rigidbody = GetComponent<Rigidbody>();

        SetMaterials();
    }

    public override void OnDestroy() {
        if (outlineInstance) Destroy(outlineInstance);
    }

    private void SetMaterials() {
        m_itemVisual = GetComponentsInChildren<Renderer>();

        m_outlineWidth = 1.05f;

        propBlock = new MaterialPropertyBlock();
        for (int v = 0; v < m_itemVisual.Length; v++) {
            Material[] currentMaterials = m_itemVisual[v].materials;
            if (!System.Array.Exists(currentMaterials, m => m.name.Contains(m_outlineMaterial.name))) {
                Material[] newMats = new Material[currentMaterials.Length + 1];
                for (int i = 0; i < currentMaterials.Length; i++)
                    newMats[i] = currentMaterials[i];

                if (!outlineInstance)
                    outlineInstance = new Material(m_outlineMaterial);
                newMats[^1] = outlineInstance;

                m_itemVisual[v].materials = newMats;
            }

            m_itemVisual[v].GetPropertyBlock(propBlock);
            propBlock.SetFloat("_OutlineScale", 0);
            m_itemVisual[v].SetPropertyBlock(propBlock);
        }        
    }

    public void SetThirdPersonViewOnly() {
        Destroy(_collider);
        Destroy(_rigidbody);
        Destroy(_clientNetworkTransform);
        Destroy(this);

        m_thirdPersonVisual.SetActive(true);
        m_onGroundVisual.SetActive(false);
    }

    public virtual void OnHoverOverItem(bool isOnTarget) {
        propBlock.SetFloat("_OutlineScale", isOnTarget ? m_outlineWidth : 0);
        for (int i = 0; i < m_itemVisual.Length; i++)        
            m_itemVisual[i].SetPropertyBlock(propBlock);                
    }

    public virtual void Interact(Player_InteractionSystem interactor) {
        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke("");
        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(interactor.ActualSlotSelected);

        if (IsOwner || IsClient)
            DespawnObjectServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void DespawnObjectServerRpc() {
        if (!IsServer) return;

        if (_object != null && _object.IsSpawned)
            _object.Despawn(true);
    }
}

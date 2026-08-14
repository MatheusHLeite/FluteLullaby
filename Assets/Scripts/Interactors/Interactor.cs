using Sirenix.OdinInspector;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Interactor : NetworkBehaviour, IInteractable {
    [FoldoutGroup("Visual")][SerializeField] private string m_screenShowcaseName;
    [FoldoutGroup("Visual")] [SerializeField] private Material m_outlineMaterial;

    [FoldoutGroup("3D Inventory showcase")] [SerializeField] private MeshRenderer[] allRenderers;
    [FoldoutGroup("3D Inventory showcase")] [SerializeField] private Material unlitMaterial;

    private float m_outlineWidth = 1.075f;

    private Renderer[] m_itemVisual;

    private Collider _collider;
    private Rigidbody _rigidbody;
    private NetworkTransform _networkTransform;
    private NetworkRigidbody _networkRigidbody;

    private MaterialPropertyBlock propBlock;
    private Material outlineInstance;

    public override void OnNetworkSpawn() {
        _collider = GetComponent<Collider>();
        _networkTransform = GetComponent<NetworkTransform>();
        _networkRigidbody = GetComponent<NetworkRigidbody>();
        _rigidbody = GetComponent<Rigidbody>();

        SetMaterials();
    }

    public override void OnNetworkDespawn() {
        if (outlineInstance) 
            Destroy(outlineInstance);
    }

    private void SetMaterials() {
        m_itemVisual = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();

        m_outlineWidth = 1.05f;

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
        Destroy(_networkTransform);
        Destroy(_networkRigidbody);
        Destroy(this);
    }

    public void SetAsShowcaseItem() {
        foreach (var renderer in allRenderers) 
            renderer.material = unlitMaterial;

        SetThirdPersonViewOnly();
    }

    public virtual void OnHoverOverItem(bool isOnTarget) {
        if (!string.IsNullOrEmpty(m_screenShowcaseName))Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_screenShowcaseName : "");

        propBlock.SetFloat("_OutlineScale", isOnTarget ? m_outlineWidth : 0);
        for (int i = 0; i < m_itemVisual.Length; i++)        
            m_itemVisual[i].SetPropertyBlock(propBlock);                
    }

    public virtual void Interact(Player_InteractionSystem interactor) {
        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke("");
        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(interactor.ActualSlotSelected, true);
    }
}

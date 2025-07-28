using Unity.Netcode;
using UnityEngine;

public class NetworkWeaponSpawner : NetworkBehaviour {
    [Header("Settings")]
    [SerializeField] private bool m_instantiateOnStart;
    [SerializeField] private CollectableItems[] m_items;

    public override void OnNetworkSpawn() {
        if (!m_instantiateOnStart) return;

        SpawnItem();
    }

    public void SpawnItem() {
        if (!IsServer) return;

        CollectableItems selectedItem = m_items[Random.Range(0, m_items.Length)];
        GameObject item = Instantiate(selectedItem.m_item.m_collectibleItemPrefab, 
            selectedItem.m_useActualPositionAndRotation ? transform.position : selectedItem.m_position, 
            selectedItem.m_useActualPositionAndRotation ? transform.rotation : selectedItem.m_rotation);
        item.GetComponent<NetworkObject>().Spawn();
    }
}
using Unity.Netcode;
using UnityEngine;

namespace DelightStudio.Systems { 
    public class RandomItemSpawner : NetworkBehaviour {
        [Header("Setup")]
        [SerializeField] private ItemSpawner[] items;

        public override void OnNetworkSpawn() {
            if (!IsServer) return;

            foreach (var item in items) {
                Vector3 pos = item.spawnPosition.position;
                Quaternion rot = Quaternion.Euler(item.item.m_itemSpawnRotation);

                Interactor newItem = Instantiate(item.item.m_itemPrefab, pos, rot);
                if (newItem.gameObject.TryGetComponent(out NetworkObject nwgo))
                    nwgo.Spawn();
            }
        }
    }

    [System.Serializable]
    public struct ItemSpawner {
        public Item_SO item;
        public Transform spawnPosition;
    }
}
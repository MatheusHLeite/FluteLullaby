using UnityEngine;

namespace DelightStudio.Item {
    public class Item_Highlight : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private ParticleSystem[] m_particles;
        [SerializeField] private GameObject m_root;

        public void Setup(ItemRarity rarity) {
            Color color = Singleton.Instance.GameManager.GetRarityColor(rarity);

            foreach (var particle in m_particles) {
                var mainModule = particle.main;
                mainModule.startColor = color;
            }
        }

        public void SetOnHandItem(bool isOnHand) {
            m_root.SetActive(!isOnHand);
        }

        private void LateUpdate() {
            if (!m_root.activeSelf) return;
            m_root.transform.rotation = Quaternion.identity;
        }
    }
}
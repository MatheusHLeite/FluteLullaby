using UnityEngine;

namespace DelightStudio.UI {
    public class UI_ItemShowcase : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Transform m_itemShowcase;
        [SerializeField] private float m_rotationSpeed = 45f;

        private bool isVisualizing;

        private void Awake() {
            Singleton.Instance.GameEvents.OnItemShowcaseSet.AddListener(SetItemToVisualize);
            Singleton.Instance.GameEvents.OnItemShowcaseUnset.AddListener(UnsetItemToVisualize);
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnItemShowcaseSet.RemoveListener(SetItemToVisualize);
            Singleton.Instance.GameEvents.OnItemShowcaseUnset.RemoveListener(UnsetItemToVisualize);
        }

        private void SetItemToVisualize(Item_SO item) {
            isVisualizing = true;

            if (m_itemShowcase.transform.childCount > 0)
                Destroy(m_itemShowcase.transform.GetChild(0).gameObject);

            Interactor showcaseItem = Instantiate(item.m_itemPrefab, m_itemShowcase);
            showcaseItem.transform.localPosition = item.m_showcaseItemPosition;
            showcaseItem.transform.localScale = item.m_showcaseItemScale;
            showcaseItem.SetAsShowcaseItem();

            Item_Interactor itemInteractor = showcaseItem as Item_Interactor;
            if (itemInteractor != null)
                itemInteractor.SetAs3DView();

            SetLayerRecursively(showcaseItem.gameObject, LayerMask.NameToLayer("InventoryShowcase"));
        }

        private void UnsetItemToVisualize() {
            isVisualizing = false;

            if (m_itemShowcase.transform.childCount > 0)
                Destroy(m_itemShowcase.transform.GetChild(0).gameObject);
        }

        void SetLayerRecursively(GameObject obj, int layer) {
            obj.layer = layer;

            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        private void Update() {
            if (!isVisualizing) return;

            m_itemShowcase.Rotate(Vector3.up * m_rotationSpeed * Time.deltaTime);
        }
    }
}
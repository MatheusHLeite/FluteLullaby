using UnityEngine;

public class TooltipSystem : MonoBehaviour {
    private static TooltipSystem m_current;

    [SerializeField] private Tooltip m_inventoryTooltip;

    private void Awake() {
        m_current = this;
    }

    public static void ShowInventoryTooltip(Item_SO item, ItemData itemData, int quantity) {
        m_current.m_inventoryTooltip.SetInventoryTooltip(item, itemData, quantity);
        m_current.m_inventoryTooltip.gameObject.SetActive(true);
    }
}

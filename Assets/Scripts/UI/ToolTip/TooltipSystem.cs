using UnityEngine;

public class TooltipSystem : MonoBehaviour {
    private static TooltipSystem m_current;

    [SerializeField] private Tooltip m_inventoryTooltip;

    private void Awake() {
        m_current = this;
    }

    public static Item_SO CurrentTooltipItem;

    public static void ShowInventoryTooltip(Item_SO item) {
        m_current.m_inventoryTooltip.SetInventoryTooltip(item);
        CurrentTooltipItem = item;
    }

    public static void HideInventoryTooltip(bool immediate = false) {
        m_current.m_inventoryTooltip.OnHideTooltip(immediate);
    }
}

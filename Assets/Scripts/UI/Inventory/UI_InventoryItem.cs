using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InventoryItem : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;

    private Item_SO currentItem;

    public void SetItem(Item_SO newItem, int index) {
        ItemData itemData = Singleton.Instance.SaveManager.GetItemData(newItem.id);

        GetComponent<UI_DragDropHandler>().SetIndexAndQuantity(index, itemData.m_quantity);

        currentItem = newItem;

        iconImage.sprite = newItem.m_icon;
        iconImage.preserveAspect = true;

        quantityText.text = itemData.m_quantity > 1 ? itemData.m_quantity.ToString() : "";
    }

    public Item_SO GetCurrentItem() => currentItem;
}
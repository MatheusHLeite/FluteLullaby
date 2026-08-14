using DelightStudio.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_InventoryItem : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private GameObject blockItemOverlay;

    private UI_DragDropHandler dragDropHandler;

    private string uniqueId;

    private Item_SO currentItem;
    public Item_SO GetCurrentItem() => currentItem;

    private void UpdateItem(ItemData data) {
        if (data.itemBaseId == currentItem.id && data.uniqueId == uniqueId) {
            if (data.quantity <= 0) {
                Destroy(gameObject);
                return;
            }

            dragDropHandler.UpdateQuantity(data.quantity);
            quantityText.text = data.quantity > 1 ? data.quantity.ToString() : "";
        }
    }

    private void OnWeaponReload(bool isReloading, WeaponClass weapon) {
        Weapon thisWeapon = currentItem as Weapon;
        Ammo_SO thisAmmo = currentItem as Ammo_SO;

        if (currentItem.m_itemType != ItemType.Ammo)
            return;

        if (thisWeapon != null && 
            thisWeapon.m_weaponType == weapon)
            blockItemOverlay.SetActive(isReloading);
        else if (thisAmmo != null &&
            thisAmmo.m_weapon == weapon)
            blockItemOverlay.SetActive(isReloading);
    }

    public void SetItem(ItemData itemData, int index, UI_Slot slot) {
        Singleton.Instance.GameEvents.OnItemUpdated.AddListener(UpdateItem);
        Singleton.Instance.GameEvents.OnWeaponReload.AddListener(OnWeaponReload);

        Item_SO newItem = Singleton.Instance.GameManager.GetItemByID(itemData.itemBaseId);

        currentItem = newItem;
        uniqueId = itemData.uniqueId;

        dragDropHandler = GetComponent<UI_DragDropHandler>();
        dragDropHandler.SetIndexAndQuantity(index, itemData, slot);

        iconImage.sprite = newItem.m_icon;
        iconImage.preserveAspect = true;

        quantityText.text = itemData.quantity > 1 ? itemData.quantity.ToString() : "";
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnItemUpdated.RemoveListener(UpdateItem);
        Singleton.Instance.GameEvents.OnWeaponReload.RemoveListener(OnWeaponReload);
    }
}
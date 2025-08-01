using UnityEngine;

public class Weapon_Interactor : Interactor {
    [Header("Item")]
    [SerializeField] private Item_SO m_weapon;

    public override void OnHoverOverItem(bool isOnTarget) {
        base.OnHoverOverItem(isOnTarget);

        Singleton.Instance.GameEvents.OnHoverOverItem?.Invoke(isOnTarget ? m_weapon.m_itemName : "");
    }

    public override void Interact(Player_InteractionSystem interactor) {
        if (interactor.GetPlayerInventory().Contains(m_weapon)) return;
        
        Singleton.Instance.GameEvents.OnSlotItemCollected?.Invoke(m_weapon, interactor.ActualSlotSelected);
       
        base.Interact(interactor);
    }
}

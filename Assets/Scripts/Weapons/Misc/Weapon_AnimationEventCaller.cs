using UnityEngine;

public class Weapon_AnimationEventCaller : MonoBehaviour {
    private Collider m_hitbox;
    private Player_CombatSystem combatSystem;

    private void Awake() {
        combatSystem = transform.root.GetComponent<Player_CombatSystem>();
        Singleton.Instance.GameEvents.OnActualSlotItem.AddListener(OnWeaponChange);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnActualSlotItem.RemoveListener(OnWeaponChange);
    }

    private void OnWeaponChange(Item_SO item) {
        m_hitbox = GetComponentInChildren<Collider>();
    }

    #region Melee animation events
    private void OpenDamageCollider() {
        m_hitbox.enabled = true;
        m_hitbox.isTrigger = true;
    }

    private void CloseDamageCollider() {        
        m_hitbox.isTrigger = false;
        m_hitbox.enabled = false;
    }

    private void OnComboWindowOpened() => combatSystem.SetComboWindow(true);

    private void OnComboWindowClosed() => combatSystem.SetComboWindow(false);

    private void OnAttackAnimationStarted() {
        CloseDamageCollider();
        combatSystem.SetComboWindow(false);
        combatSystem.SetAttackWindow(false); 
    }

    private void OnAttackAnimationFinished() {
        combatSystem.ResetComboStep();
        combatSystem.SetAttackWindow(true); 
    }
    #endregion
}

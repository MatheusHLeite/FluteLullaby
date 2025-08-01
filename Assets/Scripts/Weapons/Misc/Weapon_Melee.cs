using UnityEngine;

public class Weapon_Melee : MonoBehaviour {
    private Player_AnimationSystem AnimationSystem;
    private Player_CombatSystem CombatSystem;

    private Animator m_animator;
    private BoxCollider m_hitbox;

    private float m_defaultDamage;
    private float m_damage;
    private int m_comboStep;
    private int m_maxComboStep = 3;

    private bool m_canAttack;
    private bool m_canCombo;

    private MeleeWeapon_SO weapon;

    public void SetupWeapon(Item_SO item, Player_CombatSystem combat) {
        weapon = item as MeleeWeapon_SO;     

        m_hitbox = GetComponent<BoxCollider>();
        m_animator = GetComponent<Animator>();

        CombatSystem = combat;
        AnimationSystem = combat.GetComponent<Player_AnimationSystem>();

        m_damage = weapon.m_damage;
        m_defaultDamage = weapon.m_damage;

        m_hitbox.center = new Vector3(0, weapon.m_hitboxSize.y / 2, 0);
        m_hitbox.size = weapon.m_hitboxSize;

        m_canAttack = true;

        CombatSystem.SetCanSwitch(true);
    }

    public void CallAttack() {
        if (m_canCombo || m_canAttack) {
            CombatSystem.SetCanSwitch(false);

            if (m_canCombo)
                IncreaseComboStep();

            m_animator.SetTrigger($"Attack_{m_comboStep + 1}");
            AnimationSystem.OnAttack(m_comboStep + 1);
        }
    }

    protected void IncreaseComboStep() {
        m_comboStep++;

        IncreaseDamage(12.5f); //Set in percentage

        if (m_comboStep > m_maxComboStep)
            ResetComboStep();
    }

    protected void ResetComboStep() {
        m_comboStep = 0;
        m_canCombo = false;

        CombatSystem.SetCanSwitch(true);

        m_damage = m_defaultDamage;
    }

    public void IncreaseDamage(float percentage) => m_damage += m_damage * (percentage / 100);

    #region Animation events
    private void OpenDamageCollider() {
        m_hitbox.enabled = true;
        m_hitbox.isTrigger = true;
    }

    private void CloseDamageCollider() {
        m_hitbox.isTrigger = false;
        m_hitbox.enabled = false;
    }

    private void OnComboWindowOpened() => m_canCombo = true;

    private void OnComboWindowClosed() => m_canCombo = false;

    private void OnAttackAnimationStarted() {
        CloseDamageCollider();

        m_canCombo = false;
        m_canAttack = false;
    }

    private void OnAttackAnimationFinished(){
        ResetComboStep();
        m_canAttack = true;
    }
    #endregion

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out Enemy enemy)) {
            enemy.TakeDamage(m_damage);
        }
    }
}
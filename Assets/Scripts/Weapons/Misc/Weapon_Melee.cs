using DelightStudio.AI;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class Weapon_Melee : MonoBehaviour, IWeapon {
    [Header("Weapon Setup")]
    [SerializeField] protected Transform attackPoint;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private Vector3 m_hitBoxSize;

    [Header("Combo")]
    [SerializeField] private int maxCombo = 3;
    [SerializeField] private float comboResetTime = 0.8f;
    [SerializeField] private float comboDamageIncrease = 0.25f;

    protected float _hitRadius;
    protected float _damage;
    protected float _impactForce;

    private float damageMultiplier;
    private float attackSpeedMultiplier;

    protected Player_CombatSystem CombatSystem;
    protected Player_AnimationSystem AnimationSystem;
    protected Player_AudioSystem AudioSystem;

    private CinemachineImpulseSource impulseSource;

    protected MeleeWeapon_SO weapon;

    private bool isAttacking;
    private bool comboWindowOpen;
    private bool comboQueued;

    private int currentCombo;
    private float comboResetTimer;

    private readonly Collider[] hitResults = new Collider[32];

    private const string AttackAnimTrigger = "Attack";
    private const string ComboIndexParameter = "ComboIndex";

    private const string AttackSpeedMultiplier = "AttackSpeed_Multiplier";

    #region Get
    public Item_SO GetItem() => weapon;
    public int GetCurrentCombo() => currentCombo;
    public float GetCurrentComboMultiplier() {
        return 1f + ((currentCombo - 1) * comboDamageIncrease);
    }
    public float GetCurrentComboDamage() {
        return _damage * GetCurrentComboMultiplier();
    }
    public WeaponClass GetWeaponClass() => weapon.m_weaponType;
    #endregion

    #region Setup
    public virtual void SetupWeapon(Item_SO item, Player_CombatSystem combat) {
        weapon = item as MeleeWeapon_SO;
        MeleeWeaponData data = Singleton.Instance.SaveManager.GetMeleeWeaponFromInventory(item.id).meleeData;

        CombatSystem = combat;
        AnimationSystem = combat.GetComponent<Player_AnimationSystem>();
        AudioSystem = combat.GetComponent<Player_AudioSystem>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        _damage = weapon.m_damage;
        _impactForce = weapon.m_impactForce;
        _hitRadius = weapon.m_hitRangeSize;

        OnWeaponUpgrade(data);
        ResetCombo();
    }

    public void OnWeaponUpgrade(MeleeWeaponData data) {       
        damageMultiplier = Mathf.Clamp(data.m_damageMultiplier, 1f, 2f);
        attackSpeedMultiplier = Mathf.Clamp(data.m_attackSpeedMultiplier, 1f, 2f);

        HandleWeaponMultipliers();
    }
    #endregion

    private void HandleWeaponMultipliers() {
        _damage = GetWeaponDamage();
        AnimationSystem.SetAttackSpeedMultiplier(AttackSpeedMultiplier, attackSpeedMultiplier);
    }

    private float GetWeaponDamage() {
        return _damage; //* damageMultiplier
    }

    #region Main calls
    public void FireButtonDown(Player_CombatSystem combat) {
        if (CombatSystem == null)
            CombatSystem = combat;

        if (isAttacking) {
            if (comboWindowOpen && currentCombo < maxCombo)
                comboQueued = true;
            return;
        }

        StartAttack();
    }

    public void FireButtonHold(Player_CombatSystem combat) { }

    public void FireButtonUp(Player_CombatSystem combat) { }

    public void Reload(Player_CombatSystem combat) { }
    #endregion

    #region Attack
    private void StartAttack() {
        if (currentCombo >= maxCombo || Time.time >= comboResetTimer)
            currentCombo = 0;

        currentCombo++;

        isAttacking = true;
        comboWindowOpen = false;
        comboQueued = false;

        CombatSystem.SetCanSwitch(false);
        AnimationSystem.OnSetCombo(ComboIndexParameter, currentCombo);
        AnimationSystem.OnMeleeAttack(AttackAnimTrigger);

        OnAttackStarted();
    }

    private void StartNextComboAttack() {
        if (currentCombo >= maxCombo) {
            EndAttack();
            return;
        }

        currentCombo++;

        isAttacking = true;
        comboWindowOpen = false;
        comboQueued = false;

        AnimationSystem.OnSetCombo(ComboIndexParameter, currentCombo);

        OnAttackStarted();
    }

    private void EndAttack() {
        isAttacking = false;
        comboWindowOpen = false;
        comboQueued = false;

        comboResetTimer = Time.time + comboResetTime;

        CombatSystem.SetCanSwitch(true);

        OnAttackEnded();
    }

    private void ResetCombo() {
        currentCombo = 0;
        comboWindowOpen = false;
        comboQueued = false;
        comboResetTimer = 0f;
    }
    #endregion

    #region Hit
    protected virtual void PerformAttackHit() {
        if (!CombatSystem.IsOwner || attackPoint == null)
            return;

        int hitCount = Physics.OverlapBoxNonAlloc(attackPoint.position, m_hitBoxSize / 2, hitResults, attackPoint.rotation, hitLayers, QueryTriggerInteraction.Ignore);
        float damage = GetCurrentComboDamage();

        List<Enemy_Manager> enemies = new List<Enemy_Manager>();

        for (int i = 0; i < hitCount; i++) {
            Collider collider = hitResults[i];

            if (collider == null)
                continue;

            Damagable_BodyPart damagable = collider.GetComponent<Damagable_BodyPart>();

            if (damagable == null)
                continue;

            Enemy_Manager enemy = damagable.GetComponentInParent<Enemy_Manager>();

            if (enemies.Contains(enemy))
                continue;

            enemies.Add(enemy);

            Player_CombatSystem hitCombat = damagable.GetComponentInParent<Player_CombatSystem>();

            if (hitCombat == CombatSystem)
                continue;

            Vector3 hitPoint = collider.ClosestPoint(attackPoint.position);
            Vector3 hitDirection = (hitPoint - transform.position).normalized;

            damagable.TakeDamage(damage, hitPoint, hitDirection, _impactForce);
            OnSuccessfulHit(damagable, hitPoint, hitDirection, damage);
        }
    }
    #endregion

    #region Animation Events
    public void OnAttackHit() {
        if (!isAttacking)
            return;

        PerformAttackHit();
    }

    public void OnComboWindowOpen() {
        if (!isAttacking)
            return;

        comboWindowOpen = true;
    }

    public void OnAttackEnd() {
        if (!isAttacking)
            return;

        if (comboQueued && currentCombo < maxCombo) {
            StartNextComboAttack();
            return;
        }

        EndAttack();
    }
    #endregion

    #region Virtual Calls
    protected virtual void OnAttackStarted() { }

    protected virtual void OnSuccessfulHit(Damagable_BodyPart damagable, Vector3 hitPoint, Vector3 direction, float damage) {
        Singleton.Instance.GlobalTimeManager.TriggerOnHitSlowMotion(0.3f);

        ParticleSystem ps = Singleton.Instance.VFXManager.GetRandomHitEffect();
        Instantiate(ps, hitPoint, Quaternion.identity);

        float impulseForce = Mathf.Clamp(_impactForce / 150f, 0.2f, 1.2f);
        //impulseSource.GenerateImpulseWithForce(impulseForce);
    }

    protected virtual void OnAttackEnded() { }
    #endregion

    #region Debug
    private void OnDrawGizmosSelected() {
        if (attackPoint == null)
            return;

        Gizmos.matrix = Matrix4x4.TRS(
        attackPoint.position,
        attackPoint.rotation,
        Vector3.one
    );

        Gizmos.DrawWireCube(
            Vector3.zero,
            m_hitBoxSize
        );

        Gizmos.matrix = Matrix4x4.identity;
    }
    #endregion

    #region Update
    private void Update() {
        if (isAttacking)
            return;

        if (currentCombo > 0 && Time.time >= comboResetTimer)
            ResetCombo();
    }
    #endregion
}
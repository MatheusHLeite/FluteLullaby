using Unity.Cinemachine;
using UnityEngine;

public abstract class Weapon_Firearm : MonoBehaviour {
    [Header("Weapon setup")]    
    public ParticleSystem muzzleFlash;
    public ParticleSystem smokeFX;
    public Animator armsAnimator;

    #region Protected variables
    protected float m_damage;
    protected float m_range;
    protected float m_impact;

    protected RaycastHit hit;
    protected Ray ray;

    protected Player_CameraMovementSystem CameraMovement;
    protected Player_AnimationSystem AnimationSystem;
    protected Player_AudioSystem AudioSystem;
    #endregion

    #region Private variables
    private int currentAmmo;
    private int m_maxAmmo;
    private int stockedAmmo;

    private float m_fireRateMultiplier;
    private float m_reloadSpeedMultiplier;
    private float m_weaponRecoilForce;

    private Animator animator;
    private Player_CombatSystem CombatSystem;    
    private CinemachineImpulseSource impulseSource;

    private bool isReloading;
    private bool isShooting;

    private LongRangeWeapon_SO weapon;
    #endregion

    #region Get
    public Item_SO GetItem() => weapon;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetStockedAmmo() => stockedAmmo;
    #endregion

    #region Public setup
    public virtual void SetupWeapon(Item_SO item, Player_CombatSystem combat, WeaponData data) {
        weapon = item as LongRangeWeapon_SO;

        CombatSystem = combat;
        CameraMovement = combat.GetComponent<Player_CameraMovementSystem>();
        AnimationSystem = combat.GetComponent<Player_AnimationSystem>();
        AudioSystem = combat.GetComponent<Player_AudioSystem>();
        animator = GetComponent<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        CombatSystem.SetCanSwitch(true);

        m_damage = weapon.m_damage;
        m_maxAmmo = weapon.m_maxAmmo;
        m_range = weapon.m_range;
        m_weaponRecoilForce = weapon.m_recoilForce;
        m_impact = weapon.m_impactForce;

        currentAmmo = data.m_currentAmmo;
        stockedAmmo = data.m_stockedAmmo;

        armsAnimator.SetTrigger(weapon.m_weapon.ToString());

        OnWeaponUpgrade(data);
    }

    public void OnWeaponUpgrade(WeaponData data) {
        m_fireRateMultiplier = data.m_fireRateMultiplier < 1 ? 1 : data.m_fireRateMultiplier;
        m_reloadSpeedMultiplier = data.m_reloadSpeedMultiplier < 1 ? 1 : data.m_reloadSpeedMultiplier;

        HandleWeaponMultipliers();
    }
    #endregion

    #region Private calls
    private void HandleWeaponMultipliers() {
        animator.SetFloat("FireRate_Multiplier", m_fireRateMultiplier);
        animator.SetFloat("ReloadSpeed_Multiplier", m_reloadSpeedMultiplier);
    }

    private void Reload() {
        CombatSystem.SetCanSwitch(false);

        AnimationSystem.OnReload();
        animator.SetTrigger("Reload");
        isReloading = true;
    }
    #endregion

    #region Public functions
    public virtual void CallFire() {
        if (isReloading || isShooting || (currentAmmo <= 0 && stockedAmmo <= 0)) return;

        if (currentAmmo <= 0 && stockedAmmo > 0) {
            CallReload();
            return;
        }

        isShooting = true;

        animator.SetTrigger("Shoot");
        AnimationSystem.OnShot();
    }

    public virtual void CallReload() {
        if (isReloading || isShooting || stockedAmmo <= 0) return;

        if (currentAmmo < m_maxAmmo)
            Reload();        
    }
    #endregion

    #region FireEvents
    protected virtual void OnShot() {
        if (!CombatSystem.IsOwner) return;

        currentAmmo--;

        Singleton.Instance.GameEvents.OnAmmoConsumed?.Invoke(weapon.id, currentAmmo, stockedAmmo);

        muzzleFlash.Play();
        smokeFX.Play();

        impulseSource.GenerateImpulse(new Vector3(-m_weaponRecoilForce, 0, 0));

        ray = CameraMovement.GetPlayerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));        
    }
    #endregion

    #region Animation events
    protected abstract void Fire();

    public virtual void OnReloadEnd() {
        int prevCurrentAmmo = currentAmmo;
        currentAmmo = stockedAmmo + currentAmmo >= m_maxAmmo ? m_maxAmmo : currentAmmo + stockedAmmo;

        stockedAmmo -= m_maxAmmo - prevCurrentAmmo;
        if (stockedAmmo <= 0) stockedAmmo = 0;

        isReloading = false;

        Singleton.Instance.GameEvents.OnAmmoConsumed?.Invoke(weapon.id, currentAmmo, stockedAmmo);

        CombatSystem.SetCanSwitch(true);
    }

    public virtual void OnFireEnd() {
        CombatSystem.SetCanSwitch(true);

        if (currentAmmo == 0 && stockedAmmo > 0)
            Reload();       

        isShooting = false;
    }
    #endregion
    
    private void Update() { 
        if (!Singleton.Instance.GameManager._developmentMode || !CombatSystem.IsOwner) return;

        if (UnityEngine.Input.GetKeyDown(KeyCode.L)){
            stockedAmmo++;
            Singleton.Instance.GameEvents.OnAmmoConsumed?.Invoke(weapon.id, currentAmmo, stockedAmmo);
        }
    }
}

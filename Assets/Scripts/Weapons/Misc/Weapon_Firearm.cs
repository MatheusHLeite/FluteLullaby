using Unity.Cinemachine;
using UnityEngine;

public abstract class Weapon_Firearm : MonoBehaviour, IWeapon {
    [Header("Weapon setup")]    
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem smokeFX;

    protected int layerToIgnore;

    #region Protected variables
    protected Transform weaponMuzzle;

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
    private int stockedAmmo;
    private int remainingAmmo;
    private int maxAmmo;

    private float minimumShotTimeCooldown;
    private float minimumShotTime;

    private float fireRateMultiplier;
    private float reloadSpeedMultiplier;
    private float weaponRecoilForce;

    private Animator animator;
    private Player_CombatSystem CombatSystem;    
    private CinemachineImpulseSource impulseSource;

    private const string ShootAnimTrigger = "Shoot";
    private const string ReloadAnimTrigger = "Reload";

    private const string FireRate = "FireRate_Multiplier";
    private const string ReloadSpeed = "ReloadSpeed_Multiplier";

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
    public virtual void SetupWeapon(Item_SO item, Player_CombatSystem combat) {
        weapon = item as LongRangeWeapon_SO;
        FirearmWeaponData data = Singleton.Instance.SaveManager.GetLongRangeWeaponFromInventory(item.id).firearmData;

        layerToIgnore = LayerMask.GetMask("NPCVisual");

        CombatSystem = combat;
        CameraMovement = combat.GetComponent<Player_CameraMovementSystem>();
        AnimationSystem = combat.GetComponent<Player_AnimationSystem>();
        AudioSystem = combat.GetComponent<Player_AudioSystem>();
        animator = GetComponent<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        m_damage = weapon.m_damage;
        maxAmmo = weapon.m_maxAmmo;
        m_range = weapon.m_range;
        weaponRecoilForce = weapon.m_recoilForce;
        m_impact = weapon.m_impactForce;
        minimumShotTimeCooldown = weapon.m_fireRate;

        OnWeaponUpgrade(data);

        currentAmmo = data.m_currentAmmo;
        stockedAmmo = Singleton.Instance.SaveManager.GetAllItemQuantities(weapon.m_ammo.id);

        Singleton.Instance.GameEvents.OnItemCollected.AddListener((item, i, o, b) => OnAmmoCollected());
        Singleton.Instance.GameEvents.OnItemDropped.AddListener(i => OnAmmoCollected());

        weaponMuzzle = muzzleFlash.transform;
        currentAmmo = 999;
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnItemCollected.RemoveListener((item, i, o, b) => OnAmmoCollected());
        Singleton.Instance.GameEvents.OnItemDropped.RemoveListener(i => OnAmmoCollected());
    }

    public void OnWeaponUpgrade(FirearmWeaponData data) {
        fireRateMultiplier = data.m_fireRateMultiplier < 1 ? 1 : data.m_fireRateMultiplier;
        reloadSpeedMultiplier = data.m_reloadSpeedMultiplier < 1 ? 1 : data.m_reloadSpeedMultiplier;

        HandleWeaponMultipliers();
    }
    #endregion

    #region Private calls
    private void HandleWeaponMultipliers() {
        animator.SetFloat(FireRate, fireRateMultiplier);
        animator.SetFloat(ReloadSpeed, reloadSpeedMultiplier);
    }

    private void StartReload() {
        CombatSystem.SetCanSwitch(false);

        Singleton.Instance.GameEvents.OnWeaponReload?.Invoke(true, weapon.m_weaponType);

        AnimationSystem.OnReload();
        animator.SetTrigger(ReloadAnimTrigger);
        isReloading = true;
    }

    private void UpdateAmmo() => Singleton.Instance.GameEvents.OnAmmoUpdated?.Invoke(weapon, currentAmmo, stockedAmmo, remainingAmmo);     

    private void OnAmmoCollected() {
        stockedAmmo = Singleton.Instance.SaveManager.GetAllItemQuantities(weapon.m_ammo.id);

        remainingAmmo = 0;
        UpdateAmmo();
    }
    #endregion

    #region Public functions
    public void Fire(Player_CombatSystem combat) {
        if (isReloading || isShooting || (currentAmmo <= 0 && stockedAmmo <= 0)) return;

        if (currentAmmo <= 0 && stockedAmmo > 0) {
            Reload(combat);
            return;
        }

        if (Time.time < minimumShotTime) return;
        minimumShotTime = Time.time + minimumShotTimeCooldown;

        isShooting = true;

        animator.SetTrigger(ShootAnimTrigger);
        AnimationSystem.OnShot();

        Fire();
    }

    public void Reload(Player_CombatSystem combat) {
        if (isReloading || isShooting || stockedAmmo <= 0) return;

        if (currentAmmo < maxAmmo)
            StartReload();
    }

    /*public virtual void CallFire() {
        
    }

    public virtual void CallReload() {
           
    }*/
    #endregion

    #region FireEvents
    protected virtual void OnShot() {
        if (!CombatSystem.IsOwner) return;

        currentAmmo--;

        remainingAmmo = 0;
        UpdateAmmo();

        muzzleFlash.Play();
        smokeFX.Play();

        impulseSource.GenerateImpulse(new Vector3(-weaponRecoilForce, 0, 0));

        ray = CameraMovement.GetPlayerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
    }
    #endregion

    private void UnloadWeapon() {
        stockedAmmo += currentAmmo;
        //UpdateAmmo();
    }

    #region Animation events
    protected abstract void Fire();

    public virtual void OnReloadEnd() {
        int prevCurrentAmmo = currentAmmo;
        currentAmmo = stockedAmmo + currentAmmo >= maxAmmo ? maxAmmo : currentAmmo + stockedAmmo;

        int ammoDifference = maxAmmo - prevCurrentAmmo;
        stockedAmmo -= ammoDifference;
        if (stockedAmmo <= 0) stockedAmmo = 0;

        isReloading = false;

        remainingAmmo = ammoDifference;
        UpdateAmmo();

        Singleton.Instance.GameEvents.OnWeaponReload?.Invoke(false, weapon.m_weaponType);
        CombatSystem.SetCanSwitch(true);
    }

    public virtual void OnFireEnd() {
        CombatSystem.SetCanSwitch(true);

        if (currentAmmo == 0 && stockedAmmo > 0)
            StartReload();       

        isShooting = false;
    }    
    #endregion
}

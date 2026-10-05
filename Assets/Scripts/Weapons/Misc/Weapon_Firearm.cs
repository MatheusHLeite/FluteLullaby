using Unity.Cinemachine;
using UnityEngine;

public abstract class Weapon_Firearm : MonoBehaviour, IWeapon {
    [Header("Weapon setup")]    
    [SerializeField] private ParticleSystem muzzleFlash;
    [SerializeField] private ParticleSystem smokeFX;

    #region Protected variables
    protected float m_damage;
    #endregion

    #region Private variables
    private Transform weaponMuzzle;
    private RaycastHit hit;

    private int currentAmmo;   
    private int stockedAmmo;
    private int remainingAmmo;
    private int maxAmmo;

    private float _minSpread;
    private float _maxSpread;
    private float _spreadPerShot;
    private float _spreadRecovery;
    private float _defaultSpreadRecoveryTime;

    private float _range;
    private float _impact;
    private float _currentSpread;

    private float minimumShotTimeCooldown;
    private float minimumShotTime;
    private float lastShotTime;

    private float fireRateMultiplier;
    private float reloadSpeedMultiplier;
    private float weaponRecoilForce;

    private bool isReloading;
    private bool isShooting;

    private int layerToIgnore;

    private UI_PlayerHUD hud;

    private Animator animator;

    private Player_CombatSystem CombatSystem;
    private Player_MovementSystem MovementSystem;
    private Player_CameraMovementSystem CameraMovement;
    private Player_AnimationSystem AnimationSystem;
    private Player_AudioSystem AudioSystem;
    private CinemachineImpulseSource impulseSource;

    private const string ShootAnimTrigger = "Shoot";
    private const string ReloadAnimTrigger = "Reload";

    private const string FireRate = "FireRate_Multiplier";
    private const string ReloadSpeed = "ReloadSpeed_Multiplier";

    private LongRangeWeapon_SO weapon;
    #endregion

    #region Get
    public Item_SO GetItem() => weapon;
    public int GetCurrentAmmo() => currentAmmo;
    public int GetStockedAmmo() => stockedAmmo;
    public WeaponClass GetWeaponClass() => weapon.m_weaponType;
    #endregion

    #region Public setup
    public virtual void SetupWeapon(Item_SO item, Player_CombatSystem combat) {
        weapon = item as LongRangeWeapon_SO;
        FirearmWeaponData data = Singleton.Instance.SaveManager.GetLongRangeWeaponFromInventory(item.id).firearmData;

        layerToIgnore = LayerMask.GetMask("NPCVisual");

        CombatSystem = combat;
        MovementSystem = combat.GetComponent<Player_MovementSystem>();
        CameraMovement = combat.GetComponent<Player_CameraMovementSystem>();
        AnimationSystem = combat.GetComponent<Player_AnimationSystem>();
        AudioSystem = combat.GetComponent<Player_AudioSystem>();
        animator = GetComponent<Animator>();
        impulseSource = GetComponent<CinemachineImpulseSource>();

        hud = UI_PlayerHUD.Instance;

        m_damage = weapon.m_damage;
        maxAmmo = weapon.m_maxAmmo;
        _range = weapon.m_range;
        weaponRecoilForce = weapon.m_recoilForce;
        _impact = weapon.m_impactForce;
        minimumShotTimeCooldown = weapon.m_fireRate;

        _minSpread = weapon.m_minSpread;
        _maxSpread = weapon.m_maxSpread;
        _spreadPerShot = weapon.m_spreadPerShot;
        _spreadRecovery = weapon.m_spreadRecovery;
        _defaultSpreadRecoveryTime = weapon.m_defaultSpreadRecoveryTime;

        _currentSpread = _minSpread;

        OnWeaponUpgrade(data);

        currentAmmo = data.m_currentAmmo;
        stockedAmmo = Singleton.Instance.SaveManager.GetAllItemQuantities(weapon.m_ammo.id);

        Singleton.Instance.GameEvents.OnItemCollected.AddListener((item, i, o, b) => OnAmmoCollected());
        Singleton.Instance.GameEvents.OnItemDropped.AddListener((i, t) => OnAmmoCollected());

        weaponMuzzle = muzzleFlash.transform;
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnItemCollected.RemoveListener((item, i, o, b) => OnAmmoCollected());
        Singleton.Instance.GameEvents.OnItemDropped.RemoveListener((i, t) => OnAmmoCollected());
    }

    public void OnWeaponUpgrade(FirearmWeaponData data) {
        fireRateMultiplier = Mathf.Clamp(data.m_fireRateMultiplier, 1f, 3f);
        reloadSpeedMultiplier = Mathf.Clamp(data.m_reloadSpeedMultiplier, 1f, 2f);

        HandleWeaponMultipliers();
    }
    #endregion

    #region Private calls
    private void HandleWeaponMultipliers() {
        animator.SetFloat(FireRate, fireRateMultiplier);
        animator.SetFloat(ReloadSpeed, reloadSpeedMultiplier);

        AnimationSystem.SetFireRateSpeedMultiplier(FireRate, fireRateMultiplier);
        AnimationSystem.SetReloadSpeedMultiplier(ReloadSpeed, reloadSpeedMultiplier);
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
        if (isReloading || isShooting || (currentAmmo <= 0 && stockedAmmo <= 0)) 
            return;

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
    #endregion

    #region FireEvents
    protected virtual void PerformShot(float damage) {
        Ray ray = CreateSpreadRay();
        Vector3 dir = ray.direction;

        Physics.Raycast(ray, out hit, _range, ~layerToIgnore);

        if (hit.collider != null && hit.collider.TryGetComponent(out Damagable_BodyPart damagable))
            damagable.TakeDamage(damage, hit.point, dir, _impact);

        Singleton.Instance.GameEvents.OnShot?.Invoke(weaponMuzzle.position, hit, dir);
    }

    protected virtual void OnShot() {
        if (!CombatSystem.IsOwner) 
            return;

        currentAmmo--;

        remainingAmmo = 0;
        UpdateAmmo();

        muzzleFlash.Play();
        smokeFX.Play();

        impulseSource.GenerateImpulse(new Vector3(-weaponRecoilForce, 0, 0));
        AudioSystem.CallPlayShotSFX(weapon.m_weaponType);

        IncreaseSpread(_spreadPerShot);
        lastShotTime = Time.time + _spreadRecovery;
    }
    #endregion

    #region Spread
    private void IncreaseSpread(float spreadValue) {
        _currentSpread += spreadValue;
        _currentSpread = Mathf.Clamp(_currentSpread, _minSpread, _maxSpread);
    }

    private void RecoverSpread() {
        if (lastShotTime > Time.time || _currentSpread <= _minSpread)
            return;

        float recoveryTime = _defaultSpreadRecoveryTime;
        if (MovementSystem.IsCrouched)
            recoveryTime = _defaultSpreadRecoveryTime * 1.6f;

        _currentSpread = Mathf.Lerp(_currentSpread, _minSpread, Time.deltaTime * recoveryTime);
    }

    private Ray CreateSpreadRay() {
        Camera camera = CameraMovement.GetPlayerCamera;
        Vector3 forward = camera.transform.forward;

        if (_currentSpread <= 0.35f)
            return new Ray(camera.transform.position, forward);

        float angle = Random.Range(0f, _currentSpread);
        float rotation = Random.Range(0f, 360f);

        Vector3 right = camera.transform.right;
        Vector3 up = camera.transform.up;
        Vector3 offset = right * Mathf.Cos(rotation * Mathf.Deg2Rad) + up * Mathf.Sin(rotation * Mathf.Deg2Rad);
        Vector3 direction = Quaternion.AngleAxis(angle, offset) * forward;

        return new Ray(camera.transform.position, direction.normalized);
    }

    private void UpdateSpread() {
        bool shouldIncreaseSpread = false;
        float targetSpread = 0;

        if (!MovementSystem.IsGrounded) {
            shouldIncreaseSpread = true;
            targetSpread = _maxSpread;
        }

        if (MovementSystem.IsMoving) {
            if (MovementSystem.IsCrouched)
                targetSpread = _minSpread + 0.1f;
            else {
                shouldIncreaseSpread = true;
                float multiplier = MovementSystem.IsSprinting ? .75f : .5f;

                targetSpread = _maxSpread * multiplier;
            }
        }

        if (shouldIncreaseSpread) {
            _currentSpread = Mathf.Lerp(_currentSpread, targetSpread, Time.deltaTime * _defaultSpreadRecoveryTime);
            return;
        }

        RecoverSpread();
    }
    #endregion

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

    #region Update
    private void Update() {
        UpdateSpread();        
        hud.SetSpread(_currentSpread, _minSpread, _maxSpread);
    }
    #endregion
}

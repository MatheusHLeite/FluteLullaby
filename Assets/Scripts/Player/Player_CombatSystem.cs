using DelightStudio.AI;
using DelightStudio.Data;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class Player_CombatSystem : NetworkBehaviour {
    [Header("Setup")]    
    [SerializeField] private float m_parryWindowDuration = 0.25f;
    [SerializeField] private float m_parrySpamPenalty = 0.5f;

    public IWeapon CurrentHandItemAction { private set; get; }

    private Weapon_Firearm _firearm;
    private Weapon_Melee _melee;

    private bool _canSwitchWeapons;
    private float _parryTimer;
    private float _parryCooldownTimer;
    private float _defenseBreakTime;
    private float _invulnerabilityTime;

    private CinemachineImpulseSource _impulseSource;

    private NetworkVariable<bool> IsBlocking_NV = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> IsParryActive_NV = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #region Private references
    private Player_InputHandler Input;
    private Player_AnimationSystem Animator;
    private Player_InventorySystem Inventory;
    private Player_CameraMovementSystem Camera;
    private Player_MovementSystem Movement;
    private Player_HealthSystem HealthSystem;
    private Player_VisualManagementSystem Visual;
    #endregion

    public bool IsBlocking => IsBlocking_NV.Value;
    public bool ParryWindowOpened => IsParryActive_NV.Value;
    public bool IsInvulnerable => _invulnerabilityTime > Time.time;
    public bool IsMeleeWeaponEquipped => _melee != null && _firearm == null;

    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        Animator = GetComponent<Player_AnimationSystem>();
        Inventory = GetComponent<Player_InventorySystem>();
        Camera = GetComponent<Player_CameraMovementSystem>();
        HealthSystem = GetComponent<Player_HealthSystem>();
        Movement = GetComponent<Player_MovementSystem>();
        Visual = GetComponent<Player_VisualManagementSystem>();
        _impulseSource = GetComponent<CinemachineImpulseSource>();

        SetCanSwitch(true);
    }
    #endregion

    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnActualSlotItemSet.AddListener(OnSlotSelected);
    }

    public void DeinitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnActualSlotItemSet.RemoveListener(OnSlotSelected);
    }
    #endregion

    public void SetCanSwitch(bool canSwitch) => _canSwitchWeapons = canSwitch;

    public bool GetCanSwitch() => _canSwitchWeapons;

    private void HandleAttack() {
        if (CurrentHandItemAction == null || IsBlocking) 
            return;

        if (Input.Attack)
            CurrentHandItemAction.Fire(this);

        if (Input.Reload)
            CurrentHandItemAction.Reload(this);
    }

    #region Block an parry
    private void HandleBlock() {
        if (!IsMeleeWeaponEquipped)
            return;

        if (!IsBlocking && (!_canSwitchWeapons || Movement.StaminaRemaining < 2.5f || _defenseBreakTime > Time.time))
            return;

        if (IsInvulnerable)
            return;

        if (_parryCooldownTimer > 0f) 
            _parryCooldownTimer -= Time.deltaTime;

        bool openParryWindow = false;

        if (Input.Block && !IsBlocking) {
            if (_parryCooldownTimer <= 0f) {
                openParryWindow = true;
                _parryTimer = m_parryWindowDuration;
            }

            SetBlockState(true, openParryWindow);  
        }
        else if (!Input.Block && IsBlocking) 
            CancelBlock();        
    }

    public void BreakDefense(Vector3 pos) {
        _defenseBreakTime = Time.time + 2f;

        _impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = 1.65f;
        _impulseSource.GenerateImpulseWithVelocity(new Vector3(3, .5f, 0));

        CancelBlock();
        Visual.OnDefenseBroken(pos);
    }

    private void CancelBlock() {
        _parryCooldownTimer = m_parrySpamPenalty;
        SetBlockState(false, false);
    }

    public void PerformParry(NetworkObject attacker, Vector3 pos) {
        SetCanSwitch(false);

        _invulnerabilityTime = Time.time + 1.24f;

        _impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = 0.45f;
        _impulseSource.GenerateImpulseWithVelocity(Vector3.one * .5f);

        Animator.OnParry();
        SetBlockState(false, false);

        Visual.OnParry(pos);

        if (attacker != null) 
            StunEnemyServerRpc(attacker.NetworkObjectId);        
    }

    private void HandleParryWindow() {
        if (!ParryWindowOpened)
            return;

        _parryTimer -= Time.deltaTime;

        if (_parryTimer <= 0f) 
            SetBlockState(IsBlocking, false);        
    }

    private void SetBlockState(bool blocking, bool parrying) {
        SetCanSwitch(!blocking);

        Animator.OnBlock(blocking);
        SetBlockStateServerRpc(blocking, parrying);

    }

    [Rpc(SendTo.Server)]
    private void SetBlockStateServerRpc(bool isBlocking, bool isParrying) {
        IsBlocking_NV.Value = isBlocking;
        IsParryActive_NV.Value = isParrying;
    }
    #endregion

    [Rpc(SendTo.Server)]
    private void StunEnemyServerRpc(ulong attackerId) {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(attackerId, out NetworkObject enemyObj))
            return;

        if (enemyObj.TryGetComponent(out Enemy_Combat enemy))
            enemy.ApplyStaggerAmount(350);        
    }

    private void OnSlotSelected(Item_SO item, bool hasItemPreviously) {
        ResetCurrentWeapon();
        Animator.ChangeIdleState(item as Weapon, hasItemPreviously); 
    }

    private void ResetCurrentWeapon() {
        CurrentHandItemAction = null;
        _firearm = null;

        Singleton.Instance.GameEvents.OnCurrentWeaponChanged?.Invoke(null, true);
    }

    public void SetWeapon(IWeapon weaponEquipped, Item_SO item) {
        CurrentHandItemAction = null;
        _firearm = null;
        _melee = null;

        if (weaponEquipped == null) 
            return;

        CurrentHandItemAction = weaponEquipped;

        Weapon_Firearm firearm = CurrentHandItemAction as Weapon_Firearm;
        Weapon_Melee melee = CurrentHandItemAction as Weapon_Melee;

        if (item == null)
            return;

        if (firearm != null)
            SetFirearm(firearm, item);
        else if (melee != null)
            SetMelee(melee, item);
    }

    private void SetFirearm(Weapon_Firearm firearm, Item_SO item) {
        if (item == null) 
            return;

        firearm.SetupWeapon(item, this);
        _firearm = firearm;

        Singleton.Instance.GameEvents.OnCurrentWeaponChanged?.Invoke(_firearm, false);

    }

    private void SetMelee(Weapon_Melee melee, Item_SO item) {
        if (item == null)
            return;

        melee.SetupWeapon(item, this);
        _melee = melee;

        Singleton.Instance.GameEvents.OnCurrentWeaponChanged?.Invoke(_melee, false);
    }

    public void Tick(bool isOwner) {
        if (!isOwner || HealthSystem.IsDead || GameManager.GetGameState() != GameState.Resumed) 
            return;

        HandleAttack();
        HandleBlock();
        HandleParryWindow();
    }

    #region Melee events
    public void OnAttackHit() {
        if (_melee == null)
            return;

        _melee.OnAttackHit(); 
    }

    public void OnComboWindowOpen() {
        if (_melee == null)
            return;

        _melee.OnComboWindowOpen();
    }

    public void OnAttackEnd() {
        if (_melee == null)
            return;

        _melee.OnAttackEnd();
    }
    #endregion
}
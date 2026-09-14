using DelightStudio.AI;
using DelightStudio.Data;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class Player_CombatSystem : NetworkBehaviour {
    [SerializeField] private GameObject m_parryVFX;
    [SerializeField] private GameObject m_defenseBrokenVFX;
    [SerializeField] private float m_parryWindowDuration = 0.25f;
    [SerializeField] private float m_parrySpamPenalty = 0.5f;

    public IWeapon CurrentHandItemAction { private set; get; }

    private Weapon_Firearm _firearm;
    private Weapon_Melee _melee;

    private bool _canSwitchWeapons;
    private float _parryTimer;
    private float _parryCooldownTimer;
    private float _defenseBreakTime;

    private CinemachineImpulseSource _impulseSource;

    private enum VisualEffectsType { ParryFX, DefenseFX }

    public NetworkVariable<bool> IsBlocking_NV = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsParryActive_NV = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #region Private references
    private Player_InputHandler Input;
    private Player_AnimationSystem Animator;
    private Player_InventorySystem Inventory;
    private Player_CameraMovementSystem Camera;
    private Player_MovementSystem Movement;
    private Player_HealthSystem HealthSystem;
    #endregion

    public bool IsBlocking { get; private set; }
    public bool ParryWindowOpened { get; private set; }


    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        Animator = GetComponent<Player_AnimationSystem>();
        Inventory = GetComponent<Player_InventorySystem>();
        Camera = GetComponent<Player_CameraMovementSystem>();
        HealthSystem = GetComponent<Player_HealthSystem>();
        Movement = GetComponent<Player_MovementSystem>();
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
        if (CurrentHandItemAction == null || !_canSwitchWeapons || IsBlocking) 
            return;

        if (Input.Attack)
            CurrentHandItemAction.Fire(this);

        if (Input.Reload)
            CurrentHandItemAction.Reload(this);
    }

    #region Block an parry
    private void HandleBlock() {
        if (!IsBlocking && (!_canSwitchWeapons || Movement.StaminaRemaining < 2.5f || _defenseBreakTime > Time.time))
            return;

        if (_parryCooldownTimer > 0f) 
            _parryCooldownTimer -= Time.deltaTime;

        if (Input.Block && !IsBlocking) {
            IsBlocking = true;

            if (_parryCooldownTimer <= 0f) {
                ParryWindowOpened = true;
                _parryTimer = m_parryWindowDuration;
            }

            SetBlockState();  
        }
        else if (!Input.Block && IsBlocking) 
            CancelBlock();        
    }

    public void BreakDefense(Vector3 pos) {
        _defenseBreakTime = Time.time + 2f;

        _impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = 1.65f;
        _impulseSource.GenerateImpulseWithVelocity(new Vector3(3, .5f, 0));

        CancelBlock();
        RequestVFXSpawn(pos, VisualEffectsType.DefenseFX);
    }

    private void CancelBlock() {
        IsBlocking = false;
        ParryWindowOpened = false;

        _parryCooldownTimer = m_parrySpamPenalty;

        SetBlockState();
    }

    public void PerformParry(NetworkObject attacker, Vector3 pos) {
        SetCanSwitch(false);

        IsBlocking = false;
        ParryWindowOpened = false;
        
        _impulseSource.ImpulseDefinition.TimeEnvelope.DecayTime = 0.45f;
        _impulseSource.GenerateImpulseWithVelocity(Vector3.one * .5f);

        Animator.OnParry();
        SetBlockState();

        Singleton.Instance.GlobalTimeManager.TriggerParrySlowMotion(0.6f);

        RequestVFXSpawn(pos, VisualEffectsType.ParryFX);

        if (attacker != null) 
            StunEnemyServerRpc(attacker.NetworkObjectId);        
    }

    private void HandleParryWindow() {
        if (!ParryWindowOpened)
            return;

        _parryTimer -= Time.deltaTime;

        if (_parryTimer <= 0f) {
            ParryWindowOpened = false;
            SetBlockState();
        }
    }

    private void SetBlockState() {
        Animator.OnBlock(IsBlocking);
        SetBlockStateServerRpc(IsBlocking, ParryWindowOpened);
    }

    [Rpc(SendTo.Server)]
    private void SetBlockStateServerRpc(bool isBlocking, bool isParryActive) {
        IsBlocking_NV.Value = isBlocking;
        IsParryActive_NV.Value = isParryActive;
    }
    #endregion

    private void RequestVFXSpawn(Vector3 pos, VisualEffectsType visualEffectsType) {
        if (IsServer)
            PlayParryFXRpc(pos, visualEffectsType);
        else
            RequestParryFXSpawnRpc(pos, visualEffectsType);
    }

    [Rpc(SendTo.Server)]
    private void RequestParryFXSpawnRpc(Vector3 position, VisualEffectsType visualEffectsType) => PlayParryFXRpc(position, visualEffectsType);

    [Rpc(SendTo.ClientsAndHost)]
    private void PlayParryFXRpc(Vector3 position, VisualEffectsType visualEffectsType) {
        GameObject correctFX = GetCorrectFX(visualEffectsType);

        if (correctFX == null)
            return;

        GameObject vfxInstance = Instantiate(correctFX, position, Quaternion.LookRotation(transform.forward));
        Destroy(vfxInstance, 3f);
    }

    private GameObject GetCorrectFX(VisualEffectsType visualEffectsType) {
        return visualEffectsType switch { 
            VisualEffectsType.DefenseFX => m_defenseBrokenVFX,
            VisualEffectsType.ParryFX => m_parryVFX,
            _ => null
        };
    }

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

        Singleton.Instance.GameEvents.OnAmmoUISet?.Invoke(null);
    }

    public void SetWeapon(IWeapon weaponEquipped, Item_SO item) {
        CurrentHandItemAction = weaponEquipped != null ? weaponEquipped : null;
        _firearm = weaponEquipped != null ? CurrentHandItemAction as Weapon_Firearm : null;

        if (_firearm != null && item != null)
            _firearm.SetupWeapon(item, this);

        Singleton.Instance.GameEvents.OnAmmoUISet?.Invoke(this._firearm);
    }

    public void Tick(bool isOwner) {
        if (!isOwner || HealthSystem.IsDead || GameManager.GetGameState() != GameState.Resumed) return;

        HandleAttack();
        HandleBlock();
        HandleParryWindow();
    }
}
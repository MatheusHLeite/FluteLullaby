using DelightStudio.Data;
using Unity.Netcode;
using UnityEngine;

public class Player_CombatSystem : NetworkBehaviour {
    #region Private references
    private Player_InputHandler Input;
    private Player_AnimationSystem Animator;
    private Player_InventorySystem Inventory;
    private Player_CameraMovementSystem Camera;
    private Player_HealthSystem HealthSystem;
    #endregion

    public IWeapon CurrentHandItemAction { private set; get; }

    private Transform rightHand;
    private Weapon_Firearm firearm;
    private Weapon_Melee melee;

    private bool canSwitchWeapons;

    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        Animator = GetComponent<Player_AnimationSystem>();
        Inventory = GetComponent<Player_InventorySystem>();
        Camera = GetComponent<Player_CameraMovementSystem>();
        HealthSystem = GetComponent<Player_HealthSystem>();

        rightHand = Inventory.GetRightHand();

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

    public void SetCanSwitch(bool canSwitch) => canSwitchWeapons = canSwitch;

    public bool GetCanSwitch() => canSwitchWeapons;

    private void HandleAttack() {
        if (CurrentHandItemAction == null || !canSwitchWeapons) 
            return;

        if (Input.Attack)
            CurrentHandItemAction.Fire(this);

        if (Input.Reload)
            CurrentHandItemAction.Reload(this);
    }

    private void OnSlotSelected(Item_SO item, bool hasItemPreviously) => Animator.ChangeIdleState(item as Weapon, hasItemPreviously);

    public void SetWeapon(IWeapon weaponEquipped, Item_SO item) {
        CurrentHandItemAction = weaponEquipped;
        firearm = CurrentHandItemAction as Weapon_Firearm;

        if (firearm != null && item != null)
            firearm.SetupWeapon(item, this);

        Singleton.Instance.GameEvents.OnAmmoUISet?.Invoke(this.firearm);
    }

    public void Tick(bool isOwner) {
        if (!isOwner || HealthSystem.IsDead || GameManager.GetGameState() != GameState.Resumed) return;

        HandleAttack();        
    }
}
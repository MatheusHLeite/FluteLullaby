using System.Collections;
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

    private Item_SO _actualEquippedWeapon;

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
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnActualSlotItem.AddListener(OnSlotSelected);
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnActualSlotItem.RemoveListener(OnSlotSelected);
        }
    }
    #endregion

    public Item_SO GetActualEquippedWeapon() { return _actualEquippedWeapon; }

    public void SetCanSwitch(bool canSwitch) => canSwitchWeapons = canSwitch;

    public bool GetCanSwitch() => canSwitchWeapons;

    private void HandleAttack() {
        if (_actualEquippedWeapon == null) return;

        if (_actualEquippedWeapon.m_itemType == ItemType.MeleeWeapon) 
            HandleMeleeAttack();        
        else if (_actualEquippedWeapon.m_itemType == ItemType.Firearm)        
            HandleFirearmAttack();
    }

    private void HandleMeleeAttack() {
        if (melee == null) return;

        if (Input.Attack)
            melee.CallAttack();
    }

    private void HandleFirearmAttack() {
        if (firearm == null) return;

        if (Input.Attack) 
            firearm.CallFire();        

        if (Input.Reload) 
            firearm.CallReload();        
    }

    private void OnSlotSelected(Item_SO item) {        
        StartCoroutine(DelayedEquip(item));
    }

    private IEnumerator DelayedEquip(Item_SO item) {
        _actualEquippedWeapon = item;
        Animator.ChangeIdleState();

        yield return new WaitForEndOfFrame();

        this.firearm = rightHand.childCount > 0 && rightHand.GetChild(0).TryGetComponent(out Weapon_Firearm firearm) ? firearm : null;
        this.melee = rightHand.childCount > 0 && rightHand.GetChild(0).TryGetComponent(out Weapon_Melee melee) ? melee : null;

        Singleton.Instance.GameEvents.OnWeaponChanged?.Invoke(this.firearm);
    }

    private void Update() {
        if (!IsOwner || HealthSystem.IsDead) return;

        HandleAttack();        
    }
}
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

    private int comboStep;
    private int maxComboStep = 3;

    private bool canAttack;
    private bool canCombo;

    private Transform rightHand;
    private Weapon_Firearm weapon;

    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        Animator = GetComponent<Player_AnimationSystem>();
        Inventory = GetComponent<Player_InventorySystem>();
        Camera = GetComponent<Player_CameraMovementSystem>();
        HealthSystem = GetComponent<Player_HealthSystem>();

        rightHand = Inventory.GetRightHand();
    }

    private void Start()
    {
        canAttack = true; //[TODO] Change logic
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

    private void HandleAttack() {
        if (_actualEquippedWeapon == null) return;

        if (_actualEquippedWeapon.m_itemType == ItemType.MeleeWeapon) 
            HandleMeleeAttack();        
        else if (_actualEquippedWeapon.m_itemType == ItemType.Firearm)        
            HandleFirearmAttack();
    }

    private void HandleMeleeAttack() {
        if (Input.Attack) {
            if (canCombo || canAttack) {
                if (canCombo)                
                    IncreaseComboStep();                

                Animator.OnAttack(comboStep);
            }
        }        
    }

    private void HandleFirearmAttack() {
        if (weapon == null) return;

        if (Input.Attack) 
            weapon.CallFire();        

        if (Input.Reload) 
            weapon.CallReload();        
    }

    public void IncreaseComboStep() {
        comboStep++;

        if (comboStep > maxComboStep)
            ResetComboStep();        
    }

    public void ResetComboStep() {
        comboStep = 0;
        canCombo = false;
    }

    public void SetAttackWindow(bool attack) {
        canAttack = attack;
    }

    public void SetComboWindow(bool combo) {
        canCombo = combo;
    }

    private void OnSlotSelected(Item_SO item) {        
        StartCoroutine(DelayedEquip(item));
    }

    private IEnumerator DelayedEquip(Item_SO item) {
        _actualEquippedWeapon = item;
        Animator.ChangeIdleState();

        yield return new WaitForEndOfFrame();

        if (rightHand.childCount > 0 && rightHand.GetChild(0).TryGetComponent(out Weapon_Firearm weapon)) {
            this.weapon = weapon;
            weapon.SetupWeapon(_actualEquippedWeapon, this);
        }
        else 
            this.weapon = null;       
        
        Singleton.Instance.GameEvents.OnWeaponChanged?.Invoke(this.weapon);
    }

    private void Update() {
        if (!IsOwner || HealthSystem.IsDead) return;

        HandleAttack();        
    }
}
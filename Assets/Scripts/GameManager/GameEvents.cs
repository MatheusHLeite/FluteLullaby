using UnityEngine;
using UnityEngine.Events;

public class GameEvents : MonoBehaviour {
    #region Network
    public UnityEvent OnHostStarted { get; set; } = new();
    public UnityEvent OnClientStarted { get; set; } = new();
    #endregion

    #region Data
    public UnityEvent<PlayerSaveData> OnDataLoaded { get; private set; } = new();
    public UnityEvent OnPlayerLoaded { get; private set; } = new();
    public UnityEvent<ItemData> OnItemSaved { get; private set; } = new();
    public UnityEvent<ItemData> OnItemUpdated { get; private set; } = new();
    public UnityEvent<LongRangeWeapon_SO, int, int, int> OnAmmoUpdated { get; private set; } = new();
    public UnityEvent<PlayerSaveData> OnSettingsDataLoaded { get; private set; } = new();
    #endregion

    #region Game Management
    public UnityEvent OnGameStarted { get; private set; } = new();
    public UnityEvent OnGameResumed { get; private set; } = new();
    public UnityEvent OnGamePaused { get; private set; } = new();
    public UnityEvent OnInventoryOpened { get; private set; } = new();
    public UnityEvent<Vector3, Vector3, float> OnPlayerDie { get; private set; } = new();
    public UnityEvent OnPlayerRespawn { get; private set; } = new();
    #endregion

    #region Inventory management
    public UnityEvent<Item_SO, int, int, bool> OnItemCollected { get; private set; } = new();
    public UnityEvent<int> OnItemDropped { get; private set; } = new();
    public UnityEvent<ItemData, int> OnInventoryItemAdded { get; private set; } = new();
    public UnityEvent<ItemData, int> OnInventoryItemRemoved { get; private set; } = new();
    public UnityEvent<ItemData, int> OnItemSplit { get; private set; } = new();        
    public UnityEvent<int, int> OnQuickSlotItemUpdated { get; private set; } = new();
    public UnityEvent<ItemData, int, int> OnInventoryItemSlotChanged { get; private set; } = new();
    public UnityEvent<Weapon_Firearm> OnWeaponChanged { get; private set; } = new();
    public UnityEvent<int> OnSlotSelected { get; private set; } = new();
    public UnityEvent<Item_SO> OnActualSlotItemSet { get; private set; } = new();
    public UnityEvent<int> OnDragBegun { get; private set; } = new();
    #endregion

    #region Player management
    public UnityEvent<float, float> OnHealthSet { get; private set; } = new();
    public UnityEvent<float, float> OnDamageTaken { get; private set; } = new();
    public UnityEvent<float, float> OnStaminaUsage { get; private set; } = new();
    #endregion

    #region Settings
    public UnityEvent<float> OnSensitivityChange { get; private set; } = new();
    public UnityEvent<string> OnMicrophoneDeviceSwitch { get; private set; } = new();
    public UnityEvent<Quality> OnGlobalEffectsQualityChanged { get; private set; } = new();
    public UnityEvent<InputSystem_Actions> OnPlayerInputLoaded { get; private set; } = new();
    public UnityEvent OnBindsUpdated { get; private set; } = new();
    #endregion

    #region Interaction
    public UnityEvent<bool, Weapons> OnWeaponReload { get; set; } = new();
    public UnityEvent<string> OnHoverOverItem { get; set; } = new();
    public UnityEvent OnHit { get; private set; } = new();
    public UnityEvent OnKill { get; private set; } = new();
    public UnityEvent<RaycastHit> OnShotHit { get; private set; } = new();
    #endregion
}
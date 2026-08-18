using DelightStudio.Data;
using UnityEngine;
using UnityEngine.Events;

public class GameEvents : MonoBehaviour {
    #region Network
    public UnityEvent OnHostStarted { get; set; } = new();
    public UnityEvent OnClientStarted { get; set; } = new();
    #endregion

    #region Data
    public UnityEvent<PlayerSaveData> OnDataLoaded { get; private set; } = new();
    public UnityEvent<Player_Manager> OnPlayerLoaded { get; private set; } = new();
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
    public UnityEvent<Weapon_Firearm> OnAmmoUISet { get; private set; } = new();
    public UnityEvent<int, bool> OnSlotSelected { get; private set; } = new();
    public UnityEvent<Item_SO, bool> OnActualSlotItemSet { get; private set; } = new();
    public UnityEvent<int> OnDragBegun { get; private set; } = new();
    #endregion

    #region Player management
    public UnityEvent<float, float> OnHealthSet { get; private set; } = new();
    public UnityEvent<float, float> OnDamageTaken { get; private set; } = new();
    public UnityEvent<float> OnStaminaConsume { get; private set; } = new();
    public UnityEvent<float> OnStaminaUISet { get; private set; } = new();
    #endregion

    #region Settings
    public UnityEvent<float> OnSensitivityChanged { get; private set; } = new();
    public UnityEvent<int> OnInvertAxisChanged { get; private set; } = new();
    public UnityEvent<int> OnCameraBobEnabledChanged { get; private set; } = new();
    public UnityEvent<int> OnPlayerIndicatorChanged { get; private set; } = new();
    public UnityEvent<int> OnUISizeChanged { get; private set; } = new();
    public UnityEvent<int> OnSprintToggleChanged { get; private set; } = new();
    public UnityEvent<int> OnDamageNumbersEnabledChanged { get; private set; } = new();
    public UnityEvent<int> OnSubtitleTypeChanged { get; private set; } = new();
    public UnityEvent<int> OnFontSizeChanged { get; private set; } = new();
    public UnityEvent<string> OnMicrophoneDeviceSwitch { get; private set; } = new();
    public UnityEvent<Quality> OnGlobalEffectsQualityChanged { get; private set; } = new();
    public UnityEvent<InputSystem_Actions> OnPlayerInputLoaded { get; private set; } = new();
    public UnityEvent<bool> OnBindsUpdated { get; private set; } = new();
    #endregion

    #region Interaction
    public UnityEvent<bool, WeaponClass> OnWeaponReload { get; set; } = new();
    public UnityEvent<string> OnHoverOverItem { get; set; } = new();
    public UnityEvent OnInteractionReset { get; set; } = new();
    public UnityEvent OnHit { get; private set; } = new();
    public UnityEvent OnKill { get; private set; } = new();
    public UnityEvent<Vector3, RaycastHit, Vector3> OnShot { get; private set; } = new();
    #endregion

    #region UI
    public UnityEvent<Item_SO> OnItemShowcaseSet { get; private set; } = new();
    public UnityEvent OnItemShowcaseUnset { get; private set; } = new();
    public UnityEvent OnScreenSwitch { get; private set; } = new();
    #endregion

    public UnityEvent<bool> LockNavigationInputs { get; set; } = new();
    public UnityEvent<ImportantDecision> OnImportantDecisionTaken { get; set; } = new();

    public UnityEvent<BestiaryData> OnNewEnemyFound { get; private set; } = new();
    public UnityEvent<BestiaryData> OnBestiaryNotificationRead { get; private set; } = new();
    public UnityEvent<Enemy_SO> OnUpdateEnemyFound { get; private set; } = new();
    public UnityEvent<Enemy_SO> OnUpdateBestiaryRead { get; private set; } = new();

    public UnityEvent<Statistic> OnStatisticUpdated { get; private set; } = new();

    public UnityEvent<NotesSaveData> OnNoteDataSaved { get; private set; } = new();

    public UnityEvent<Enemy_SO> OnEnemyKilled { get; private set; } = new();
}
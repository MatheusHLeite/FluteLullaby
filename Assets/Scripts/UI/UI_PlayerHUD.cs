using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerHUD : MonoBehaviour {
    [Header("References")]
    [SerializeField] private CanvasGroup m_hud;

    [Header("Health")]
    [SerializeField] private Image m_healthBar;
    [SerializeField] private Image m_healthBarEffect;
    [SerializeField] private GameObject m_criticalHealthIndicator;
    [SerializeField] private Color m_defaultHealthColor;
    [SerializeField] private Color m_lowHealthColor;

    [Header("Stamina")]
    [SerializeField] private Image m_staminaBar;
    [SerializeField] private CanvasGroup m_staminaBarCanvas;
    [SerializeField] private GameObject m_criticalStaminaIndicator;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text m_selectedActionIndicator;    
    [SerializeField] private TMP_Text m_ammo;
    [SerializeField] private CanvasGroup[] m_damageTakenScreenEffect;
    [SerializeField] private TMP_Text m_flaskAmount;
    
    [Header("Crosshair")]
    [SerializeField] private GameObject m_defaultCrosshair;
    [SerializeField] private CrosshairType[] m_crosshairs;

    [Header("Crosshair Parts [Default]")]
    [SerializeField] private RectTransform m_top;
    [SerializeField] private RectTransform m_bottom;
    [SerializeField] private RectTransform m_left;
    [SerializeField] private RectTransform m_right;
    [Space(5)]
    [SerializeField] private RectTransform m_shotgunCrosshairRT;
    [Space(5)]
    [SerializeField] private float m_minGap = 5f;
    [SerializeField] private float m_maxGap = 150f;

    [Header("Animation")]
    [SerializeField] private float m_animationSpeed = 15f;

    public static UI_PlayerHUD Instance;

    private float _targetGap;

    private float _maxStamina;
    private bool _staminaFull;

    private float _lastHealthValue;

    private bool isDead;

    private WeaponClass _currentWeapon;
    
    private CinemachineImpulseSource _impulseSource;
    private Camera _playerCamera;

    #region Start
    private void Awake() {
        Instance = this;

        Singleton.Instance.GameEvents.OnHoverOverItem.AddListener(SetSelectedActionText);        
        Singleton.Instance.GameEvents.OnDamageTaken.AddListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaConsume.AddListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnStaminaUISet.AddListener(SetMaxStamina);
        Singleton.Instance.GameEvents.OnAmmoUpdated.AddListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnCurrentWeaponChanged.AddListener(OnWeaponChanged);
        Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnInventoryOpened.AddListener(OnInventoryOpened);
        Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
        Singleton.Instance.GameEvents.OnFlaskAmountUpdated.AddListener(OnFlaskAmountChanged);
        Singleton.Instance.GameEvents.OnCriticalIndicatorShow.AddListener(SetCriticalIndicator);
        Singleton.Instance.GameEvents.OnHealthSet.AddListener(SetHealthUI);
        Singleton.Instance.GameEvents.OnPlayerLoaded.AddListener(SetPlayerCamera);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnHoverOverItem.RemoveListener(SetSelectedActionText);        
        Singleton.Instance.GameEvents.OnDamageTaken.RemoveListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaConsume.RemoveListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnStaminaUISet.RemoveListener(SetMaxStamina);
        Singleton.Instance.GameEvents.OnAmmoUpdated.RemoveListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnCurrentWeaponChanged.RemoveListener(OnWeaponChanged);
        Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnInventoryOpened.RemoveListener(OnInventoryOpened);
        Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
        Singleton.Instance.GameEvents.OnFlaskAmountUpdated.RemoveListener(OnFlaskAmountChanged);
        Singleton.Instance.GameEvents.OnCriticalIndicatorShow.RemoveListener(SetCriticalIndicator);
        Singleton.Instance.GameEvents.OnHealthSet.RemoveListener(SetHealthUI);
        Singleton.Instance.GameEvents.OnPlayerLoaded.RemoveListener(SetPlayerCamera);
    }

    private void Start() {        
        _impulseSource = GetComponent<CinemachineImpulseSource>();

        m_ammo.gameObject.SetActive(false);
        m_defaultCrosshair.gameObject.SetActive(true);
        m_criticalHealthIndicator.SetActive(false);
        m_criticalStaminaIndicator.SetActive(false);

        _targetGap = m_minGap;

        OnWeaponChanged(null, false);
    }

    private void SetPlayerCamera(Player_Manager player) {
        _playerCamera = player.GetPlayerCamera();
    }

    private void SetMaxStamina(float max) {
        _maxStamina = max;
    }

    private void SetHealthUI(float currentHealth, float maxHealth) {
        m_healthBar.DOKill();
        m_healthBarEffect.DOKill();

        isDead = false;
        _lastHealthValue = currentHealth;

        float fillValue = Mathf.Clamp01(currentHealth / maxHealth);
        Color correctColor = Color.Lerp(m_lowHealthColor, m_defaultHealthColor, fillValue);

        m_healthBar.color = Color.green;
        m_healthBar.DOColor(correctColor, 2f).SetDelay(.4f);

        m_healthBar.DOFillAmount(fillValue, 0.15f);
        m_healthBarEffect.DOFillAmount(fillValue, 0f).SetDelay(2f);

        HandleHUDVisibility(false, false);
    }
    #endregion

    #region HUD
    private void OnGamePaused() {
        if (isDead)
            return;

        HandleHUDVisibility(true); 
    }

    private void OnInventoryOpened() { 
        if (isDead)
            return;

        HandleHUDVisibility(true); 
    }

    private void OnGameResumed() {
        if (isDead)
            return;

        HandleHUDVisibility(false); 
    }

    private void HandleHUDVisibility(bool hide, bool shouldFade = true) {
        m_hud.DOKill();

        m_hud.interactable = !hide;
        m_hud.blocksRaycasts = !hide;

        if (!shouldFade) {
            m_hud.alpha = hide ? 0f : 1f;
            return;
        }

        m_hud.alpha = hide ? 1f : 0f;
        m_hud.DOFade(hide ? 0 : 1f, 0.2f);
    }
    #endregion

    #region Critical indicator
    private void SetCriticalIndicator(CriticalIndicator indicator, bool enable) {
        GameObject correctIndicator = indicator switch {
            CriticalIndicator.Health => m_criticalHealthIndicator,
            CriticalIndicator.Stamina => m_criticalStaminaIndicator,
            _ => null
        };

        if (correctIndicator == null)
            return;

        correctIndicator.SetActive(enable);
    }
    #endregion

    private void OnWeaponChanged(IWeapon weapon, bool isChangingWeapons) {
        WeaponClass weaponType = WeaponClass.None;
        
        m_ammo.gameObject.SetActive(false);

        if (isChangingWeapons) {
            weapon = null;
            weaponType = WeaponClass.Melee;
        }

        if (weapon != null) {
            weaponType = weapon.GetWeaponClass();
            Weapon_Firearm instance = weapon as Weapon_Firearm;

            if (instance != null) {
                m_ammo.gameObject.SetActive(true);

                int currentAmmo = instance.GetCurrentAmmo();
                int stockedAmmo = instance.GetStockedAmmo();

                m_ammo.text = $"{currentAmmo}/<size=50%>{stockedAmmo}</size>";
            }
        }

        SelectCrosshair(weaponType);
    }

    private void SelectCrosshair(WeaponClass weapon) {
        bool enableDefaultCrosshair = weapon == WeaponClass.None;
        m_defaultCrosshair.gameObject.SetActive(enableDefaultCrosshair);

        for (int i = 0; i < m_crosshairs.Length; i++)
            m_crosshairs[i].m_crosshair.SetActive(false);

        if (enableDefaultCrosshair)
            return;

        for (int i = 0; i < m_crosshairs.Length; i++)
            m_crosshairs[i].m_crosshair.SetActive(weapon == m_crosshairs[i].m_weapon);

        _currentWeapon = weapon;
    }

    private void OnAmmoSpent(LongRangeWeapon_SO weapon, int currentAmmo, int maxAmmo, int remainingAmmo) {
        m_ammo.text = $"{currentAmmo}/<size=50%>{maxAmmo}</size>";
    }

    private void OnStaminaUsage(float currentStamina) {
        m_staminaBar.fillAmount = currentStamina / _maxStamina;

        if (m_staminaBar.fillAmount >= 1 && !_staminaFull) {            
            _staminaFull = true;
            m_staminaBarCanvas.DOKill();
            m_staminaBarCanvas.DOFade(0, 0.8f).SetDelay(1f);
        }
        else if (m_staminaBar.fillAmount < 1 && _staminaFull) {
            _staminaFull = false;
            m_staminaBarCanvas.DOKill();
            m_staminaBarCanvas.DOFade(1, 0.8f);
        }
    }

    private void OnDamageTakenScreenVisual(bool isBlocking) {
        int index = Random.Range(0, m_damageTakenScreenEffect.Length);

        float force = isBlocking ? 0.3f : 1.25f;
        _impulseSource.GenerateImpulseWithForce(force);

        if (isBlocking) 
            return;

        m_damageTakenScreenEffect[index].alpha = 1;
        m_damageTakenScreenEffect[index].DOFade(0, 0.5f).SetDelay(1.7f);
    }

    private void OnDamageTaken(float currentHealth, float maxHealth, bool isBlocking) {
        isDead = currentHealth <= 0;

        if (isDead) {
            OnDamageTakenScreenVisual(false);

            m_healthBar.DOKill();
            m_healthBarEffect.DOKill();

            m_healthBar.color = m_defaultHealthColor;

            m_healthBar.fillAmount = 0f;
            m_healthBarEffect.fillAmount = 0f;

            _lastHealthValue = 0f;

            HandleHUDVisibility(true, false);
            return;
        }

        if (_lastHealthValue == 0)
            _lastHealthValue = currentHealth;

        bool isHealing = currentHealth > _lastHealthValue;
        _lastHealthValue = currentHealth;

        if (currentHealth < maxHealth && !isHealing)
            OnDamageTakenScreenVisual(isBlocking);

        float fillValue = Mathf.Clamp01(currentHealth / maxHealth);
        Color correctColor = Color.Lerp(m_lowHealthColor, m_defaultHealthColor, fillValue);

        m_healthBar.DOKill();
        m_healthBarEffect.DOKill();

        m_healthBar.color = isHealing ? Color.green : Color.red;
        m_healthBar.DOColor(correctColor, 0.3f);

        m_healthBar.DOFillAmount(fillValue, 0.09f);
        m_healthBarEffect.DOFillAmount(fillValue, 0.35f).SetDelay(1.2f);
    }

    private void SetSelectedActionText(string text) {
        m_selectedActionIndicator.text = text;
    }

    private void OnFlaskAmountChanged(int amount) {
        m_flaskAmount.text = amount.ToString();
    }

    #region Crosshair spread
    public void SetSpread(float currentSpread, float minSpread, float maxSpread) {
        _targetGap = SpreadAngleToPixels(currentSpread, minSpread, maxSpread);
        ApplyGap();
    }

    private float SpreadAngleToPixels(float currentSpread, float minSpread, float maxSpread) {
        if (_currentWeapon == WeaponClass.Shotgun)        
            return AngleToPixels(currentSpread);        

        float normalizedSpread = Mathf.InverseLerp(minSpread, maxSpread, currentSpread);
        return Mathf.Lerp(m_minGap, m_maxGap, normalizedSpread);
    }

    private float AngleToPixels(float angle) {
        float halfFov = _playerCamera.fieldOfView * 0.5f;
        float halfScreenHeight = Screen.height * 0.5f;

        float pixelsPerTan = halfScreenHeight / Mathf.Tan(halfFov * Mathf.Deg2Rad);
        return Mathf.Tan(angle * Mathf.Deg2Rad) * pixelsPerTan;
    }

    private void ApplyGap() {
        //_currentGap = Mathf.Lerp(_currentGap, _targetGap, Time.deltaTime * m_animationSpeed);

        if (_currentWeapon == WeaponClass.Revolver)        
            ApplyNormalCrosshair(_targetGap);        
        else        
            ApplyCircleCrosshair(_targetGap);        
    }

    private void ApplyNormalCrosshair(float gap) {  
        m_top.anchoredPosition = new Vector2(m_top.anchoredPosition.x, gap);
        m_bottom.anchoredPosition = new Vector2(m_bottom.anchoredPosition.x, -gap);
        m_left.anchoredPosition = new Vector2(-gap, m_left.anchoredPosition.y);
        m_right.anchoredPosition = new Vector2(gap, m_right.anchoredPosition.y);
    }

    private void ApplyCircleCrosshair(float radius) {
        m_shotgunCrosshairRT.sizeDelta = new Vector2(radius * 5f, radius * 5f);
    }
    #endregion
}

[System.Serializable]
public struct CrosshairType {
    public WeaponClass m_weapon;
    public GameObject m_crosshair;
}
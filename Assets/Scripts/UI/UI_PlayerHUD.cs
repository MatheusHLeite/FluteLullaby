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
    [SerializeField] private Color m_defaultHealthColor;
    [SerializeField] private Color m_lowHealthColor;

    [Header("Stamina")]
    [SerializeField] private Image m_staminaBar;
    [SerializeField] private CanvasGroup m_staminaBarCanvas;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text m_selectedActionIndicator;    
    [SerializeField] private TMP_Text m_ammo;
    [SerializeField] private CanvasGroup[] m_damageTakenScreenEffect;
    
    [Header("Crosshair")]
    [SerializeField] private GameObject m_defaultCrosshair;
    [SerializeField] private CrosshairType[] m_crosshairs;    

    private float _maxStamina;
    private bool _staminaFull;

    private int _crosshairType;

    private CinemachineImpulseSource _impulseSource;

    #region Start
    private void Awake() {       
        Singleton.Instance.GameEvents.OnHoverOverItem.AddListener(SetSelectedActionText);        
        Singleton.Instance.GameEvents.OnDamageTaken.AddListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaConsume.AddListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnStaminaUISet.AddListener(SetMaxStamina);
        Singleton.Instance.GameEvents.OnAmmoUpdated.AddListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnAmmoUISet.AddListener(OnWeaponChanged);
        Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnInventoryOpened.AddListener(OnInventoryOpened);
        Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnHoverOverItem.RemoveListener(SetSelectedActionText);        
        Singleton.Instance.GameEvents.OnDamageTaken.RemoveListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaConsume.RemoveListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnStaminaUISet.RemoveListener(SetMaxStamina);
        Singleton.Instance.GameEvents.OnAmmoUpdated.RemoveListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnAmmoUISet.RemoveListener(OnWeaponChanged);
        Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnInventoryOpened.RemoveListener(OnInventoryOpened);
        Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
    }

    private void Start() {        
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        m_ammo.gameObject.SetActive(false);
        m_defaultCrosshair.gameObject.SetActive(true);

        m_healthBar.fillAmount = 1f;
        m_healthBar.color = m_defaultHealthColor;
    }

    private void SetMaxStamina(float max) {
        _maxStamina = max;
    }
    #endregion

    #region HUD
    private void OnGamePaused() => HandleHUDVisibility(true);

    private void OnInventoryOpened() => HandleHUDVisibility(true);

    private void OnGameResumed() => HandleHUDVisibility(false);

    private void HandleHUDVisibility(bool hide) {
        m_hud.DOKill();

        m_hud.interactable = !hide;
        m_hud.blocksRaycasts = !hide;

        m_hud.alpha = hide ? 1f : 0f;
        m_hud.DOFade(hide ? 0 : 1f, 0.2f);
    }
    #endregion

    private void OnCrosshairTypeChanged(int index) {
        _crosshairType = index;
    }

    private void OnWeaponChanged(Weapon_Firearm weapon) {
        WeaponClass weaponType = WeaponClass.None;

        if (weapon == null || (weapon != null && weapon.GetItem().m_itemType != ItemType.Firearm)) {
            m_ammo.gameObject.SetActive(false);
        }
        else {
            if (!m_ammo.gameObject.activeSelf) m_ammo.gameObject.SetActive(true);
            m_ammo.text = $"{weapon.GetCurrentAmmo()}/<size=50%>{weapon.GetStockedAmmo()}</size>";

            weaponType = weapon.ThisWeaponClass;
        }
        
        CheckCrosshair(weaponType);
    }

    private void CheckCrosshair(WeaponClass weapon) {
        if (weapon == WeaponClass.None) {
            SelectCrosshair(weapon);
            m_defaultCrosshair.gameObject.SetActive(true);
            return;
        }

        m_defaultCrosshair.gameObject.SetActive(false);
        SelectCrosshair(weapon);
    }

    private void SelectCrosshair(WeaponClass weapon) {
        for (int i = 0; i < m_crosshairs.Length; i++) {
            for (int c = 0; c < m_crosshairs[i].m_crosshairs.Length; c++) {
                m_crosshairs[i].m_crosshairs[c].SetActive(false);
            }
        }

        for (int i = 0; i < m_crosshairs.Length; i++) {
            if (weapon == m_crosshairs[i].m_weapon) {
                m_crosshairs[i].m_crosshairs[_crosshairType].SetActive(true);
                break;
            }
        }
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

    private void OnDamageTakenScreenVisual() {
        int index = Random.Range(0, m_damageTakenScreenEffect.Length);

        m_damageTakenScreenEffect[index].alpha = 1;
        m_damageTakenScreenEffect[index].DOFade(0, 0.5f).SetDelay(1.7f);
    
        _impulseSource.GenerateImpulse();
    }

    private void OnDamageTaken(float currentHealth, float maxHealth) {
        if (currentHealth < maxHealth)
            OnDamageTakenScreenVisual();

        float fillValue = Mathf.Clamp01(currentHealth / maxHealth);
        Color correctColor = Color.Lerp(m_lowHealthColor, m_defaultHealthColor, fillValue);
        
        m_healthBar.DOKill();
        m_healthBarEffect.DOKill();

        m_healthBar.color = Color.red;
        m_healthBar.DOColor(correctColor, 0.3f);

        m_healthBar.DOFillAmount(fillValue, 0.03f);
        m_healthBarEffect.DOFillAmount(fillValue, 0.25f).SetDelay(1.2f);
    }

    private void SetSelectedActionText(string text) {
        m_selectedActionIndicator.text = text;
    }
}

[System.Serializable]
public struct CrosshairType {
    public WeaponClass m_weapon;
    public GameObject[] m_crosshairs;
}
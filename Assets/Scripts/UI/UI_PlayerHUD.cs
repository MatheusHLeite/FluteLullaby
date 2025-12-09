using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerHUD : MonoBehaviour {
    [Header("UI")]
    [SerializeField] private TMP_Text m_selectedActionIndicator;
    [SerializeField] private Image m_healthBar;
    [SerializeField] private Image m_healthBarEffect;
    [SerializeField] private Image m_staminaBar;
    [SerializeField] private CanvasGroup m_staminaBarCanvas;
    [SerializeField] private TMP_Text m_killCounter;
    [SerializeField] private TMP_Text m_deathCounter;
    [SerializeField] private CanvasGroup m_killIndicator;
    [SerializeField] private TMP_Text m_ammo;
    [SerializeField] private CanvasGroup[] m_damageTakenScreenEffect;
    
    [Header("Crosshair")]
    [SerializeField] private Animator m_crosshairAnimator;
    [SerializeField] private CrosshairType[] m_crosshairs;
    [SerializeField] private GameObject m_defaultCrosshair;

    private CinemachineImpulseSource impulseSource;

    private int _killCount;
    private int _deathCount;
    private bool m_staminaFull;

    private int crosshairType;

    private void Awake() {
        m_ammo.gameObject.SetActive(false);

        Singleton.Instance.GameEvents.OnHoverOverItem.AddListener(SetSelectedActionText);        
        Singleton.Instance.GameEvents.OnHealthSet.AddListener(OnHealthSet);
        Singleton.Instance.GameEvents.OnDamageTaken.AddListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaUsage.AddListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnHit.AddListener(OnHit);
        Singleton.Instance.GameEvents.OnKill.AddListener(OnKill);
        Singleton.Instance.GameEvents.OnAmmoUpdated.AddListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnWeaponChanged.AddListener(OnWeaponChanged);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnHoverOverItem.RemoveListener(SetSelectedActionText);        
        Singleton.Instance.GameEvents.OnHealthSet.RemoveListener(OnHealthSet);
        Singleton.Instance.GameEvents.OnDamageTaken.RemoveListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaUsage.RemoveListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnHit.RemoveListener(OnHit);
        Singleton.Instance.GameEvents.OnKill.RemoveListener(OnKill);
        Singleton.Instance.GameEvents.OnAmmoUpdated.RemoveListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnWeaponChanged.RemoveListener(OnWeaponChanged);
    }

    private void Start() {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void OnCrosshairTypeChanged(int index) {
        crosshairType = index;
    }

    private void OnWeaponChanged(Weapon_Firearm weapon) {
        Weapons weaponType = Weapons.None;

        if (weapon == null || (weapon != null && weapon.GetItem().m_itemType != ItemType.Firearm)) {
            m_ammo.gameObject.SetActive(false);
        }
        else {
            if (!m_ammo.gameObject.activeSelf) m_ammo.gameObject.SetActive(true);
            m_ammo.text = $"{weapon.GetCurrentAmmo()}/<size=50%>{weapon.GetStockedAmmo()}</size>";

            weaponType = weapon.GetItem().weaponType;
        }
        
        CheckCrosshair(weaponType);
    }

    private void CheckCrosshair(Weapons weapon) {
        if (weapon == Weapons.None) {
            SelectCrosshair(weapon);
            m_defaultCrosshair.gameObject.SetActive(true);
            return;
        }

        m_defaultCrosshair.gameObject.SetActive(false);
        SelectCrosshair(weapon);
    }

    private void SelectCrosshair(Weapons weapon) {
        for (int i = 0; i < m_crosshairs.Length; i++) {
            for (int c = 0; c < m_crosshairs[i].m_crosshairs.Length; c++) {
                m_crosshairs[i].m_crosshairs[c].SetActive(false);
            }
        }

        for (int i = 0; i < m_crosshairs.Length; i++) {
            if (weapon == m_crosshairs[i].m_weapon) {
                m_crosshairs[i].m_crosshairs[crosshairType].SetActive(true);
                break;
            }
        }
    }

    private void OnAmmoSpent(LongRangeWeapon_SO weapon, int currentAmmo, int maxAmmo, int remainingAmmo) {
        m_ammo.text = $"{currentAmmo}/<size=50%>{maxAmmo}</size>";
    }

    private void OnHit() {
        m_crosshairAnimator.SetTrigger("OnHit");

        /*if (killed) {
            m_hitCrosshair.alpha = 0;
            m_killCrosshair.alpha = 1;
            m_killCrosshair.DOFade(0, 0.55f).SetDelay(0.14f);
        }
        else {
            m_killCrosshair.alpha = 0;
            m_hitCrosshair.alpha = 1;
            m_hitCrosshair.DOFade(0, 0.4f).SetDelay(0.11f);
        }  */      
    }

    private void OnKill() {
         m_crosshairAnimator.SetTrigger("OnKill");

        _killCount++;
        m_killCounter.SetText(_killCount.ToString());
        m_killCounter.transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0.2f), 0.15f);

        m_killIndicator.alpha = 1;
        m_killIndicator.transform.localScale = new Vector3(1.9f, 2.3f, 1.9f);
        m_killIndicator.DOFade(0, 0.75f).SetDelay(1f);
        m_killIndicator.transform.DOScale(Vector3.one, 0.2f);
    }

    private void OnStaminaUsage(float currentStamina, float maxStamina) {
        m_staminaBar.fillAmount = currentStamina / maxStamina;

        if (m_staminaBar.fillAmount >= 1 && !m_staminaFull) {            
            m_staminaFull = true;
            m_staminaBarCanvas.DOKill();
            m_staminaBarCanvas.DOFade(0, 0.8f).SetDelay(1f);
        }
        else if (m_staminaBar.fillAmount < 1 && m_staminaFull) {
            m_staminaFull = false;
            m_staminaBarCanvas.DOKill();
            m_staminaBarCanvas.DOFade(1, 0.8f);
        }
    }

    private void OnDamageTakenScreenVisual() {
        int index = Random.Range(0, m_damageTakenScreenEffect.Length);

        m_damageTakenScreenEffect[index].alpha = 1;
        m_damageTakenScreenEffect[index].DOFade(0, 0.5f).SetDelay(1.7f);
    
        //impulseSource.GenerateImpulse(new Vector3(Random.Range(.1f, .35f), Random.Range(-.15f, .15f), 0f));
    }

    private void OnDamageTaken(float currentHealth, float maxHealth) {
        if (currentHealth < maxHealth)
            OnDamageTakenScreenVisual();

        m_healthBar.fillAmount = currentHealth / maxHealth;

        m_healthBarEffect.DOFillAmount(currentHealth / maxHealth, 0.25f).SetDelay(1f);
        m_healthBarEffect.color = Color.white;
        m_healthBarEffect.DOColor(Color.red, 0.25f);

        m_healthBar.color = Color.green;

        if (m_healthBar.fillAmount <= 0.7)
            m_healthBar.color = Color.yellow;       
        if (m_healthBar.fillAmount <= 0.4)
            m_healthBar.color = new Color32(255, 128, 0, 255);
        if (m_healthBar.fillAmount <= 0.2)
            m_healthBar.color = Color.red;        

        if (m_healthBar.fillAmount <= 0) {
            OnDeath();
        }
    }

    private void OnDeath() {
        m_healthBar.fillAmount = 0;

        _deathCount++;

        m_deathCounter.text = _deathCount.ToString();
    }

    private void OnHealthSet(float actualHealth, float maxHealth) {
        m_healthBar.fillAmount = maxHealth;
        m_healthBar.color = Color.green;
    }

    private void SetSelectedActionText(string text) {
        m_selectedActionIndicator.text = text;
    }
}

[System.Serializable]
public struct CrosshairType {
    public Weapons m_weapon;
    public GameObject[] m_crosshairs;
}
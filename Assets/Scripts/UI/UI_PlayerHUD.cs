using DG.Tweening;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerHUD : MonoBehaviour {
    [Header("UI")]
    [SerializeField] private Transform[] m_slots;
    [SerializeField] private TMP_Text m_selectedActionIndicator;
    [SerializeField] private Image m_slotItem_prefab; //[TODO] Change to   item  
    [SerializeField] private Image m_healthBar;
    [SerializeField] private Image m_staminaBar;
    [SerializeField] private CanvasGroup m_staminaBarCanvas;
    [SerializeField] private TMP_Text m_killCounter;
    [SerializeField] private TMP_Text m_deathCounter;
    [SerializeField] private CanvasGroup m_killIndicator;
    [SerializeField] private TMP_Text m_ammo;
    [SerializeField] private CanvasGroup[] m_damageTakenScreenEffect;
    
    [Header("Crosshair")]
    [SerializeField] private Animator m_crosshairAnimator;

    private CinemachineImpulseSource impulseSource;

    private static Transform[] _slots;
    public static int _slotsCount;

    private Transform _lastSlot;
    private float _slotChangeTime = 0.125f;

    private int _killCount;
    private int _deathCount;
    private bool m_staminaFull;

    private Vector3 _increasedSlotSize => Vector3.one + new Vector3(0.15f, 0.15f, 0.15f);

    private void Awake() {
        Singleton.Instance.GameEvents.OnHoverOverItem.AddListener(SetSelectedActionText);
        Singleton.Instance.GameEvents.OnSlotItemCollected.AddListener(OnSlotItemCollected);
        Singleton.Instance.GameEvents.OnSlotItemDropped.AddListener(OnSlotItemDropped);
        Singleton.Instance.GameEvents.OnSlotSelected.AddListener(OnSlotSelected);
        Singleton.Instance.GameEvents.OnHealthSet.AddListener(OnHealthSet);
        Singleton.Instance.GameEvents.OnDamageTaken.AddListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaUsage.AddListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnHit.AddListener(OnHit);
        Singleton.Instance.GameEvents.OnKill.AddListener(OnKill);
        Singleton.Instance.GameEvents.OnAmmoConsumed.AddListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnWeaponChanged.AddListener(OnWeaponChanged);

        _slots = m_slots;
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnHoverOverItem.RemoveListener(SetSelectedActionText);
        Singleton.Instance.GameEvents.OnSlotItemCollected.RemoveListener(OnSlotItemCollected);
        Singleton.Instance.GameEvents.OnSlotItemDropped.RemoveListener(OnSlotItemDropped);
        Singleton.Instance.GameEvents.OnSlotSelected.RemoveListener(OnSlotSelected);
        Singleton.Instance.GameEvents.OnHealthSet.RemoveListener(OnHealthSet);
        Singleton.Instance.GameEvents.OnDamageTaken.RemoveListener(OnDamageTaken);
        Singleton.Instance.GameEvents.OnStaminaUsage.RemoveListener(OnStaminaUsage);
        Singleton.Instance.GameEvents.OnHit.RemoveListener(OnHit);
        Singleton.Instance.GameEvents.OnKill.RemoveListener(OnKill);
        Singleton.Instance.GameEvents.OnAmmoConsumed.RemoveListener(OnAmmoSpent);
        Singleton.Instance.GameEvents.OnWeaponChanged.RemoveListener(OnWeaponChanged);

        _slots = null;
    }

    private void Start() {
        impulseSource = GetComponent<CinemachineImpulseSource>();

        _lastSlot = m_slots[0]; //[TODO] handle on load, see which slot the player left the game selected
        OnSlotSelected(0);

        Singleton.Instance.GameEvents.OnWeaponChanged?.Invoke(null); //[TODO] Change after save system
    }

    public static int GetSlotsCount() => _slots.Length;

    private void OnWeaponChanged(Weapon_Firearm weapon) {
        if (weapon == null || (weapon != null && weapon.GetItem().m_itemType != ItemType.Firearm)) {
            m_ammo.gameObject.SetActive(false);
            return;
        }

        if (!m_ammo.gameObject.activeSelf) m_ammo.gameObject.SetActive(true);
        m_ammo.text = $"{weapon.GetCurrentAmmo()}/<size=50%>{weapon.GetStockedAmmo()}</size>";
    }

    private void OnAmmoSpent(string id, int currentAmmo, int maxAmmo) {
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

    private void OnHealthSet(int actualHealth, int maxHealth) {
        //this.maxHealth = maxHealth;
        m_healthBar.fillAmount = maxHealth;

        m_healthBar.color = Color.green;
    }

    private void OnSlotSelected(int index) {
        _lastSlot.DOScale(Vector3.one, _slotChangeTime);
        m_slots[index].DOScale(_increasedSlotSize, _slotChangeTime);

        _lastSlot = m_slots[index];
    }

    private void OnSlotItemCollected(Item_SO item, int index) {
        Image slot = Instantiate(m_slotItem_prefab, m_slots[index]);
        slot.sprite = item.m_icon;
    }

    private void OnSlotItemDropped(int index) {
        Destroy(m_slots[index].GetChild(0).gameObject);
    }

    private void SetSelectedActionText(string text) {
        m_selectedActionIndicator.text = text;
    }
}

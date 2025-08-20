using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour, IPointerExitHandler {
    [Header("UI")]
    [SerializeField] private TMP_Text txt_title;
    [SerializeField] private Image img_icon;
    [SerializeField] private TMP_Text txt_description;
    [SerializeField] private TMP_Text txt_ownedAmount;
    [SerializeField] private TMP_Text txt_itemType;
    [SerializeField] private Button btn_split;

    [Header("Setup")]
    [SerializeField] private float m_multiplier = 0.0275f;

    #region Private
    private RectTransform rt;
    private CanvasGroup cg;

    private int quantity;

    private bool isReloading;
    //private bool m_hovering;
    //private float m_smoothness = 10f;
    private Color m_color = Color.white;
    private string m_typeText;

    private float m_mp;
    //private Vector2 pos;
    private float pivotX;
    private float pivotY;
    #endregion

    private void Awake() {
        rt = GetComponent<RectTransform>();
        cg = GetComponent<CanvasGroup>();

        cg.alpha = 0.0f;
        transform.localScale = Vector3.zero;

        Singleton.Instance.GameEvents.OnWeaponReload.AddListener(OnWeaponReload);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnWeaponReload.RemoveListener(OnWeaponReload);
    }

    private void OnWeaponReload(bool isReloading, Weapons weapons) => this.isReloading = isReloading;

    public void SetInventoryTooltip(Item_SO item, ItemData itemData, int quantity) {
        PopUpTooltip();

        switch (item.m_itemType) {
            case ItemType.MeleeWeapon:
                m_color = Color.blue;
                m_typeText = "Weapon";
                break;
            case ItemType.Firearm:
                m_color = Color.blue;
                m_typeText = "Weapon";
                break;
            case ItemType.PuzzlePiece:
                m_color = Color.red;
                m_typeText = "Puzzle piece";
                break;
            case ItemType.Collectible:
                m_color = Color.yellow;
                m_typeText = "Collectible";
                break;
            case ItemType.Ammo:
                m_color = Color.white;
                m_typeText = "Ammo";
                break;
        }

        this.quantity = quantity;

        txt_title.text = item.m_itemName;
        txt_description.text = item.m_description;
        img_icon.sprite = item.m_icon;
        txt_ownedAmount.text = quantity > 1 ? "Amount: " + quantity.ToString() : "";
        txt_itemType.text = m_typeText;
        txt_itemType.color = m_color;

        btn_split.gameObject.SetActive(quantity > 1);

        btn_split.onClick.RemoveAllListeners();
        btn_split.onClick.AddListener(() => OnItemSplit(itemData));
    }

    private void PopUpTooltip() {
        cg.interactable = true;

        Vector2 pos = Input.mousePosition;
        transform.position = pos;

        //m_hovering = true;
        cg.DOKill();
        transform.DOKill();

        cg.DOFade(1, 0.45f);
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.InOutCubic);

        pos = Input.mousePosition;
        pivotY = pos.y / Screen.height;
        pivotX = pos.x / Screen.width;

        if (pivotX <= 0.5f) m_mp = -m_multiplier;
        else m_mp = m_multiplier;
        pivotX = Mathf.RoundToInt(pivotX);

        rt.pivot = new Vector2(pivotX + m_mp, pivotY);
        transform.position = pos;
    }

    public void OnTooltipMouseExit() {
        cg.interactable = false;
        cg.DOKill();
        transform.DOKill();

        transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InOutCirc);
        cg.DOFade(0, 0.45f).OnComplete(() => {
            gameObject.SetActive(false);
            //m_hovering = false;
        });        
    }

    private void OnItemSplit(ItemData item) {
        if (isReloading) return;

        if (InventoryManager.IsInventoryFull()) {
            Debug.Log("<color=red>Inventory is full</color>");
            return;
        }

        OnTooltipMouseExit();
        Singleton.Instance.GameEvents.OnItemSplit?.Invoke(item, quantity);
    }

    public void OnPointerExit(PointerEventData eventData) => OnTooltipMouseExit();

    /*private void LateUpdate() {
        if (!m_hovering) return;

        pos = Input.mousePosition;
        pivotY = pos.y / Screen.height;
        pivotX = pos.x / Screen.width;

        if (pivotX <= 0.5f) m_mp = -m_multiplier;
        else m_mp = m_multiplier;
        pivotX = Mathf.RoundToInt(pivotX);

        rt.pivot = Vector2.Lerp(rt.pivot, new Vector2(pivotX + m_mp, pivotY), Time.deltaTime * m_smoothness);
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * m_smoothness);
    }*/
}

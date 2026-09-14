using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour {
    [Header("UI")]
    [SerializeField] private TMP_Text txt_title;
    [SerializeField] private TMP_Text txt_description;
    [SerializeField] private TMP_Text txt_itemType;
    [SerializeField] private Image img_rarityBackground;
    [SerializeField] private GameObject m_rarityStarsHolder;
    [SerializeField] private Image[] m_rarities;

    [Header("Setup")]
    [SerializeField] private float m_multiplier = 0.0275f;

    private Color defaultStarColor = new Color(1f, .85f, 0f, 1f);
    private Color specialStarColor = new Color(1f, 0f, 0f, 1f);

    #region Private
    private CanvasGroup cg;
    private string typeText;
    #endregion

    private void Awake() {
        cg = GetComponent<CanvasGroup>();

        cg.alpha = 0.0f;
        cg.interactable = false;
        cg.blocksRaycasts = true;
    }

    public void SetInventoryTooltip(Item_SO item) {
        Singleton.Instance.GameEvents.OnItemShowcaseSet?.Invoke(item);
        
        cg.DOKill();
        cg.DOFade(1, 0.45f);

        typeText = item.m_itemType switch {
            ItemType.Firearm => "Weapon",
            ItemType.MeleeWeapon => "Weapon",
            ItemType.PuzzlePiece => "Puzzle piece",
            ItemType.Collectible => "Collectible",
            ItemType.Ammo => "Ammo",
            _ => string.Empty
        };

        bool hasPublicRarity = item.m_itemType == ItemType.MeleeWeapon || item.m_itemType == ItemType.Firearm;
        int rarityLevel = (int)item.m_itemRarity + 1;
        Color color = Singleton.Instance.GameManager.GetRarityColor(item.m_itemRarity);

        txt_title.text = item.m_itemName;
        txt_description.text = item.m_description;
        txt_itemType.text = typeText;
        img_rarityBackground.color = color;

        m_rarityStarsHolder.SetActive(hasPublicRarity);
        for (int i = 0; i < m_rarities.Length; i++) {
            m_rarities[i].color = rarityLevel == m_rarities.Length ? specialStarColor : defaultStarColor;
            m_rarities[i].gameObject.SetActive(i < rarityLevel);
        }

        bool isMissingItemType = string.IsNullOrEmpty(typeText);

        txt_itemType.gameObject.SetActive(!isMissingItemType);
        img_rarityBackground.gameObject.SetActive(!isMissingItemType);
    }

    public void OnHideTooltip(bool immediate) {
        if (immediate) {
            cg.alpha = 0f;
            Singleton.Instance.GameEvents.OnItemShowcaseUnset?.Invoke();
            return;
        }

        cg.DOKill();
        cg.DOFade(0, 0.45f).OnComplete(() => {
            Singleton.Instance.GameEvents.OnItemShowcaseUnset?.Invoke();
        });      
    }
}

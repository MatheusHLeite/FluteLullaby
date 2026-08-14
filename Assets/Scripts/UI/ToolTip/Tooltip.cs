using DG.Tweening;
using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour {
    [Header("UI")]
    [SerializeField] private TMP_Text txt_title;
    [SerializeField] private TMP_Text txt_description;
    [SerializeField] private TMP_Text txt_itemType;

    [Header("Setup")]
    [SerializeField] private float m_multiplier = 0.0275f;

    #region Private
    private CanvasGroup cg;

    private Color color = Color.white;
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

        switch (item.m_itemType) {
            case ItemType.MeleeWeapon:
            case ItemType.Firearm:
                color = Color.red;
                typeText = "Weapon";
                break;
            case ItemType.PuzzlePiece:
                color = Color.magenta;
                typeText = "Puzzle piece";
                break;
            case ItemType.Collectible:
                color = Color.yellow;
                typeText = "Collectible";
                break;
            case ItemType.Ammo:
                color = Color.white;
                typeText = "Ammo";
                break;
        }

        txt_title.text = item.m_itemName;
        txt_description.text = item.m_description;
        txt_itemType.text = typeText;
        txt_itemType.color = color;
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

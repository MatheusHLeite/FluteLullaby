using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class RadialMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler {
    [Header("Setup")]
    [SerializeField] private TMP_Text txt_choice;
    [SerializeField] private Image img_background;

    private CanvasGroup thisCg;
    private ImportantDecision thisDecision;

    public static bool Selected;

    private void Awake() {
        Singleton.Instance.GameEvents.OnImportantDecisionTaken.AddListener(OnImportantDecisionTaken);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnImportantDecisionTaken.RemoveListener(OnImportantDecisionTaken);
    }

    private void OnImportantDecisionTaken(ImportantDecision decision) {
        if (decision.id == thisDecision.id) {
            thisCg.DOFade(0, 1.5f);
            return;
        }

        thisCg.DOFade(0, 0.9f);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (Selected) return;
        img_background.DOFade(0.15f, 0.2f);
        img_background.transform.DOScale(Vector3.one * 1.25f, 0.35f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (Selected) return;
        img_background.DOFade(0.04f, 0.2f);
        img_background.transform.DOScale(Vector3.one, 0.1f);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (Selected) return;
        img_background.transform.DOScale(Vector3.one * 3f, 2f).SetEase(Ease.InBounce);
        img_background.color = Color.green;

        Singleton.Instance.GameEvents.OnImportantDecisionTaken?.Invoke(thisDecision);

        Selected = true;
    }

    private void UpdateUI() {
        txt_choice.transform.rotation = Quaternion.identity;
    }

    public void SetupUI(ImportantDecision decision) {
        thisCg = GetComponent<CanvasGroup>();

        txt_choice.text = decision.optionText;
        thisDecision = decision;
    }

#if UNITY_EDITOR
    void LateUpdate() {
        UpdateUI();
    }
#endif
}

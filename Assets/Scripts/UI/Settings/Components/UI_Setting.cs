using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Setting : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Settings")]
    [SerializeField] private TMP_Text m_selectedOption;
    [SerializeField] private Button m_selectNext;
    [SerializeField] private Button m_selectPrevious;

    [SerializeField] private Image[] m_slotSelectionIndicator;
    [SerializeField] private CanvasGroup thisCanvasGroup;

    private Color inativeIndexColor = new Color(.5f, .5f, .5f);

    private List<UIOption> options = new List<UIOption>();
    private int actualIndex;

    [SerializeField] private UnityEvent<int> m_OnValueChanged = new UnityEvent<int>();
    public UnityEvent<int> onValueChanged { get { return m_OnValueChanged; } set { m_OnValueChanged = value; } }

    private void Awake() {
        m_selectNext.onClick.AddListener(SelectNextOption);
        m_selectPrevious.onClick.AddListener(SelectPreviousOption);

        thisCanvasGroup.alpha = 0;
    }

    private void OnDestroy() {
        m_selectNext.onClick.RemoveAllListeners();
        m_selectPrevious.onClick.RemoveAllListeners();
    }

    public void SetupOptions(List<UIOption> newOptions, int index) {
        options = newOptions;
        actualIndex = index;

        if (options.Count <= 0) {
            m_selectedOption.SetText("Error");
            m_selectedOption.color = Color.red;

            m_selectNext.gameObject.SetActive(false);
            m_selectPrevious.gameObject.SetActive(false);
            return;
        }

        if (options.Count <= 1) {
            m_selectedOption.SetText(actualIndex == -1 ? "Custom" : options[actualIndex].text);
            m_selectedOption.color = inativeIndexColor;

            m_selectNext.gameObject.SetActive(false);
            m_selectPrevious.gameObject.SetActive(false);
            return; 
        }

        SetupIndicator();
    }

    public void SetInactive() {
        m_selectedOption.color = inativeIndexColor;

        m_selectNext.gameObject.SetActive(false);
        m_selectPrevious.gameObject.SetActive(false);
    }

    private void SetupIndicator() {
        m_selectedOption.SetText(actualIndex == -1 ? "Custom" : options[actualIndex].text);

        if (actualIndex == -1) {          
            for (int i = 0; i < m_slotSelectionIndicator.Length; i++)            
                m_slotSelectionIndicator[i].gameObject.SetActive(false);            
            return;
        }

        if (options.Count <= m_slotSelectionIndicator.Length) {
            for (int i = 0; i < options.Count; i++) {
                m_slotSelectionIndicator[i].gameObject.SetActive(true);
                m_slotSelectionIndicator[i].color = inativeIndexColor;
            }

            m_slotSelectionIndicator[actualIndex].color = Color.white;
        }
    }

    public void SetSpecificIndex(int index) {
        actualIndex = index;

        OnSelect();
    }

    public void UpdateIndexWithoutNotify(int index) {
        actualIndex = index;
        SetupIndicator();
    }

    private void SelectNextOption() {
        actualIndex++;
        if (actualIndex > options.Count - 1) actualIndex = 0;

        OnSelect();
    }

    private void SelectPreviousOption() {
        actualIndex--;
        if (actualIndex < 0) actualIndex = options.Count - 1;

        OnSelect();
    }

    private void OnSelect(bool shouldApply = true) {
        SetupIndicator();
        if (shouldApply) m_OnValueChanged?.Invoke(actualIndex);
    }

    public int GetSettingsIndex() => actualIndex;

    public void OnPointerEnter(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(1, 0.25f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(0, 0.25f);
    }
}

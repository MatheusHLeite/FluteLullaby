using DG.Tweening;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_SliderSetting : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Setup")]
    [SerializeField] protected string m_stringComplement = "";
    [SerializeField] protected SliderType m_type;
    [SerializeField] private CanvasGroup thisCanvasGroup;

    protected Slider slider;
    protected TMP_InputField valueTxt;

    private int minValue;
    private int maxValue;

    private float defaultValue;

    protected UnityAction<float> action;
    protected bool valueIsMultiplied;

    private void Awake() {
        thisCanvasGroup.alpha = 0;
    }

    public void Setup(Vector2 minMaxValues, float value, string txtValue = null, bool wholeNumbers = true) {
        slider = GetComponentInChildren<Slider>();
        valueTxt = GetComponentInChildren<TMP_InputField>();

        string stringValue = !string.IsNullOrEmpty(txtValue) ? txtValue : value.ToString();

        slider.wholeNumbers = wholeNumbers;

        minValue = (int)minMaxValues.x;
        maxValue = (int)minMaxValues.y;

        slider.minValue = minValue;
        slider.maxValue = maxValue;

        slider.SetValueWithoutNotify(value);
        valueTxt.SetTextWithoutNotify($"{stringValue} {m_stringComplement}");

        defaultValue = value;
    }

    public virtual void AddListener(UnityAction<float> onValueChange) {
        action = onValueChange;

        valueTxt.onEndEdit.AddListener(i => OnTextValueChanged(i));
    }

    public void RemoveAllListeners() {
        slider.onValueChanged.RemoveAllListeners();
        valueTxt.onEndEdit.RemoveAllListeners();
    }

    private void OnTextValueChanged(string text) {
        if (Regex.IsMatch(text, "[a-zA-Z]")) {
            action?.Invoke(defaultValue);
            return; 
        }

        string textCorrected = text.Replace(" ", "");
        if (!string.IsNullOrEmpty(m_stringComplement)) textCorrected = textCorrected.Replace(m_stringComplement, "");

        float value = float.Parse(textCorrected);
        string finalText = $"{value} {m_stringComplement}";

        switch (m_type) {
            case SliderType.Volume:
                if (value < minValue) value = minValue;
                if (value > maxValue) value = maxValue;

                value /= 100f;
                break;
            case SliderType.Gamma:
                if (value < minValue * 100) value = minValue;
                if (value > maxValue * 100) value = maxValue;
     
                if (value <= 100f) {
                    value = (value - 10f) / (100f - 10f) * 0.5f;
                }
                else {
                    value = 0.5f + (value - 100f) / (150f - 100f) * 0.5f;
                }
                value = Mathf.Clamp01(value);
                break;
            case SliderType.FPS:
                if (value < minValue) value = minValue;
                if (value > maxValue) value = maxValue;

                if (value == maxValue) finalText = "Unlimited";
                break;
        }

        slider.SetValueWithoutNotify(value);
        valueTxt.SetTextWithoutNotify(finalText);
        defaultValue = value;

        action?.Invoke(value);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(1, 0.25f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(0, 0.25f);
    }
}

public enum SliderType { Volume, Gamma, FPS }
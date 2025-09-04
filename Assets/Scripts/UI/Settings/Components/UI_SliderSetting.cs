using System.Text.RegularExpressions;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SliderSetting : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("Setup")]
    [SerializeField] protected string m_stringComplement = "";
    [SerializeField] protected SliderType m_type;

    [Header("UI")]
    [SerializeField] protected Slider slider;
    [SerializeField] protected TMP_InputField valueTxt;
    [SerializeField] private CanvasGroup thisCanvasGroup;

    private int minValue;
    private int maxValue;

    private float defaultValue;
    private bool hasText;

    protected UnityAction<float> action;
    protected bool valueIsMultiplied;

    private void Awake() {
        thisCanvasGroup.alpha = 0;
    }

    public Slider GetSlider() => slider;

    public TMP_InputField GetInputField() => valueTxt;

    public void Setup(Vector2 minMaxValues, float value, string txtValue = null, bool wholeNumbers = true, bool hasText = true) {
        string stringValue = !string.IsNullOrEmpty(txtValue) ? txtValue : value.ToString("0.00");

        slider.wholeNumbers = wholeNumbers;
        this.hasText = hasText;

        minValue = (int)minMaxValues.x;
        maxValue = (int)minMaxValues.y;

        slider.minValue = minValue;
        slider.maxValue = maxValue;

        slider.SetValueWithoutNotify(value);

        defaultValue = value;

        if (hasText) {
            if (m_type == SliderType.Volume) 
                stringValue = Mathf.RoundToInt(slider.value * 100).ToString();

            valueTxt.SetTextWithoutNotify($"{stringValue} {m_stringComplement}");
            return;
        }

        valueTxt.gameObject.SetActive(false);
    }

    public virtual void AddListener(UnityAction<float> onValueChange) {
        action = onValueChange;

        if (hasText) valueTxt.onEndEdit.AddListener(i => OnTextValueChanged(i));
        slider.onValueChanged.AddListener(OnSliderChange); 
    }

    public void RemoveAllListeners() {
        slider.onValueChanged.RemoveAllListeners();
        if (hasText) valueTxt.onEndEdit.RemoveAllListeners();
    }

    private void OnSliderChange(float value) {
        switch (m_type) {
            case SliderType.Default:
                action?.Invoke(value);
                valueTxt.text = $"{value.ToString("0.00")} {m_stringComplement}";
                break;
            case SliderType.Volume:
                float result = Mathf.RoundToInt(value * 100);

                action?.Invoke(value);
                valueTxt.text = $"{(int)result} {m_stringComplement}";
                break;
            case SliderType.FPS:
                valueTxt.text = value <= Singleton.Instance.SettingsManager.minMaxFPS.y ? value.ToString() : "Unlimited";
                break;
        }
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
                value /= 100f;

                if (value < minValue) value = minValue;
                if (value > maxValue) value = maxValue;
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

    public void OnPointerUp() { if (m_type == SliderType.FPS) action?.Invoke(slider.value); }
}

public enum SliderType { Default, Volume, FPS }
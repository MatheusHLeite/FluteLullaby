using UnityEngine;
using UnityEngine.Events;

public class UI_GammaSlider : UI_SliderSetting {
    public override void AddListener(UnityAction<float> onValueChange) {
        base.AddListener(onValueChange);
        slider.onValueChanged.AddListener(OnSliderChange);

        valueTxt.text = $"{(int)GetPercentageText(slider.value)} {m_stringComplement}";

        m_type = SliderType.Gamma;
    }

    private void OnSliderChange(float value) {
        action?.Invoke(value);

        valueTxt.text = $"{(int)GetPercentageText(value)} {m_stringComplement}";
    }

    private float GetPercentageText(float sliderValue) => sliderValue <= 0.5f ? Mathf.Lerp(10f, 100f, sliderValue / 0.5f) : 
        Mathf.Lerp(100f, 150f, (sliderValue - 0.5f) / 0.5f);
}

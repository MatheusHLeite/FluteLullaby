using UnityEngine;
using UnityEngine.Events;

public class UI_VolumeSlider : UI_SliderSetting {
    public override void AddListener(UnityAction<float> onValueChange) {
        base.AddListener(onValueChange);
        slider.onValueChanged.AddListener(OnSliderChange);

        float result = Mathf.RoundToInt(slider.value * 100);
        valueTxt.text = $"{(int)result} {m_stringComplement}";

        m_type = SliderType.Volume;
    }

    private void OnSliderChange(float value) {      
        float result = Mathf.RoundToInt(value * 100);

        action?.Invoke(value);
        valueTxt.text = $"{(int)result} {m_stringComplement}";
    }
}

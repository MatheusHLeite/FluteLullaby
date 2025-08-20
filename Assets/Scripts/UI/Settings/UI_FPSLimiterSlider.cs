using UnityEngine.Events;

public class UI_FPSLimiterSlider : UI_SliderSetting {
    public override void AddListener(UnityAction<float> onValueChange) {
        base.AddListener(onValueChange);
        slider.onValueChanged.AddListener(OnFPSTextUpdate);

        m_type = SliderType.FPS;
    }

    private void OnFPSTextUpdate(float value) {
        valueTxt.text = value <= Singleton.Instance.SettingsManager.minMaxFPS.y ? value.ToString() : "Unlimited";
    }

    public void OnPointerUp() => action?.Invoke(slider.value);
}

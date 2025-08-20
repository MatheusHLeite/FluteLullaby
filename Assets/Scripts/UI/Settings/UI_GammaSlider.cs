using UnityEngine.Events;

public class UI_GammaSlider : UI_SliderSetting {
    public override void AddListener(UnityAction<float> onValueChange) {
        base.AddListener(onValueChange);
        slider.onValueChanged.AddListener(action);

        m_type = SliderType.Gamma;
    }
}

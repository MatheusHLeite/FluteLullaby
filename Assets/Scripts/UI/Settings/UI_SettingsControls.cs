using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsControls : MonoBehaviour {
    public TMP_Text txtValue;
    public Slider sensSlider;

    private void Awake() {
        sensSlider.onValueChanged.AddListener(OnSensitivityChange);
        Singleton.Instance.GameEvents.OnDataLoaded.AddListener(OnSettingsDataLoaded);
    }

    private void OnDestroy() {
        sensSlider.onValueChanged.RemoveListener(OnSensitivityChange);
        Singleton.Instance.GameEvents.OnDataLoaded.RemoveListener(OnSettingsDataLoaded);
    }

    private void Start() {
        sensSlider.minValue = 0.01f;
        sensSlider.maxValue = 18f;

        txtValue.text = 2.ToString();
    }

    private void OnSettingsDataLoaded(PlayerSaveData data) {

    }

    private void OnSensitivityChange(float sensitivity) {
        txtValue.text = sensitivity.ToString("0.00");
        Singleton.Instance.GameEvents.OnSensitivityChange?.Invoke(sensitivity);
    }
}

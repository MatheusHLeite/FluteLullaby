using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CrosshairCustomizer : MonoBehaviour {
    [Header("Elements")]
    [SerializeField] private Image[] m_lines;
    [SerializeField] private Image m_dot;
    [SerializeField] private Button btn_reset;

    [Header("UI")]
    [SerializeField] private CustomSlider s_lineLength;
    [SerializeField] private CustomSlider s_spacing;
    [SerializeField] private CustomSlider s_thickness;
    [SerializeField] private CustomSlider s_dotSize;
    [SerializeField] private CustomSlider s_redColor;
    [SerializeField] private CustomSlider s_greenColor;
    [SerializeField] private CustomSlider s_blueColor;
    [SerializeField] private CustomSlider s_alpha;

    private float m_lineLength = 9f;
    private float m_spacing = 3.5f;
    private float m_thickness = 3f;
    private float m_dotSize = 0f;
    private float m_redColor = 0;
    private float m_greenColor = 1;
    private float m_blueColor = 0;
    private float m_alpha = 1;
    private Color m_crosshairColor = Color.green;

    #region Initialization
    private void Awake() {
        s_lineLength.slider.onValueChanged.AddListener(OnLineLengthChanged);
        s_spacing.slider.onValueChanged.AddListener(OnLineSpacingChanged);
        s_thickness.slider.onValueChanged.AddListener(OnLineThicknessChanged);
        s_dotSize.slider.onValueChanged.AddListener(OnDotSizeChanged);
        s_redColor.slider.onValueChanged.AddListener(OnRedColorChanged);
        s_greenColor.slider.onValueChanged.AddListener(OnGreenColorChanged);
        s_blueColor.slider.onValueChanged.AddListener(OnBlueColorChanged);
        s_alpha.slider.onValueChanged.AddListener(OnAlphaChanged);
        btn_reset.onClick.AddListener(OnResetToDefault);
    }

    private void OnDestroy() {
        s_lineLength.slider.onValueChanged.RemoveListener(OnLineLengthChanged);
        s_spacing.slider.onValueChanged.RemoveListener(OnLineSpacingChanged);
        s_thickness.slider.onValueChanged.RemoveListener(OnLineThicknessChanged);
        s_dotSize.slider.onValueChanged.RemoveListener(OnDotSizeChanged);
        s_redColor.slider.onValueChanged.RemoveListener(OnRedColorChanged);
        s_greenColor.slider.onValueChanged.RemoveListener(OnGreenColorChanged);
        s_blueColor.slider.onValueChanged.RemoveListener(OnBlueColorChanged);
        s_alpha.slider.onValueChanged.RemoveListener(OnAlphaChanged);
        btn_reset.onClick.RemoveListener(OnResetToDefault);
    }

    private void Start() {
        SetupUI();
    }

    private void SetupUI() {
        LoadCrosshairSettings();

        SetupSlider(s_lineLength);
        SetupSlider(s_spacing);
        SetupSlider(s_thickness);
        SetupSlider(s_dotSize);
        SetupSlider(s_redColor);
        SetupSlider(s_greenColor);
        SetupSlider(s_blueColor);
        SetupSlider(s_alpha);

        s_lineLength.slider.SetValueWithoutNotify(m_lineLength);
        s_spacing.slider.SetValueWithoutNotify(m_spacing);
        s_thickness.slider.SetValueWithoutNotify(m_thickness);
        s_dotSize.slider.SetValueWithoutNotify(m_dotSize);
        s_redColor.slider.SetValueWithoutNotify(m_redColor);
        s_greenColor.slider.SetValueWithoutNotify(m_greenColor);
        s_blueColor.slider.SetValueWithoutNotify(m_blueColor);
        s_alpha.slider.SetValueWithoutNotify(m_alpha);

        ChangeCrosshairSpacing();
        ChangeCrosshairThickness();
        SetCrosshairColor();
        ChangeDotSize();
    }

    private void SetupSlider(CustomSlider slider) {
        slider.slider.minValue = slider.minValue;
        slider.slider.maxValue = slider.maxValue;
    }
    #endregion

    #region Events
    private void OnLineLengthChanged(float value) {
        m_lineLength = value;        
        ChangeCrosshairSpacing();
        ChangeCrosshairThickness();
    }

    private void OnLineSpacingChanged(float value) {
        m_spacing = value;        
        ChangeCrosshairSpacing();
    }

    private void OnLineThicknessChanged(float value) {
        m_thickness = value;        
        ChangeCrosshairThickness();
    }

    private void OnRedColorChanged(float color) {
        m_redColor = color;        
        SetCrosshairColor();
    }

    private void OnGreenColorChanged(float color) {
        m_greenColor = color;        
        SetCrosshairColor();
    }

    private void OnBlueColorChanged(float color) {
        m_blueColor = color;        
        SetCrosshairColor();
    }

    private void OnAlphaChanged(float alpha) {
        m_alpha = alpha;        
        SetCrosshairColor();
    }

    private void OnDotSizeChanged(float value) {
        m_dotSize = value;        
        ChangeDotSize();
    }

    private void OnResetToDefault() {
        m_lineLength = 9f;
        m_spacing = 3.5f;
        m_thickness = 3f;
        m_dotSize = 0f;
        m_redColor = 0f;
        m_greenColor = 1f;
        m_blueColor = 0f;
        m_alpha = 1f;
        m_crosshairColor = Color.green;
 
        ChangeCrosshairSpacing();
        ChangeCrosshairThickness();
        SetCrosshairColor();
        ChangeDotSize();

        s_lineLength.slider.SetValueWithoutNotify(m_lineLength);
        s_spacing.slider.SetValueWithoutNotify(m_spacing);
        s_thickness.slider.SetValueWithoutNotify(m_thickness);
        s_dotSize.slider.SetValueWithoutNotify(m_dotSize);
        s_redColor.slider.SetValueWithoutNotify(m_redColor);
        s_greenColor.slider.SetValueWithoutNotify(m_greenColor);
        s_blueColor.slider.SetValueWithoutNotify(m_blueColor);
        s_alpha.slider.SetValueWithoutNotify(m_alpha);
    }
    #endregion

    #region Set crosshair values
    private void ChangeCrosshairSpacing() {
        s_spacing.txtValue.text = m_spacing.ToString("0.00");
        s_lineLength.txtValue.text = m_lineLength.ToString("0.00");

        m_lines[0].rectTransform.anchoredPosition = new Vector2(0, m_spacing + m_lineLength / 2);
        m_lines[1].rectTransform.anchoredPosition = new Vector2(0, -m_spacing - m_lineLength / 2);
        m_lines[2].rectTransform.anchoredPosition = new Vector2(-m_spacing - m_lineLength / 2, 0);
        m_lines[3].rectTransform.anchoredPosition = new Vector2(m_spacing + m_lineLength / 2, 0);       
    }

    private void ChangeCrosshairThickness() {
        s_thickness.txtValue.text = m_thickness.ToString("0.00");
        foreach (var line in m_lines) {
            line.rectTransform.sizeDelta = new Vector2(m_thickness, m_lineLength);
        }
    }

    private void SetCrosshairColor() {
        m_crosshairColor = new Color(m_redColor, m_greenColor, m_blueColor, m_alpha);

        s_redColor.txtValue.text = m_redColor.ToString("0.00");
        s_greenColor.txtValue.text = m_greenColor.ToString("0.00");
        s_blueColor.txtValue.text = m_blueColor.ToString("0.00");
        s_alpha.txtValue.text = m_alpha.ToString("0.00");

        m_dot.color = m_crosshairColor;
        foreach (var line in m_lines) line.color = m_crosshairColor;
    }

    private void ChangeDotSize() {
        s_dotSize.txtValue.text = m_dotSize.ToString("0.00");
        m_dot.rectTransform.sizeDelta = new Vector2(m_dotSize, m_dotSize);
    }
    #endregion

    #region Load and Save
    public void SaveCrosshairSettings() {
        PlayerPrefs.SetFloat("Customization_CrosshairLength", m_lineLength);
        PlayerPrefs.SetFloat("Customization_CrosshairThickness", m_thickness);
        PlayerPrefs.SetFloat("Customization_CrosshairSpacing", m_spacing);
        PlayerPrefs.SetFloat("Customization_CrosshairRedColor", m_redColor);
        PlayerPrefs.SetFloat("Customization_CrosshairGreenColor", m_greenColor);
        PlayerPrefs.SetFloat("Customization_CrosshairBlueColor", m_blueColor);
        PlayerPrefs.SetFloat("Customization_CrosshairAlpha", m_alpha);
        PlayerPrefs.SetFloat("Customization_DotSize", m_dotSize);
    }

    private void LoadCrosshairSettings() {
        m_lineLength = PlayerPrefs.GetFloat("Customization_CrosshairLength", 9f);
        m_thickness = PlayerPrefs.GetFloat("Customization_CrosshairThickness", 3f);
        m_spacing = PlayerPrefs.GetFloat("Customization_CrosshairSpacing", 3.5f);
        m_redColor = PlayerPrefs.GetFloat("Customization_CrosshairRedColor", 0f);
        m_greenColor = PlayerPrefs.GetFloat("Customization_CrosshairGreenColor", 1f);
        m_blueColor = PlayerPrefs.GetFloat("Customization_CrosshairBlueColor", 0f);
        m_alpha = PlayerPrefs.GetFloat("Customization_CrosshairAlpha", 1f);
        m_crosshairColor = new Color(m_redColor, m_greenColor, m_blueColor, m_alpha);
        m_dotSize = PlayerPrefs.GetFloat("Customization_DotSize", 0f);
    }
    #endregion
}

[Serializable]
public struct CustomSlider {
    public Slider slider;
    public float minValue;
    public float maxValue;
    public TMP_Text txtValue;
}

public struct CrosshairCustomization {

}
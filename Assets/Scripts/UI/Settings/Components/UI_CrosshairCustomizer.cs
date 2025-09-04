using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CrosshairCustomizer : MonoBehaviour {
    [Header("UI")]
    [SerializeField] private CustomSlider s_lineLength;
    [SerializeField] private CustomSlider s_spacing;
    [SerializeField] private CustomSlider s_thickness;
    [SerializeField] private CustomSlider s_dotSize;

    [SerializeField] private CustomSlider s_redColor;
    [SerializeField] private CustomSlider s_greenColor;
    [SerializeField] private CustomSlider s_blueColor;
    [SerializeField] private CustomSlider s_alpha;

    [Header("Values")]
    [SerializeField] private Vector2 m_crosshairMinMaxSettings;
    [SerializeField] private Vector2 m_colorMinMaxSettings;

    [Header("Crosshair")]
    [SerializeField] private Image[] m_lines;
    [SerializeField] private Image m_dot;

    [Header("Preview Crosshair")]
    [SerializeField] private Image[] m_previewLines;
    [SerializeField] private Image m_previewDot;

    [Header("Elements")]
    [SerializeField] private Button btn_reset;

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
    private void AddListeners() {
        s_lineLength.slider.onValueChanged.AddListener(OnLineLengthChanged);
        s_lineLength.txtValue.onEndEdit.AddListener(s => OnLineLengthChanged(float.Parse(s)));
        s_spacing.slider.onValueChanged.AddListener(OnLineSpacingChanged);
        s_spacing.txtValue.onEndEdit.AddListener(s => OnLineSpacingChanged(float.Parse(s)));
        s_thickness.slider.onValueChanged.AddListener(OnLineThicknessChanged);
        s_thickness.txtValue.onEndEdit.AddListener(s => OnLineThicknessChanged(float.Parse(s)));
        s_dotSize.slider.onValueChanged.AddListener(OnDotSizeChanged);
        s_dotSize.txtValue.onEndEdit.AddListener(s => OnDotSizeChanged(float.Parse(s)));
        s_redColor.slider.onValueChanged.AddListener(OnRedColorChanged);
        s_redColor.txtValue.onEndEdit.AddListener(s => OnRedColorChanged(float.Parse(s)));
        s_greenColor.slider.onValueChanged.AddListener(OnGreenColorChanged);
        s_greenColor.txtValue.onEndEdit.AddListener(s => OnGreenColorChanged(float.Parse(s)));
        s_blueColor.slider.onValueChanged.AddListener(OnBlueColorChanged);
        s_blueColor.txtValue.onEndEdit.AddListener(s => OnBlueColorChanged(float.Parse(s)));
        s_alpha.slider.onValueChanged.AddListener(OnAlphaChanged);
        s_alpha.txtValue.onEndEdit.AddListener(s => OnAlphaChanged(float.Parse(s)));

        btn_reset.onClick.AddListener(OnResetToDefault);
    }

    private void OnDestroy() {
        s_lineLength.slider.onValueChanged.RemoveListener(OnLineLengthChanged);
        s_lineLength.txtValue.onEndEdit.RemoveListener(s => OnLineLengthChanged(float.Parse(s)));
        s_spacing.slider.onValueChanged.RemoveListener(OnLineSpacingChanged);
        s_spacing.txtValue.onEndEdit.RemoveListener(s => OnLineSpacingChanged(float.Parse(s)));
        s_thickness.slider.onValueChanged.RemoveListener(OnLineThicknessChanged);
        s_thickness.txtValue.onEndEdit.RemoveListener(s => OnLineThicknessChanged(float.Parse(s)));
        s_dotSize.slider.onValueChanged.RemoveListener(OnDotSizeChanged);
        s_dotSize.txtValue.onEndEdit.RemoveListener(s => OnDotSizeChanged(float.Parse(s)));
        s_redColor.slider.onValueChanged.RemoveListener(OnRedColorChanged);
        s_redColor.txtValue.onEndEdit.RemoveListener(s => OnRedColorChanged(float.Parse(s)));
        s_greenColor.slider.onValueChanged.RemoveListener(OnGreenColorChanged);
        s_greenColor.txtValue.onEndEdit.RemoveListener(s => OnGreenColorChanged(float.Parse(s)));
        s_blueColor.slider.onValueChanged.RemoveListener(OnBlueColorChanged);
        s_blueColor.txtValue.onEndEdit.RemoveListener(s => OnBlueColorChanged(float.Parse(s)));
        s_alpha.slider.onValueChanged.RemoveListener(OnAlphaChanged);
        s_alpha.txtValue.onEndEdit.RemoveListener(s => OnAlphaChanged(float.Parse(s)));

        btn_reset.onClick.RemoveListener(OnResetToDefault);
    }

    private void Start() {
        SetupUI();
    }

    private void SetupUI() {
        LoadCrosshairSettings();

        SetupSlider(s_lineLength, m_crosshairMinMaxSettings);
        SetupSlider(s_spacing, m_crosshairMinMaxSettings);
        SetupSlider(s_thickness, m_crosshairMinMaxSettings);
        SetupSlider(s_dotSize, m_crosshairMinMaxSettings);
        SetupSlider(s_redColor, m_colorMinMaxSettings);
        SetupSlider(s_greenColor, m_colorMinMaxSettings);
        SetupSlider(s_blueColor, m_colorMinMaxSettings);
        SetupSlider(s_alpha, m_colorMinMaxSettings);

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

        AddListeners();
    }

    private void SetupSlider(CustomSlider slider, Vector2 minMax) {
        slider.slider = slider.settings.GetSlider();
        slider.txtValue = slider.settings.GetInputField();

        slider.minValue = minMax.x;
        slider.maxValue = minMax.y;

        slider.slider.minValue = minMax.x;
        slider.slider.maxValue = minMax.y;
    }
    #endregion

    #region Events
    private void OnLineLengthChanged(float value) {
        m_lineLength = Mathf.Clamp( value, s_lineLength.minValue, s_lineLength.maxValue);
        ChangeCrosshairSpacing();
        ChangeCrosshairThickness();
    }

    private void OnLineSpacingChanged(float value) {
        m_spacing = Mathf.Clamp(value, s_spacing.minValue, s_spacing.maxValue);        
        ChangeCrosshairSpacing();
    }

    private void OnLineThicknessChanged(float value) {
        m_thickness = Mathf.Clamp(value, s_thickness.minValue, s_thickness.maxValue);        
        ChangeCrosshairThickness();
    }

    private void OnRedColorChanged(float color) {
        m_redColor = Mathf.Clamp(color, s_redColor.minValue, s_redColor.maxValue);        
        SetCrosshairColor();
    }

    private void OnGreenColorChanged(float color) {
        m_greenColor = Mathf.Clamp(color, s_greenColor.minValue, s_greenColor.maxValue);        
        SetCrosshairColor();
    }

    private void OnBlueColorChanged(float color) {
        m_blueColor = Mathf.Clamp(color, s_blueColor.minValue, s_blueColor.maxValue);        
        SetCrosshairColor();
    }

    private void OnAlphaChanged(float alpha) {
        m_alpha = Mathf.Clamp(alpha, s_alpha.minValue, s_alpha.maxValue);        
        SetCrosshairColor();
    }

    private void OnDotSizeChanged(float value) {
        m_dotSize = Mathf.Clamp(value, s_dotSize.minValue, s_dotSize.maxValue);        
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
        s_spacing.txtValue.SetTextWithoutNotify(m_spacing.ToString("0.00"));
        s_lineLength.txtValue.SetTextWithoutNotify(m_lineLength.ToString("0.00"));

        s_spacing.slider.SetValueWithoutNotify(m_spacing);
        s_lineLength.slider.SetValueWithoutNotify(m_lineLength);

        m_lines[0].rectTransform.anchoredPosition = new Vector2(0, m_spacing + m_lineLength / 2);
        m_lines[1].rectTransform.anchoredPosition = new Vector2(0, -m_spacing - m_lineLength / 2);
        m_lines[2].rectTransform.anchoredPosition = new Vector2(-m_spacing - m_lineLength / 2, 0);
        m_lines[3].rectTransform.anchoredPosition = new Vector2(m_spacing + m_lineLength / 2, 0);

        m_previewLines[0].rectTransform.anchoredPosition = new Vector2(0, m_spacing + m_lineLength / 2);
        m_previewLines[1].rectTransform.anchoredPosition = new Vector2(0, -m_spacing - m_lineLength / 2);
        m_previewLines[2].rectTransform.anchoredPosition = new Vector2(-m_spacing - m_lineLength / 2, 0);
        m_previewLines[3].rectTransform.anchoredPosition = new Vector2(m_spacing + m_lineLength / 2, 0);
    }

    private void ChangeCrosshairThickness() {
        s_lineLength.txtValue.SetTextWithoutNotify(m_lineLength.ToString("0.00"));
        s_thickness.txtValue.SetTextWithoutNotify(m_thickness.ToString("0.00"));

        s_lineLength.slider.SetValueWithoutNotify(m_lineLength);
        s_thickness.slider.SetValueWithoutNotify(m_thickness);

        foreach (var line in m_lines) line.rectTransform.sizeDelta = new Vector2(m_thickness, m_lineLength);
        foreach (var line in m_previewLines) line.rectTransform.sizeDelta = new Vector2(m_thickness, m_lineLength);
    }

    private void SetCrosshairColor() {
        m_crosshairColor = new Color(m_redColor, m_greenColor, m_blueColor, m_alpha);

        s_redColor.slider.SetValueWithoutNotify(m_redColor);
        s_greenColor.slider.SetValueWithoutNotify(m_greenColor);
        s_blueColor.slider.SetValueWithoutNotify(m_blueColor);
        s_alpha.slider.SetValueWithoutNotify(m_alpha);

        s_redColor.txtValue.SetTextWithoutNotify(m_redColor.ToString("0.00"));
        s_greenColor.txtValue.SetTextWithoutNotify(m_greenColor.ToString("0.00"));
        s_blueColor.txtValue.SetTextWithoutNotify(m_blueColor.ToString("0.00"));
        s_alpha.txtValue.SetTextWithoutNotify(m_alpha.ToString("0.00"));

        m_dot.color = m_crosshairColor;
        m_previewDot.color = m_crosshairColor;
        foreach (var line in m_lines) line.color = m_crosshairColor;
        foreach (var line in m_previewLines) line.color = m_crosshairColor;
    }

    private void ChangeDotSize() {
        s_dotSize.txtValue.SetTextWithoutNotify(m_dotSize.ToString("0.00"));
        s_dotSize.slider.SetValueWithoutNotify(m_dotSize);

        m_dot.rectTransform.sizeDelta = new Vector2(m_dotSize, m_dotSize);
        m_previewDot.rectTransform.sizeDelta = new Vector2(m_dotSize, m_dotSize);
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
public class CustomSlider {
    public UI_SliderSetting settings;
    [HideInInspector] public float minValue;
    [HideInInspector] public float maxValue;
    [HideInInspector] public Slider slider;
    [HideInInspector] public TMP_InputField txtValue;
}
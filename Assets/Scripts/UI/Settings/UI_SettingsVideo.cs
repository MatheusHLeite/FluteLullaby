using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Events;

public class UI_SettingsVideo : MonoBehaviour {
    private SettingsManager manager;

    [Header("UI Components")]
    [SerializeField] private UI_Setting resolutionOptions;
    [SerializeField] private UI_Setting displayModeOptions;
    [SerializeField] private UI_Setting presetQualityOptions;
    [SerializeField] private UI_Setting textureQualityOptions;
    [SerializeField] private UI_Setting shadowQualityOptions;
    [SerializeField] private UI_Setting lightningQualityOptions;
    [SerializeField] private UI_Setting effectsQualityOptions;
    [SerializeField] private UI_Setting vSyncOptions;
    [SerializeField] private UI_Setting antiAliasingOptions;
    [SerializeField] private UI_Setting hdrOptions;
    [SerializeField] private UI_Setting renderDistanceOptions;
    [SerializeField] private UI_Setting anisotropicFilteringOptions;
    [SerializeField] private UI_Setting ambientOcclusionOptions;
    [SerializeField] private UI_SliderSetting fpsLimitSlider;
    [SerializeField] private UI_SliderSetting gammaSlider;
    [SerializeField] private UI_SliderSetting resolutionScaleSlider;
    [SerializeField] private UI_Setting postProcessingOptions;
    [SerializeField] private UI_Setting motionBlurOptions;
    [SerializeField] private UI_Setting colorblindModeOptions;
    [Space(10)]
    [SerializeField] private Button btn_apply;

    [Header("Pop up")]
    [SerializeField] private PopUp discardChangesPopUp;

    public const string POP_UP_TITLE = "Discard changes?";
    public const string POP_UP_BODY = "You have unsaved changes, do you want to discard them?";
    public const string POP_UP_ACCEPT = "Apply changes";
    public const string POP_UP_DISCARD = "Discard changes";

    private bool hasChangesToApply;

    private UnityAction onApply;
    private UnityAction onDiscard;

    private void Awake() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.AddListener(OnSettingsDataLoaded);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.RemoveListener(OnSettingsDataLoaded);

        TeardownListeners();
    }

    public bool HasChangesToApply(UnityAction action) {
        if (hasChangesToApply) {
            onApply += action;
            onDiscard += action;

            discardChangesPopUp.Setup(POP_UP_TITLE, POP_UP_BODY, POP_UP_ACCEPT, POP_UP_DISCARD, onApply, onDiscard);

            discardChangesPopUp.OpenPopUp();
            return true;
        }

        action?.Invoke();
        return false;
    }

    private void OnSettingsDataLoaded(PlayerSaveData data) {
        manager = Singleton.Instance.SettingsManager;        

        List<UIOption> disabledHDR = new List<UIOption>() {
            new UIOption() { text = "Disabled", value = 0 }
        };

        displayModeOptions.SetupOptions(manager.displayModeOptions, data.settings.displayModeIndex);
        resolutionOptions.SetupOptions(manager.resolutionOptions, data.settings.resolutionIndex);

        presetQualityOptions.SetupOptions(manager.qualityOptions, data.settings.qualityPresetIndex);
        textureQualityOptions.SetupOptions(manager.qualityOptions, data.settings.textureQualityIndex);
        shadowQualityOptions.SetupOptions(manager.qualityOptions, data.settings.shadowQualityIndex);
        lightningQualityOptions.SetupOptions(manager.qualityOptions, data.settings.lightningQualityIndex);
        ambientOcclusionOptions.SetupOptions(manager.qualityOptions, data.settings.ambientOcclusionIndex);
        effectsQualityOptions.SetupOptions(manager.qualityOptions, data.settings.effectsQualityIndex);
        postProcessingOptions.SetupOptions(manager.qualityOptions, data.settings.postProcessingQualityIndex);

        hdrOptions.SetupOptions(!manager.HDRSupport ? disabledHDR : manager.enableOptions, data.settings.hdrEnabledIndex);
        renderDistanceOptions.SetupOptions(manager.renderDistanceOptions, data.settings.renderDistanceIndex);
        fpsLimitSlider.Setup(new Vector2(30, 301), data.settings.fpsLimitValue, data.settings.fpsLimitValue < 300 
            ? data.settings.fpsLimitValue.ToString() : "Unlimited");

        antiAliasingOptions.SetupOptions(manager.antiAliasingOptions, data.settings.antiAliasingModeIndex);
        anisotropicFilteringOptions.SetupOptions(manager.enableOptions, data.settings.anisotropicFilteringIndex);
        vSyncOptions.SetupOptions(manager.inverseEnableOptions, data.settings.vSyncEnabledIndex);

        gammaSlider.Setup(new Vector2(0, 1), data.settings.gammaValue, null, false, false);
        resolutionScaleSlider.Setup(new Vector2(0.11f,2), data.settings.resolutionScaleValue, null, false);

        motionBlurOptions.SetupOptions(manager.inverseEnableOptions, data.settings.motionBlurEnabled);

        colorblindModeOptions.SetupOptions(manager.colorblindModeOptions, data.settings.colorblindMode);

        if (!manager.HDRSupport) hdrOptions.SetInactive();

        SetupListeners();
    }

    private void SetupListeners() {       
        presetQualityOptions.onValueChanged.AddListener(SetQualityPreset);
        resolutionOptions.onValueChanged.AddListener(SetApplyButtonActive);
        displayModeOptions.onValueChanged.AddListener(SetApplyButtonActive);   
        vSyncOptions.onValueChanged.AddListener(i => manager.SetVSync(i));
        fpsLimitSlider.AddListener(i => manager.SetFPSLimit(i));
        gammaSlider.AddListener(i => manager.SetGamma(i));
        resolutionScaleSlider.AddListener(i => manager.SetResolutionScale(i));

        textureQualityOptions.onValueChanged.AddListener(i => manager.SetTextureQuality(i));
        shadowQualityOptions.onValueChanged.AddListener(i => manager.SetShadowQuality(i));
        lightningQualityOptions.onValueChanged.AddListener(i => manager.SetLightningQuality(i));
        antiAliasingOptions.onValueChanged.AddListener(i => manager.SetAntiAliasing(i));
        anisotropicFilteringOptions.onValueChanged.AddListener(i => manager.SetAnisotropicFiltering(i));
        ambientOcclusionOptions.onValueChanged.AddListener(i => manager.SetAmbientOcclusion(i));
        effectsQualityOptions.onValueChanged.AddListener(i => manager.SetEffectQuality(i));

        textureQualityOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        shadowQualityOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        lightningQualityOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        antiAliasingOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        anisotropicFilteringOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        ambientOcclusionOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        effectsQualityOptions.onValueChanged.AddListener(OnQualitySettingsChanged);
        postProcessingOptions.onValueChanged.AddListener(OnQualitySettingsChanged);

        hdrOptions.onValueChanged.AddListener(i => manager.SetHDR(i));
        renderDistanceOptions.onValueChanged.AddListener(i => manager.SetRenderDistance(i));

        postProcessingOptions.onValueChanged.AddListener(i => manager.SetPostProcessingQuality(i));
        motionBlurOptions.onValueChanged.AddListener(i => manager.SetMotionBlur(i));

        colorblindModeOptions.onValueChanged.AddListener(i => manager.SetColorBlindMode(i));

        btn_apply.onClick.AddListener(OnSettingsApply);
        btn_apply.gameObject.SetActive(false);

        onApply += OnSettingsApply;
        onDiscard += OnSettingsDiscard;

        presetQualityOptions.SetSpecificIndex(Singleton.Instance.SaveManager.PlayerData.settings.qualityPresetIndex);
    }

    private void TeardownListeners() {
        presetQualityOptions.onValueChanged.RemoveAllListeners();
        resolutionOptions.onValueChanged.RemoveAllListeners();
        displayModeOptions.onValueChanged.RemoveAllListeners();
        textureQualityOptions.onValueChanged.RemoveAllListeners();
        shadowQualityOptions.onValueChanged.RemoveAllListeners();
        lightningQualityOptions.onValueChanged.RemoveAllListeners();
        vSyncOptions.onValueChanged.RemoveAllListeners();
        antiAliasingOptions.onValueChanged.RemoveAllListeners();
        hdrOptions.onValueChanged.RemoveAllListeners();
        renderDistanceOptions.onValueChanged.RemoveAllListeners();
        anisotropicFilteringOptions.onValueChanged.RemoveAllListeners();
        ambientOcclusionOptions.onValueChanged.RemoveAllListeners();
        effectsQualityOptions.onValueChanged.RemoveAllListeners();
        postProcessingOptions.onValueChanged.RemoveAllListeners();
        motionBlurOptions.onValueChanged.RemoveAllListeners();
        fpsLimitSlider.RemoveAllListeners();
        gammaSlider.RemoveAllListeners();
        resolutionScaleSlider.RemoveAllListeners();
        colorblindModeOptions.onValueChanged.RemoveAllListeners();

        btn_apply.onClick.RemoveAllListeners();

        onApply -= OnSettingsApply;
        onDiscard -= OnSettingsDiscard;
    }

    public void SetQualityPreset(int index) {
        manager.SetQualityPreset(index);

        if (index == -1) {
            return;
        }

        Quality qualityPreset = Quality.Ultra;
        int textureQuality = 0;
        int shadowQuality = 0;
        int lightningQuality = 0;
        int antiAliasingQuality = 0;
        int hdrEnabled = 0;
        int anisotropicFiltering = 0;
        int ambientOcclusion = 0;
        int effectsQuality = 0;
        int renderDistance = 0;
        int postProcessing = 0;

        switch (index) {
            case 0: qualityPreset = Quality.Low; break;
            case 1: qualityPreset = Quality.Medium; break;
            case 2: qualityPreset = Quality.High; break;
            case 3: qualityPreset = Quality.Ultra; break;
        }

        switch (qualityPreset) {
            case Quality.Low:
                textureQuality = 0;
                shadowQuality = 0;
                lightningQuality = 0;
                antiAliasingQuality = 0;
                hdrEnabled = 0;
                anisotropicFiltering = 0;
                ambientOcclusion = 0;
                effectsQuality = 0;
                renderDistance = 0;
                postProcessing = 0;
                break;
            case Quality.Medium:
                textureQuality = 1;
                shadowQuality = 1;
                lightningQuality = 1;
                antiAliasingQuality = 1;
                hdrEnabled = 0;
                anisotropicFiltering = 0;
                ambientOcclusion = 1;
                effectsQuality = 1;
                renderDistance = 1;
                postProcessing = 1;
                break;
            case Quality.High:
                textureQuality = 2;
                shadowQuality = 2;
                lightningQuality = 2;
                antiAliasingQuality = 2;
                hdrEnabled = 0;
                anisotropicFiltering = 1;
                ambientOcclusion = 2;
                effectsQuality = 2;
                renderDistance = 2;
                postProcessing = 2;
                break;
            case Quality.Ultra:
                textureQuality = 3;
                shadowQuality = 3;
                lightningQuality = 3;
                antiAliasingQuality = 3;
                hdrEnabled = 1;
                anisotropicFiltering = 1;
                ambientOcclusion = 3;
                effectsQuality = 3;
                renderDistance = 2;
                postProcessing = 3;
                break;
        }

        textureQualityOptions.UpdateIndexWithoutNotify(textureQuality);
        shadowQualityOptions.UpdateIndexWithoutNotify(shadowQuality);
        lightningQualityOptions.UpdateIndexWithoutNotify(lightningQuality);
        antiAliasingOptions.UpdateIndexWithoutNotify(antiAliasingQuality);
        if (HDROutputSettings.main.available) hdrOptions.UpdateIndexWithoutNotify(hdrEnabled);
        anisotropicFilteringOptions.UpdateIndexWithoutNotify(anisotropicFiltering);
        ambientOcclusionOptions.UpdateIndexWithoutNotify(ambientOcclusion);
        effectsQualityOptions.UpdateIndexWithoutNotify(effectsQuality);
        renderDistanceOptions.UpdateIndexWithoutNotify(renderDistance);
        postProcessingOptions.UpdateIndexWithoutNotify(postProcessing);

        manager.SetTextureQuality(textureQuality);
        manager.SetShadowQuality(shadowQuality);
        manager.SetLightningQuality(lightningQuality);
        manager.SetAntiAliasing(antiAliasingQuality);
        if (HDROutputSettings.main.available) manager.SetHDR(hdrEnabled);
        manager.SetAnisotropicFiltering(anisotropicFiltering);
        manager.SetAmbientOcclusion(ambientOcclusion);
        manager.SetEffectQuality(effectsQuality);
        manager.SetRenderDistance(renderDistance);
        manager.SetPostProcessingQuality(postProcessing);
    }

    private void OnQualitySettingsChanged(int i) {
        presetQualityOptions.SetSpecificIndex(-1);
    }

    private void SetApplyButtonActive(int i) {
        btn_apply.gameObject.SetActive(true);

        hasChangesToApply = true;
    }

    private void OnSettingsApply() {        
        manager.SetResolution(resolutionOptions.GetSettingsIndex());
        manager.SetDisplayMode(displayModeOptions.GetSettingsIndex());

        OnSettingsClosed();
    }

    private void OnSettingsDiscard() {
        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;

        resolutionOptions.SetSpecificIndex(data.settings.resolutionIndex);
        displayModeOptions.SetSpecificIndex(data.settings.displayModeIndex);

        OnSettingsClosed();
    }

    public void OnSettingsClosed() {
        btn_apply.gameObject.SetActive(false);

        discardChangesPopUp.ClosePopUp();
        hasChangesToApply = false;
    }
}
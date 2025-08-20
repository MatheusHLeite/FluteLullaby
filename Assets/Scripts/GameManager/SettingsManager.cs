using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour {
    [Header("Settings")]
    [SerializeField] private VolumeProfile m_globalVolume;
    [SerializeField] private ScreenSpaceAmbientOcclusion m_ssao;
    private ColorAdjustments colorAdjustments;

    [Header("Audio")]
    [SerializeField] private AudioMixer m_audioMixer;

    private SaveManager SaveManager;

    public const string MasterVolume = "MasterVolume";
    public const string MusicVolume = "MusicVolume";
    public const string SFXVolume = "SFXVolume";
    public const string VoiceChatVolume = "VoiceChatVolume";

    private UniversalRenderPipelineAsset urpAsset;
    private Resolution[] resolutions;
    private string[] allDevices;
    private List<string> allResolutions;
    
    private string actualMicrophone;    

    #region Video options string
    public List<UIOption> qualityPresetOptions { get; private set; }
    public Vector2 minMaxFPS { get; private set; }
    public List<UIOption> enableOptions { get; private set; }
    public List<UIOption> inverseEnableOptions { get; private set; }
    public List<UIOption> resolutionOptions { get; private set; }
    public List<RefreshRate> refreshRateOptions { get; private set; }
    public List<UIOption> displayModeOptions { get; private set; }
    public List<UIOption> textureQualityOptions { get; private set; }
    public List<UIOption> shadowQualityOptions { get; private set; }
    public List<UIOption> antiAliasingOptions { get; private set; }
    public List<UIOption> anisotropicFilteringOptions { get; private set; }
    public List<UIOption> ambientOcclusionOption { get; private set; }
    #endregion

    #region Audio options string
    public List<UIOption> outputDevices { get; private set; }
    #endregion

    private void Awake() {        
        Singleton.Instance.GameEvents.OnDataLoaded.AddListener(OnSettingsDataLoaded);

        if (m_globalVolume.TryGet(out ColorAdjustments ca))       
            colorAdjustments = ca;        

        SaveManager = Singleton.Instance.SaveManager;

        minMaxFPS = new Vector2(30, 300);

        resolutions = Screen.resolutions;
        allResolutions = resolutions.Select(r => $"{r.width} x {r.height}").Distinct().ToList();

        resolutionOptions = new List<UIOption>();
        for (int i = 0; i < allResolutions.Count; i++)
            resolutionOptions.Add(CreateNewOption(allResolutions[i], i));
        refreshRateOptions = resolutions.Select(r => r.refreshRateRatio).Distinct().ToList();

        enableOptions = new List<UIOption>() { 
            CreateNewOption("Disabled", 0),
            CreateNewOption("Enabled", 1) 
        };
        inverseEnableOptions = new List<UIOption>() {
            CreateNewOption("Enabled", 1),
            CreateNewOption("Disabled", 0)
        };        
        displayModeOptions = new List<UIOption>() {
            CreateNewOption("Windowed", 0), //Modo janela
            CreateNewOption("Fullscreen", 1), //tela cheia
            CreateNewOption("Borderless Fullscreen", 2) //tela cheia em janela
        };
        textureQualityOptions = new List<UIOption>() {
            CreateNewOption("Minimum", 3),
            CreateNewOption("Low", 2),
            CreateNewOption("Medium", 1),
            CreateNewOption("High", 0)
        };
        shadowQualityOptions = new List<UIOption>() {
            CreateNewOption("Low", 2),
            CreateNewOption("Medium", 1),
            CreateNewOption("High", 0)
        };
        antiAliasingOptions = new List<UIOption>() {
            CreateNewOption("Disabled", 1),
            CreateNewOption("2x", 2),
            CreateNewOption("4x", 4),
            CreateNewOption("8x", 8)
        };
        outputDevices = new List<UIOption>();
        qualityPresetOptions = new List<UIOption>() {
            CreateNewOption("Lowest", 0),
            CreateNewOption("Low", 1),
            CreateNewOption("Medium", 2),
            CreateNewOption("High", 3),
            CreateNewOption("Ultra", 4)
        };
        anisotropicFilteringOptions = new List<UIOption>() {
            CreateNewOption("Disabled", 0),
            CreateNewOption("Per texture", 1),
            CreateNewOption("Forced on", 2),
        };
        ambientOcclusionOption = new List<UIOption>() {
            CreateNewOption("Disabled", 2),
            CreateNewOption("Low", 1),
            CreateNewOption("High", 0)
        };

        UpdateMicrophoneList();
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnDataLoaded.RemoveListener(OnSettingsDataLoaded);

        enableOptions.Clear();
        inverseEnableOptions.Clear();
        resolutionOptions.Clear();
        refreshRateOptions.Clear();
        displayModeOptions.Clear();
        textureQualityOptions.Clear();
        shadowQualityOptions.Clear();
        antiAliasingOptions.Clear();
    }

    private void UpdateMicrophoneList() {
        if (allDevices == Microphone.devices) return;
        allDevices = Microphone.devices;

        outputDevices.Clear();
        for (int i = 0; i < allDevices.Length; i++)
            outputDevices.Add(CreateNewOption(allDevices[i], i));
    }

    private UIOption CreateNewOption(string text, int value) {
        return new UIOption() { text = text, value = value };
    }

    private void OnSettingsDataLoaded(PlayerSaveData data) {
        urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

        if (data.settings.firstSetup) {
            SetupFirstSettings(data);
        }

        SetResolution(data.settings.resolutionIndex, true);
        SetDisplayMode(data.settings.displayModeIndex, true);
        SetRefreshRate(data.settings.refreshRateIndex, true);
        SetQualityPreset(data.settings.qualityPresetIndex, true);
        SetTextureQuality(data.settings.textureQualityIndex, true);
        SetShadowQuality(data.settings.shadowQualityIndex, true);        
        SetVSync(data.settings.vSyncEnabledIndex, true);
        SetAntiAliasing(data.settings.antiAliasingModeIndex, true);
        SetHDR(data.settings.hdrEnabledIndex, true);
        SetFPSLimit(data.settings.fpsLimitValue, true);
        SetAnisotropicFiltering(data.settings.anisotropicFilteringIndex, true);
        SetAmbientOcclusion(data.settings.ambientOcclusionIndex, true);
        SetResolutionScale(data.settings.resolutionScaleValue, true);
        SetGamma(data.settings.gammaValue, true);

        SetVolume(data.settings.masterVolume.volume, data.settings.masterVolume.volumeMixer, true);
        SetVolume(data.settings.musicVolume.volume, data.settings.musicVolume.volumeMixer, true);
        SetVolume(data.settings.soundEffectsVolume.volume, data.settings.soundEffectsVolume.volumeMixer, true);
        SetVolume(data.settings.voiceChatVolume.volume, data.settings.voiceChatVolume.volumeMixer, true);
        SetOutputDevice(data.settings.outputDeviceIndex, true);

        Singleton.Instance.GameEvents.OnSettingsDataLoaded?.Invoke(data);
    }

    #region Settings setup
    private void SetupFirstSettings(PlayerSaveData data) {
        data.settings.firstSetup = false;

        int defaultResolutionIndex = allResolutions.IndexOf($"{Screen.currentResolution.width} x {Screen.currentResolution.height}");
        int defaultRefreshRateIndex = refreshRateOptions.FindIndex(o => o.ToString() == Screen.currentResolution.refreshRateRatio.ToString());

        data.settings.resolutionIndex = defaultResolutionIndex;
        data.settings.refreshRateIndex = defaultRefreshRateIndex;
        data.settings.fpsLimitValue = (int)Screen.currentResolution.refreshRateRatio.value;

        SaveSystemHandler.SaveData(data);
    }
    #endregion

    #region Video settings
    public void SetResolution(int index, bool initialization = false) { 
        SetResolution(resolutions[index].width, resolutions[index].height, Screen.fullScreenMode, Screen.currentResolution.refreshRateRatio);

        if (!initialization && SaveManager.PlayerData.settings.resolutionIndex != index) {
            SaveManager.PlayerData.settings.resolutionIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetDisplayMode(int index, bool initialization = false) {
        int indexValue = displayModeOptions[index].value;

        FullScreenMode screenMode = FullScreenMode.ExclusiveFullScreen;
        switch (indexValue) {
            case 0:
                screenMode = FullScreenMode.Windowed;
                break;
            case 1:
                screenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 2:
                screenMode = FullScreenMode.FullScreenWindow;
                break;
        }

        Screen.fullScreenMode = screenMode;

        if (!initialization && SaveManager.PlayerData.settings.displayModeIndex != index) {
            SaveManager.PlayerData.settings.displayModeIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetRefreshRate(int index, bool initialization = false) { 
        SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, Screen.fullScreenMode, refreshRateOptions[index]);

        if (!initialization && SaveManager.PlayerData.settings.refreshRateIndex != index) {
            SaveManager.PlayerData.settings.refreshRateIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetQualityPreset(int index, bool initialization = false) {
        if (!initialization && SaveManager.PlayerData.settings.qualityPresetIndex != index) {
            SaveManager.PlayerData.settings.qualityPresetIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetTextureQuality(int index, bool initialization = false) {
        int indexValue = textureQualityOptions[index].value;

        QualitySettings.globalTextureMipmapLimit = indexValue;

        if (!initialization && SaveManager.PlayerData.settings.textureQualityIndex != index) { 
            SaveManager.PlayerData.settings.textureQualityIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetShadowQuality(int index, bool initialization = false) {
        int indexValue = textureQualityOptions[index].value;

        float shadowDistance = 50f;
        int shadowCascadeCount = 4;
        float shadowDepthBias = 1f;
        float shadowNormalBias = 0.4f;
        bool enableSoftShadows = true;
        int shadowQualityIndex = 1;

        var softShadowsField = typeof(UniversalRenderPipelineAsset)
            .GetField("m_SoftShadowsSupported", BindingFlags.NonPublic | BindingFlags.Instance);
        var softShadowQualityField = typeof(UniversalRenderPipelineAsset)
            .GetField("m_SoftShadowQuality", BindingFlags.NonPublic | BindingFlags.Instance);

        switch (indexValue) {
            case 0: //High
                shadowDistance = 80f;
                shadowCascadeCount = 4;
                shadowDepthBias = .5f;
                shadowNormalBias = .3f;
                enableSoftShadows = true;
                shadowQualityIndex = 3;
                break;
            case 1: //Medium
                shadowDistance = 40f;
                shadowCascadeCount = 2;
                shadowDepthBias = 1;
                shadowNormalBias = .5f;
                enableSoftShadows = true;
                shadowQualityIndex = 2;
                break;
            case 2: //Low
                shadowDistance = 20f;
                shadowCascadeCount = 1;
                shadowDepthBias = 2;
                shadowNormalBias = .8f;
                enableSoftShadows = false;
                shadowQualityIndex = 1;
                break;
        }

        urpAsset.shadowDistance = shadowDistance;
        urpAsset.shadowCascadeCount = shadowCascadeCount;
        urpAsset.shadowDepthBias = shadowDepthBias;
        urpAsset.shadowNormalBias = shadowNormalBias;
        softShadowsField.SetValue(urpAsset, enableSoftShadows);
        softShadowQualityField.SetValue(urpAsset, shadowQualityIndex);

        if (!initialization && SaveManager.PlayerData.settings.shadowQualityIndex != index) {
            SaveManager.PlayerData.settings.shadowQualityIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetVSync(int index, bool initialization = false) {
        int indexValue = inverseEnableOptions[index].value;

        QualitySettings.vSyncCount = indexValue;

        if (!initialization && SaveManager.PlayerData.settings.vSyncEnabledIndex != index) {
            SaveManager.PlayerData.settings.vSyncEnabledIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetAntiAliasing(int index, bool initialization = false) {
        int indexValue = antiAliasingOptions[index].value;
        urpAsset.msaaSampleCount = indexValue;

        if (!initialization && SaveManager.PlayerData.settings.antiAliasingModeIndex != index) {
            SaveManager.PlayerData.settings.antiAliasingModeIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetHDR(int index, bool initialization = false) {
        int indexValue = enableOptions[index].value;
        urpAsset.supportsHDR = indexValue == 1;

        if (!initialization && SaveManager.PlayerData.settings.hdrEnabledIndex != index) {
            SaveManager.PlayerData.settings.hdrEnabledIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetFPSLimit(float value, bool initialization = false) { 
        Application.targetFrameRate = (int)value >= minMaxFPS.x && (int)value <= minMaxFPS.y ? (int)value : -1;

        if (!initialization && SaveManager.PlayerData.settings.fpsLimitValue != (int)value) {
            SaveManager.PlayerData.settings.fpsLimitValue = (int)value;
            OnSettingsSaved();
        }           
    }

    public void SetAnisotropicFiltering(int index, bool initialization = false) {
        QualitySettings.anisotropicFiltering = (AnisotropicFiltering)index;

        if (!initialization && SaveManager.PlayerData.settings.anisotropicFilteringIndex != index)  {
            SaveManager.PlayerData.settings.anisotropicFilteringIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetAmbientOcclusion(int index, bool initialization = false) {
        int indexValue = ambientOcclusionOption[index].value;

        return;
        var field = typeof(ScreenSpaceAmbientOcclusion).GetField("m_Settings", BindingFlags.NonPublic | BindingFlags.Instance);
        object settings = field?.GetValue(m_ssao as ScreenSpaceAmbientOcclusion);
        var intensitySettings = settings.GetType().GetField("intensity", BindingFlags.Public | BindingFlags.Instance);
        var radiusSettings = settings.GetType().GetField("radius", BindingFlags.Public | BindingFlags.Instance);

        float intensity = .4f;
        float radius = .1f;

        switch (indexValue) {
            case 0: //High
                m_ssao.SetActive(true);
                intensity = 0.8f;
                radius = 0.5f;
                break;
            case 1: //Low
                m_ssao.SetActive(true);
                intensity = 0.6f;
                radius = 0.45f;
                break;
            case 2: //Disabled
                m_ssao.SetActive(false);
                intensity = 0f;
                radius = 0f;
                break;
        }
        
        intensitySettings.SetValue(settings, intensity);
        radiusSettings.SetValue(settings, radius);

        if (!initialization && SaveManager.PlayerData.settings.ambientOcclusionIndex != index) {
            SaveManager.PlayerData.settings.ambientOcclusionIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetResolutionScale(float value, bool initialization = false) {
        urpAsset.renderScale = value;

        if (!initialization && SaveManager.PlayerData.settings.resolutionScaleValue != value) {
            SaveManager.PlayerData.settings.resolutionScaleValue = value;
            OnSettingsSaved();
        }
    }

    public void SetGamma(float value, bool initialization = false) {
        float resultValue = Mathf.Lerp(-2, 2, value);
        colorAdjustments.postExposure.Override(resultValue);

        if (!initialization && SaveManager.PlayerData.settings.gammaValue != value) {
            SaveManager.PlayerData.settings.gammaValue = value;
            OnSettingsSaved();
        }
    }

    private void SetResolution(int width, int height, FullScreenMode mode, RefreshRate refreshRate) => Screen.SetResolution(width, height, mode, refreshRate);
    #endregion

    #region Audio
    public void SetVolume(float volume, VolumeMixer mixer, bool initialization = false) {
        float newVolume = Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 38f;
        string volumeType = "";

        switch (mixer) {
            case VolumeMixer.Master:
                volumeType = MasterVolume;
                if (!initialization) SaveManager.PlayerData.settings.masterVolume.volume = newVolume;
                break;
            case VolumeMixer.Music:
                volumeType = MusicVolume;
                if (!initialization) SaveManager.PlayerData.settings.musicVolume.volume = newVolume;
                break;
            case VolumeMixer.SFX:
                volumeType = SFXVolume;
                if (!initialization) SaveManager.PlayerData.settings.soundEffectsVolume.volume = newVolume;
                break;
            case VolumeMixer.VoiceChat:
                volumeType = VoiceChatVolume;
                if (!initialization) SaveManager.PlayerData.settings.voiceChatVolume.volume = newVolume;
                break;
        }

        if (!initialization)
            OnSettingsSaved();

        m_audioMixer.SetFloat(volumeType, newVolume);
    }

    public void SetOutputDevice(int index, bool initialization = false) {
        actualMicrophone = allDevices[index];
   
        Singleton.Instance.GameEvents.OnMicrophoneDeviceSwitch?.Invoke(actualMicrophone);

        if (!initialization && SaveManager.PlayerData.settings.outputDeviceIndex != index) {
            SaveManager.PlayerData.settings.outputDeviceIndex = index;
            OnSettingsSaved();
        }        

        UpdateMicrophoneList();
    }
    #endregion

    private void OnSettingsSaved() {
        SaveSystemHandler.SaveData(SaveManager.PlayerData);
    }
}

public struct UIOption {
    public string text;
    public int value;
}

[System.Serializable]
public struct UISliderOption {
    public Slider slider;
    public TMP_Text text;
}

public enum VolumeMixer { Master, Music, SFX, VoiceChat }

public enum QualityPreset { Lowest, Low, Medium, High, Ultra }
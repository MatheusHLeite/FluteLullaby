using System;
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
    private ColorAdjustments colorAdjustments;
    private MotionBlur motionBlur;

    [Header("Audio")]
    [SerializeField] private AudioMixer m_audioMixer;

    [Header("Bindable Inputs")]
    [SerializeField] private KeyBind[] m_allKeyBinds;

    private SaveManager SaveManager;

    public static Quality GlobalEffectsQuality;

    public const string MasterVolume = "MasterVolume";
    public const string MusicVolume = "MusicVolume";
    public const string SFXVolume = "SFXVolume";
    public const string VoiceChatVolume = "VoiceChatVolume";

    private UniversalRenderPipelineAsset urpAsset;
    private Resolution[] resolutions;
    private string[] allDevices;
    private List<string> allResolutions;

    private float[] distances = new float[32];

    private ScreenSpaceAmbientOcclusion m_ssao;
    private FieldInfo fiSettings, fiIntensity, fiRadius;

    private FieldInfo fiSoftShadows, fiSoftShadowQuality;

    private string actualMicrophone;

    #region General options string
    public List<UIOption> enableOptions { get; private set; }
    public List<UIOption> inverseEnableOptions { get; private set; }
    #endregion

    #region Video options string    
    public List<UIOption> resolutionOptions { get; private set; }
    public List<RefreshRate> refreshRateOptions { get; private set; }
    public List<UIOption> displayModeOptions { get; private set; }
    public List<UIOption> antiAliasingOptions { get; private set; }
    public List<UIOption> qualityOptions { get; private set; }
    public List<UIOption> qualityPresetOptions { get; private set; }
    public List<UIOption> renderDistanceOptions { get; private set; }

    public Vector2 minMaxFPS { get; private set; }
    #endregion

    #region Audio options string
    public List<UIOption> outputDevices { get; private set; } = new List<UIOption>();
    #endregion

    private void Awake() {        
        Singleton.Instance.GameEvents.OnDataLoaded.AddListener(OnSettingsDataLoaded);

        if (m_globalVolume.TryGet(out ColorAdjustments ca)) colorAdjustments = ca;
        if (m_globalVolume.TryGet(out MotionBlur mb)) motionBlur = mb;
        SaveManager = Singleton.Instance.SaveManager;
        minMaxFPS = new Vector2(30, 300);

        InitializeMainVideoSettings();
        InitializeShadowSettings();
        InitializeAOSettings();

        enableOptions = new List<UIOption>() { 
            CreateNewOption("Disabled", 0),
            CreateNewOption("Enabled", 1) 
        };
        inverseEnableOptions = new List<UIOption>() {
            CreateNewOption("Enabled", 1),
            CreateNewOption("Disabled", 0)
        };        
        displayModeOptions = new List<UIOption>() {
            CreateNewOption("Windowed", 0),
            CreateNewOption("Fullscreen", 1),
            CreateNewOption("Borderless Fullscreen", 2)
        };
        qualityOptions = new List<UIOption>() {
            CreateNewOption("Low", 3),
            CreateNewOption("Medium", 2),
            CreateNewOption("High", 1),
            CreateNewOption("Ultra", 0)
        };
        qualityPresetOptions = new List<UIOption> {
            CreateNewOption("Best performance", 3),
            CreateNewOption("Balanced", 2),
            CreateNewOption("Good quality", 1),
            CreateNewOption("Best quality", 0)
        };
        antiAliasingOptions = new List<UIOption>() {
            CreateNewOption("Disabled", 1),
            CreateNewOption("2x", 2),
            CreateNewOption("4x", 4),
            CreateNewOption("8x", 8)
        };
        renderDistanceOptions = new List<UIOption> {
            CreateNewOption("Minimum", 0),
            CreateNewOption("Average", 1),
            CreateNewOption("Maximum", 2),
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
        qualityOptions.Clear();
        antiAliasingOptions.Clear();
    }

    public KeyBind[] GetAllKeys() => m_allKeyBinds;

    private void InitializeMainVideoSettings() {
        resolutions = Screen.resolutions;
        allResolutions = resolutions.Select(r => $"{r.width} x {r.height}").Distinct().ToList();

        resolutionOptions = new List<UIOption>();
        for (int i = 0; i < allResolutions.Count; i++)
            resolutionOptions.Add(CreateNewOption(allResolutions[i], i));
        refreshRateOptions = resolutions.Select(r => r.refreshRateRatio).Distinct().ToList();
    }

    private void InitializeShadowSettings() {
        fiSoftShadows = typeof(UniversalRenderPipelineAsset)
            .GetField("m_SoftShadowsSupported", BindingFlags.NonPublic | BindingFlags.Instance);
        fiSoftShadowQuality = typeof(UniversalRenderPipelineAsset)
            .GetField("m_SoftShadowQuality", BindingFlags.NonPublic | BindingFlags.Instance);
    }

    private void InitializeAOSettings() {
        var camData = Camera.main.GetUniversalAdditionalCameraData();
        ScriptableRenderer renderer = camData.scriptableRenderer;

        if (!TryGetRendererFeature(renderer, out m_ssao)) {
            Debug.LogWarning("SSAO Renderer Feature not found.");
            return;
        }

        fiSettings = typeof(ScreenSpaceAmbientOcclusion)
            .GetField("m_Settings", BindingFlags.Instance | BindingFlags.NonPublic);

        var boxed = fiSettings.GetValue(m_ssao);
        var st = boxed.GetType();

        fiIntensity = st.GetField("Intensity", BindingFlags.Instance | BindingFlags.NonPublic);
        fiRadius = st.GetField("Radius", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    static bool TryGetRendererFeature<T>(ScriptableRenderer renderer, out T feature)
        where T : ScriptableRendererFeature {
        var maybeTry = typeof(ScriptableRenderer).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "TryGetRendererFeature" && m.IsGenericMethodDefinition);
        if (maybeTry != null) {
            var g = maybeTry.MakeGenericMethod(typeof(T));
            object[] args = { null };
            bool ok = (bool)g.Invoke(renderer, args);
            feature = (T)args[0];
            if (ok && feature) return true;
        }

        var prop = typeof(ScriptableRenderer).GetProperty("rendererFeatures",
            BindingFlags.Public | BindingFlags.Instance);
        if (prop != null) {
            var list = (List<ScriptableRendererFeature>)prop.GetValue(renderer);
            feature = list.OfType<T>().FirstOrDefault();
            if (feature) return true;
        }

        var fld = typeof(ScriptableRenderer).GetField("m_RendererFeatures",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (fld != null) {
            var list = (List<ScriptableRendererFeature>)fld.GetValue(renderer);
            feature = list.OfType<T>().FirstOrDefault();
            if (feature) return true;
        }

        feature = null;
        return false;
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

        //[TODO] ANALIZE PLAYERS SPECS TO SET THE RECOMENDED SETTINGS
        SetQualityPreset(data.settings.qualityPresetIndex, true);
        SetTextureQuality(data.settings.textureQualityIndex, true);
        SetShadowQuality(data.settings.shadowQualityIndex, true);
        SetLightningQuality(data.settings.lightningQualityIndex, true);
        SetVSync(data.settings.vSyncEnabledIndex, true);
        SetAntiAliasing(data.settings.antiAliasingModeIndex, true);
        SetHDR(data.settings.hdrEnabledIndex, true);
        SetRenderDistance(data.settings.renderDistanceIndex, true);
        SetFPSLimit(data.settings.fpsLimitValue, true);
        SetAnisotropicFiltering(data.settings.anisotropicFilteringIndex, true);
        SetAmbientOcclusion(data.settings.ambientOcclusionIndex, true);
        SetResolutionScale(data.settings.resolutionScaleValue, true);
        SetGamma(data.settings.gammaValue, true);
        SetPostProcessingQuality(data.settings.postProcessingQualityIndex, true);
        SetMotionBlur(data.settings.motionBlurEnabled, true);

        SetVolume(data.settings.masterVolume.volume, data.settings.masterVolume.volumeMixer, true);
        SetVolume(data.settings.musicVolume.volume, data.settings.musicVolume.volumeMixer, true);
        SetVolume(data.settings.soundEffectsVolume.volume, data.settings.soundEffectsVolume.volumeMixer, true);
        SetVolume(data.settings.voiceChatVolume.volume, data.settings.voiceChatVolume.volumeMixer, true);
        SetOutputDevice(data.settings.outputDeviceIndex, true);

        SetSensitivity(data.settings.mouseSensitivity, true);
        SetSprintToggle(data.settings.sprintToggleIndex, true);
        SetInvertAxis(data.settings.invertAxisIndex, true);

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
    } //Change to quality instead of Index

    public void SetTextureQuality(int index, bool initialization = false) {
        int indexValue = qualityOptions[index].value;

        QualitySettings.globalTextureMipmapLimit = indexValue;

        if (!initialization && SaveManager.PlayerData.settings.textureQualityIndex != index) { 
            SaveManager.PlayerData.settings.textureQualityIndex = index;
            OnSettingsSaved();
        }
    } //Change to quality instead of Index

    public void SetShadowQuality(int index, bool initialization = false) {
        int indexValue = qualityOptions[index].value;

        int shadowResolution = 2048;

        float shadowDistance = 50f;
        int shadowCascadeCount = 4;
        float shadowDepthBias = 1f;
        float shadowNormalBias = 0.4f;
        bool enableSoftShadows = true;
        int shadowQualityIndex = 1;

        switch (indexValue) {
            case 0: //Ultra
                shadowResolution = 8192;
                shadowDistance = 60f;
                shadowCascadeCount = 4;
                shadowDepthBias = 0.1f;
                shadowNormalBias = .75f;
                enableSoftShadows = true;
                shadowQualityIndex = 3;
                break;
            case 1: //High
                shadowResolution = 4096;
                shadowDistance = 50f;
                shadowCascadeCount = 3;
                shadowDepthBias = .15f;
                shadowNormalBias = .625f;
                enableSoftShadows = true;
                shadowQualityIndex = 2;
                break;
            case 2: //Medium
                shadowResolution = 2048;
                shadowDistance = 40f;
                shadowCascadeCount = 2;
                shadowDepthBias = .28f;
                shadowNormalBias = .55f;
                enableSoftShadows = false;
                shadowQualityIndex = 1;
                break;
            case 3: //Low
                shadowResolution = 1024;
                shadowDistance = 20f;
                shadowCascadeCount = 1;
                shadowDepthBias = .32f;
                shadowNormalBias = .4f;
                enableSoftShadows = false;
                shadowQualityIndex = 1;
                break;
        }

        urpAsset.mainLightShadowmapResolution = shadowResolution;
        urpAsset.shadowDistance = shadowDistance;
        urpAsset.shadowCascadeCount = shadowCascadeCount;
        urpAsset.shadowDepthBias = shadowDepthBias;
        urpAsset.shadowNormalBias = shadowNormalBias;
        fiSoftShadows.SetValue(urpAsset, enableSoftShadows);
        fiSoftShadowQuality.SetValue(urpAsset, shadowQualityIndex);

        if (!initialization && SaveManager.PlayerData.settings.shadowQualityIndex != index) {
            SaveManager.PlayerData.settings.shadowQualityIndex = index;
            OnSettingsSaved();
        }
    } //Change to quality instead of Index

    public void SetLightningQuality(int index, bool initialization = false) {
        int indexValue = qualityOptions[index].value;

        //[TODO] Add this when adding volumetric lightning - Pack

        switch (indexValue)  {
            case 0: //Ultra

                break;
            case 1: //High

                break;
            case 2: //Medium

                break;
            case 3: //Low

                break;
        }

        if (!initialization && SaveManager.PlayerData.settings.lightningQualityIndex != index) {
            SaveManager.PlayerData.settings.lightningQualityIndex = index;
            OnSettingsSaved();
        }
    } //Change to quality instead of Index

    public void SetVSync(int index, bool initialization = false) {
        int indexValue = inverseEnableOptions[index].value;
        QualitySettings.vSyncCount = indexValue;

        if (!initialization && SaveManager.PlayerData.settings.vSyncEnabledIndex != index) {
            SaveManager.PlayerData.settings.vSyncEnabledIndex = index;
            OnSettingsSaved();
        }
    } //Change to bool instead of Index

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
    } //Change to bool instead of Index

    public void SetRenderDistance(int index, bool initialization = false) {
        //index = 0 minmun //index = 1 Average // index = 2 maximm 

        Camera cam = Camera.main; //[TODO] check on player spawned event
       
        float enemyDistance = 160;
        float effectsDistance = 100;
        float natureDistance = 230;
        float propsDistance = 200;

        switch (index) {
            case 0:
                enemyDistance = 160;
                effectsDistance = 100;
                natureDistance = 230;
                propsDistance = 200;
                break;
            case 1:
                enemyDistance = 310;
                effectsDistance = 200;
                natureDistance = 660;
                propsDistance = 600;
                break;
            case 2:
                enemyDistance = 500;
                effectsDistance = 400;
                natureDistance = 1500;
                propsDistance = 1200;
                break;
        }

        distances[LayerMask.NameToLayer("Enemy")] = enemyDistance;
        distances[LayerMask.NameToLayer("Effect")] = effectsDistance;
        distances[LayerMask.NameToLayer("Nature")] = natureDistance;
        distances[LayerMask.NameToLayer("Prop")] = propsDistance;

        cam.layerCullDistances = distances;

        if (!initialization && SaveManager.PlayerData.settings.renderDistanceIndex != index) {
            SaveManager.PlayerData.settings.renderDistanceIndex = index;
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
        int indexValue = inverseEnableOptions[index].value;
        QualitySettings.anisotropicFiltering = indexValue == 0 ? AnisotropicFiltering.Enable : AnisotropicFiltering.Disable;

        if (!initialization && SaveManager.PlayerData.settings.anisotropicFilteringIndex != index)  {
            SaveManager.PlayerData.settings.anisotropicFilteringIndex = index;
            OnSettingsSaved();
        }
    } //Change to bool instead of Index

    public void SetAmbientOcclusion(int index, bool initialization = false) {
        int indexValue = qualityOptions[index].value;

        float intensity = .4f;
        float radius = .1f;
        bool enabled = true;

        switch (indexValue) {
            case 0: // Ultra
                intensity = 0.825f; radius = 0.525f; enabled = true; break;
            case 1: // High
                intensity = 0.6f; radius = 0.45f; enabled = true; break;
            case 2: // Medium
                intensity = 0.3f; radius = 0.2f; enabled = true; break;
            case 3: // Low
                intensity = 0f; radius = 0f; enabled = false; break;
        }

        m_ssao.SetActive(enabled);

        var boxed = fiSettings.GetValue(m_ssao);
        fiIntensity.SetValue(boxed, intensity);
        fiRadius.SetValue(boxed, radius);

        fiSettings.SetValue(m_ssao, boxed);

        var miCreate = typeof(ScriptableRendererFeature)
            .GetMethod("Create", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        miCreate?.Invoke(m_ssao, null);

        if (!initialization && SaveManager.PlayerData.settings.ambientOcclusionIndex != index) {
            SaveManager.PlayerData.settings.ambientOcclusionIndex = index;
            OnSettingsSaved();
        }
    }  //Change to quality instead of Index

    public void SetEffectQuality(int index, bool initialization = false) {
        int indexValue = qualityOptions[index].value;

        switch (indexValue) {
            case 0: // Ultra
                GlobalEffectsQuality = Quality.Ultra;
                break;
            case 1: // High
                GlobalEffectsQuality = Quality.High;
                break;
            case 2: // Medium
                GlobalEffectsQuality = Quality.Medium;
                break;
            case 3: // Low
                GlobalEffectsQuality = Quality.Low;
                break;
        }

        Singleton.Instance.GameEvents.OnGlobalEffectsQualityChanged?.Invoke(GlobalEffectsQuality);

        if (!initialization && SaveManager.PlayerData.settings.effectsQualityIndex != index) {
            SaveManager.PlayerData.settings.effectsQualityIndex = index;
            OnSettingsSaved();
        }
    } //Change to quality instead of Index

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

    #region Post processing
    public void SetPostProcessingQuality(int index, bool initialization = false) {
        //[TODO] Add this when finishing post processing
        int indexValue = qualityOptions[index].value;

        switch (indexValue) {
            case 0: // Ultra
                
                break;
            case 1: // High
                
                break;
            case 2: // Medium
                
                break;
            case 3: // Low
                
                break;
        }

        if (!initialization && SaveManager.PlayerData.settings.postProcessingQualityIndex != index) {
            SaveManager.PlayerData.settings.motionBlurEnabled = index;
            OnSettingsSaved();
        }
    } //Change to quality instead of Index

    public void SetMotionBlur(int index, bool initialization = false) {
        motionBlur.active = index == 0;

        if (!initialization && SaveManager.PlayerData.settings.motionBlurEnabled != index) {
            SaveManager.PlayerData.settings.motionBlurEnabled = index;
            OnSettingsSaved();
        }
    }
    #endregion
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

    #region Controls
    public void SetSensitivity(float value, bool initialization = false) {
        Singleton.Instance.GameEvents.OnSensitivityChanged?.Invoke(value);

        if (!initialization && SaveManager.PlayerData.settings.mouseSensitivity != value) {
            SaveManager.PlayerData.settings.mouseSensitivity = value;
            OnSettingsSaved();
        }
    }

    public void SetSprintToggle(int index, bool initialization = false) {
        Singleton.Instance.GameEvents.OnSprintToggleChanged?.Invoke(index);
        //index == 0 - Disabled
        //index == 1 - enabled

        if (!initialization && SaveManager.PlayerData.settings.sprintToggleIndex != index) {
            SaveManager.PlayerData.settings.sprintToggleIndex = index;
            OnSettingsSaved();
        }
    }

    public void SetInvertAxis(int index, bool initialization = false) {
        Singleton.Instance.GameEvents.OnInvertAxisChanged?.Invoke(index);

        if (!initialization && SaveManager.PlayerData.settings.invertAxisIndex != index) {
            SaveManager.PlayerData.settings.invertAxisIndex = index;
            OnSettingsSaved();
        }
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

public enum Quality { Low, Medium, High, Ultra }
using UnityEngine;

public class UI_SettingsAudio : MonoBehaviour {
    private SettingsManager manager;

    [Header("UI Components")]
    [SerializeField] private UI_SliderSetting slider_masterAudioVolume;
    [SerializeField] private UI_SliderSetting slider_musicAudioVolume;
    [SerializeField] private UI_SliderSetting slider_sfxAudioVolume;
    [SerializeField] private UI_SliderSetting slider_voiceChatAudioVolume;
    [SerializeField] private UI_Setting options_voiceOutputDevice;

    private void Awake() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.AddListener(OnSettingsDataLoaded);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.RemoveListener(OnSettingsDataLoaded);

        TeardownListeners();
    }

    private void OnSettingsDataLoaded(PlayerSaveData data) {
        manager = Singleton.Instance.SettingsManager;

        slider_masterAudioVolume.Setup(new Vector2(0.0001f, 1), data.settings.masterVolume.volume, null, false);
        slider_musicAudioVolume.Setup(new Vector2(0.0001f, 1), data.settings.musicVolume.volume, null, false);
        slider_sfxAudioVolume.Setup(new Vector2(0.0001f, 1), data.settings.soundEffectsVolume.volume, null, false);
        slider_voiceChatAudioVolume.Setup(new Vector2(0.0001f, 1), data.settings.voiceChatVolume.volume, null, false);
        options_voiceOutputDevice.SetupOptions(manager.outputDevices, data.settings.outputDeviceIndex);

        SetupListeners();
    }

    private void SetupListeners() {
        slider_masterAudioVolume.AddListener(i => manager.SetVolume(i, VolumeMixer.Master));
        slider_musicAudioVolume.AddListener(i => manager.SetVolume(i, VolumeMixer.Music));
        slider_sfxAudioVolume.AddListener(i => manager.SetVolume(i, VolumeMixer.SFX));
        slider_voiceChatAudioVolume.AddListener(i => manager.SetVolume(i, VolumeMixer.VoiceChat));
        options_voiceOutputDevice.onValueChanged.AddListener(i => manager.SetOutputDevice(i));
    }

    private void TeardownListeners() {
        slider_masterAudioVolume.RemoveAllListeners();
        slider_musicAudioVolume.RemoveAllListeners();
        slider_sfxAudioVolume.RemoveAllListeners();
        slider_voiceChatAudioVolume.RemoveAllListeners();
        options_voiceOutputDevice.onValueChanged.RemoveAllListeners();
    }
}

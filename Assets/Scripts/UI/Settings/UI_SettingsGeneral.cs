using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsGeneral : MonoBehaviour {
    private SettingsManager manager;

    [Header("Screens")]
    [SerializeField] private GameObject m_defaultGeneralSettingsScreen;
    [SerializeField] private GameObject m_crosshairSettingsScreen;
    [SerializeField] private GameObject m_crosshairPreviewScreen;

    [Header("UI Components")]
    [SerializeField] private UI_Setting languageOptions;
    [SerializeField] private UI_Setting playerIndicatorOptions;
    [SerializeField] private UI_Setting hudSizeOptions;
    [SerializeField] private UI_Setting headBobOptions;
    [SerializeField] private UI_Setting damageNumbersOptions;
    [SerializeField] private UI_Setting subtitlesOptions;
    [SerializeField] private UI_Setting fontSizeOptions;
    [SerializeField] private Button m_openDefaultGeneralSettingsButton;
    [SerializeField] private Button m_openCrosshairSettingsButton;

    private ScrollRect thisSR;
    private RectTransform thisRT;

    private void Awake() {
        thisSR = GetComponent<ScrollRect>();
        thisRT = GetComponent<RectTransform>();

        SwitchBetweenScreens();

        Singleton.Instance.GameEvents.OnSettingsDataLoaded.AddListener(OnSettingsDataLoaded);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.RemoveListener(OnSettingsDataLoaded);

        TeardownListeners();
    }

    private void SwitchBetweenScreens(bool isDefaultScreen = true) {
        StartCoroutine(OnScreenSwitch(isDefaultScreen));
    }

    private IEnumerator OnScreenSwitch(bool isDefaultScreen = true) {
        m_defaultGeneralSettingsScreen.SetActive(isDefaultScreen);
        m_crosshairSettingsScreen.SetActive(!isDefaultScreen);
        m_crosshairPreviewScreen.SetActive(!isDefaultScreen);

        thisSR.verticalNormalizedPosition = 1f;

        yield return new WaitForEndOfFrame();

        LayoutRebuilder.ForceRebuildLayoutImmediate(thisRT);
    }

    private void OnSettingsDataLoaded(PlayerSaveData data) {
        manager = Singleton.Instance.SettingsManager;

        languageOptions.SetupOptions(manager.languageOptions, (int)data.settings.language);
        playerIndicatorOptions.SetupOptions(manager.playerIndicatorOptions, data.settings.playerIndicatorMode);
        hudSizeOptions.SetupOptions(manager.sizeOptions, data.settings.hudSize);
        headBobOptions.SetupOptions(manager.enableOptions, data.settings.cameraBobEnabled);
        damageNumbersOptions.SetupOptions(manager.enableOptions, data.settings.damageIndicatorEnabled);
        subtitlesOptions.SetupOptions(manager.subtitlesOptions, data.settings.subtitleType);
        fontSizeOptions.SetupOptions(manager.sizeOptions, data.settings.fontSize);

        SetupListeners();
    }

    private void SetupListeners() {
        languageOptions.onValueChanged.AddListener(i => manager.SetLanguage(i));
        playerIndicatorOptions.onValueChanged.AddListener(i => manager.SetPlayerIndicatorMode(i));
        hudSizeOptions.onValueChanged.AddListener(i => manager.SetHUDSize(i));
        headBobOptions.onValueChanged.AddListener(i => manager.SetCameraBobEnabled(i));
        damageNumbersOptions.onValueChanged.AddListener(i => manager.SetDamageNumbersEnabled(i));
        subtitlesOptions.onValueChanged.AddListener(i => manager.SetSubtitleType(i));
        fontSizeOptions.onValueChanged.AddListener(i => manager.SetFontSize(i));
        
        m_openDefaultGeneralSettingsButton.onClick.AddListener(() => SwitchBetweenScreens(true));
        m_openCrosshairSettingsButton.onClick.AddListener(() => SwitchBetweenScreens(false));
    }

    private void TeardownListeners() {
        languageOptions.onValueChanged.RemoveAllListeners();
        playerIndicatorOptions.onValueChanged.RemoveAllListeners();
        hudSizeOptions.onValueChanged.RemoveAllListeners();
        headBobOptions.onValueChanged.RemoveAllListeners();
        damageNumbersOptions.onValueChanged.RemoveAllListeners();
        subtitlesOptions.onValueChanged.RemoveAllListeners();
        fontSizeOptions.onValueChanged.RemoveAllListeners();

        m_openDefaultGeneralSettingsButton.onClick.RemoveAllListeners();
        m_openCrosshairSettingsButton.onClick.RemoveAllListeners();
    }
}

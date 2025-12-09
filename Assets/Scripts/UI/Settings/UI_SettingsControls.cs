using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsControls : MonoBehaviour {
    private SettingsManager manager;

    [Header("UI Components")]
    [SerializeField] private UI_SliderSetting sensitivitySlider;
    [SerializeField] private UI_Setting sprintToggleOptions;
    [SerializeField] private UI_Setting invertAxisOptions;

    [Header("Controls rebind")]
    [SerializeField] private UI_KeyBinder Prefab_keyBinder;
    [SerializeField] private Transform m_keyRebinderHolder;
    [SerializeField] private Button m_resetToDefaultButton;

    [Header("UI")]
    [SerializeField] private PopUp m_popUp;
    [SerializeField] private Countdown m_countdown;

    private List<UI_KeyBinder> allBinders = new List<UI_KeyBinder>();

    private void Awake() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.AddListener(OnSettingsDataLoaded);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnSettingsDataLoaded.RemoveListener(OnSettingsDataLoaded);

        TeardownListeners();
    }

    public PopUp GetPopUp() => m_popUp;

    public Countdown GetCountdown() => m_countdown;

    public List<UI_KeyBinder> GetAllBinders() => allBinders;

    private void OnSettingsDataLoaded(PlayerSaveData data) {
        manager = Singleton.Instance.SettingsManager;

        KeyBind[] allKeys = manager.GetAllKeys();

        for (int i = 0; i < allKeys.Length; i++) {
            UI_KeyBinder newKey = Instantiate(Prefab_keyBinder, m_keyRebinderHolder);
            newKey.Setup(allKeys[i], this);

            allBinders.Add(newKey);
        }

        Singleton.Instance.RebindManager.OnKeyBinderListSet(allBinders);

        sensitivitySlider.Setup(new Vector2(0.01f, 18f), data.settings.mouseSensitivity, null, false, true);
        invertAxisOptions.SetupOptions(manager.enableOptions, data.settings.invertAxisIndex);
        sprintToggleOptions.SetupOptions(manager.enableOptions, data.settings.sprintToggleIndex);

        m_resetToDefaultButton.transform.parent.SetAsLastSibling();

        SetupListeners();
    }

    private void SetupListeners() {
        sensitivitySlider.AddListener(i => manager.SetSensitivity(i));
        invertAxisOptions.onValueChanged.AddListener(i => manager.SetInvertAxis(i));
        sprintToggleOptions.onValueChanged.AddListener(i => manager.SetSprintToggle(i));
        m_resetToDefaultButton.onClick.AddListener(SetupResetToDefaultPopUp);
    }

    private void TeardownListeners() {
        sensitivitySlider.RemoveAllListeners();
        invertAxisOptions.onValueChanged.RemoveAllListeners();
        sprintToggleOptions.onValueChanged.RemoveAllListeners();
        m_resetToDefaultButton.onClick.RemoveListener(SetupResetToDefaultPopUp);
    }

    public void RefreshConflictsUI() {
        //bool onConflicted = false;

        

        /*InputSystem_Actions actionAsset = Singleton.Instance.RebindManager.GetInputAsset();
        var conflicts = RebindUtils.FindConflictingBindings(actionAsset);
        var map = actionAsset.asset.FindActionMap("Player");

        for (int i = 0; i < map.actions.Count; i++) {
            if (UI_ActionList.ContainsKey(map.actions[i]))
            {
                bool isConflict = conflicts.Contains((map.actions[i], i));

                UI_ActionList[map.actions[i]].CheckBindConflict(isConflict);

                if (isConflict) onConflicted = true;
            }
        }

        if (onConflicted) {
            m_popUp.Setup("Warning", "One or more keys are being overwitten, it can cause gameplay issues!", "Okay", "", null, null);
            m_popUp.OpenPopUp();
        }*/
    }

    private void SetupResetToDefaultPopUp() {
        m_popUp.Setup("Warning", "Are you sure you want to reset all inputs to it's default?", 
            "Yes", "No", Singleton.Instance.RebindManager.ResetToDefault, null);
        m_popUp.OpenPopUp();
    }
}

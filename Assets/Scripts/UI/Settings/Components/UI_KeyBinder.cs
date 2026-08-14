using DG.Tweening;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UI_KeyBinder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("UI")]
    [SerializeField] private TMP_Text m_keyBindName;
    [SerializeField] private TMP_Text m_keyActionName;
    [SerializeField] private Button m_rebindPopUp;
    [SerializeField] private GameObject m_modifiedIcon;

    private const string PopUpTitle = "Press a new button";
    private const string PopUpBody = "Select new key for {0} \nPress 'Escape' to cancel";

    private CanvasGroup thisCanvasGroup;

    private PopUp popUp;
    private Countdown countdown;

    private string bodyText;
    private KeyBind key;

    #region Setup
    public void Setup(KeyBind key, UI_SettingsControls settingsControl) {
        Singleton.Instance.GameEvents.OnBindsUpdated.AddListener(UpdateBinds);

        this.key = key;

        thisCanvasGroup = GetComponent<CanvasGroup>();
        popUp = settingsControl.GetPopUp();
        countdown = settingsControl.GetCountdown();

        thisCanvasGroup.alpha = 0.9f;

        bodyText = string.Format(PopUpBody, key.m_actionName);

        UpdateBinds(false);
    }

    private void OnDestroy() {
        m_rebindPopUp.onClick.RemoveAllListeners();
        Singleton.Instance.GameEvents.OnBindsUpdated.RemoveListener(UpdateBinds);
    }

    public KeyBind GetKey() => key;

    private string NormalizePath(string path) {
        if (string.IsNullOrEmpty(path)) return "";
        if (path.StartsWith("/")) path = path.Substring(1);
        return path.Replace("<", "").Replace(">", "");
    }

    private string GetBindingName() {
        string mapAction = key.m_actionReference.action.actionMap.name + "/" + key.m_actionReference.action.name;
        var action = Singleton.Instance.RebindManager.GetInputAction(mapAction);
        if (action == null) {
            Debug.LogError($"Runtime action not found for {mapAction}");
            return "Error";
        }

        var bindingPath = action.bindings[0].effectivePath;
        if (string.IsNullOrEmpty(bindingPath)) return "Unbound";

        var control = InputSystem.FindControls<InputControl>(bindingPath).FirstOrDefault();
        if (control == null) return "Unbound";

        string display = control.device is Mouse ? control.shortDisplayName : control.displayName;
        Debug.Log($"GetBindingName for {key.m_actionName}: path={bindingPath}, display={display}");
        return display;
    }
    #endregion

    #region Main
    private void UpdateBinds(bool resetToDefault) {
        m_rebindPopUp.onClick.RemoveAllListeners();
        m_rebindPopUp.onClick.AddListener(OnRebindButtonClick);

        string bindingName = GetBindingName();
        Debug.Log($"Updating bind for {key.m_actionName}: {bindingName} (reset: {resetToDefault})");
        m_keyActionName.SetText(key.m_actionName);
        UpdateKeyDisplay();
    }

    private void OnRebindButtonClick() {
        popUp.Setup(PopUpTitle, bodyText, "", "", null, null, true, true);
        popUp.OpenPopUp();

        float time = 7;

        countdown.SetCountdown(time);

        Singleton.Instance.RebindManager.StartRebinding(this, OpenConflictsPopUp, UpdateKeyDisplay, popUp.ClosePopUp, time);
        Singleton.Instance.GameEvents.LockNavigationInputs?.Invoke(true);
    }

    private void OpenConflictsPopUp() {
        string conflictDetails = "";
        if (Singleton.Instance.RebindManager.conflictingBindings.Count > 0) {
            var conflicts = Singleton.Instance.RebindManager.conflictingBindings
                .Select(c => $"{c.action.name} (current key: {c.displayName})")
                .ToList();
            conflictDetails = $"The key is already bound to: {string.Join(", ", conflicts)}. Overwrite will swap (if single) or unbind the conflicting keys.";
        }
        else
            conflictDetails = "One or more keys are being overwritten, it can cause gameplay issues!";

        popUp.Setup("Warning", conflictDetails, "Overwrite", "Cancel",
            () => { Singleton.Instance.RebindManager.ForceRebind(); },
            () => {
                Singleton.Instance.RebindManager.CancelRebind("Key not overwritten");
            });
        popUp.OpenPopUp();
    }

    public void UpdateKeyDisplay() {
        string mapAction = key.m_actionReference.action.actionMap.name + "/" + key.m_actionReference.action.name;
        var action = Singleton.Instance.RebindManager.GetInputAction(mapAction);
        if (action == null) {
            Debug.LogError($"Runtime action not found for {mapAction}");
            m_keyBindName.SetText("Error");
            m_keyBindName.color = Color.red;
            return;
        }

        string currentPath = action.bindings[0].effectivePath;
        string defaultPath = key.m_actionReference.action.bindings[0].path;

        string display = GetBindingName();
        m_keyBindName.SetText(display);

        if (display == "Unbound") {
            m_keyBindName.color = Color.red;
            return;
        }

        bool isDefault = NormalizePath(currentPath) == NormalizePath(defaultPath);
        m_modifiedIcon.SetActive(!isDefault);
    }
    #endregion

    #region Mouse events
    public void OnPointerEnter(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(1, 0.25f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(0.9f, 0.25f);
    }
    #endregion
}
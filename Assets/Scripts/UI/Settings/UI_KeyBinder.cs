using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_KeyBinder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [Header("UI")]
    [SerializeField] private TMP_Text m_keyBindName;
    [SerializeField] private TMP_Text m_keyActionName;
    [SerializeField] private Button m_rebindPopUp;

    [Header("Components")]
    [SerializeField] private CanvasGroup thisCanvasGroup;

    public const string PopUpTitle = "Press a new button";
    public const string PopUpBody = "Select new key for {0} \nPress 'Escape' to cancel";

    private PopUp popUp;
    private UI_SettingsControls settingsControl;

    private string bodyText;
    private KeyBind key;

    public void Setup(KeyBind key, PopUp popUp, UI_SettingsControls settingsControl) {
        Singleton.Instance.GameEvents.OnBindsUpdated.AddListener(UpdateBinds);

        this.settingsControl = settingsControl;
        this.popUp = popUp;
        this.key = key;

        bodyText = string.Format(PopUpBody, key.m_actionName);

        UpdateBinds();
    }

    private void OnDestroy() {
        m_rebindPopUp.onClick.RemoveAllListeners();
        Singleton.Instance.GameEvents.OnBindsUpdated.RemoveListener(UpdateBinds);
    }

    private void UpdateBinds() {
        m_keyActionName.SetText(key.m_actionName);
        m_keyBindName.SetText(key.m_actionReference.action.GetBindingDisplayString());

        m_rebindPopUp.onClick.RemoveAllListeners();
        m_rebindPopUp.onClick.AddListener(OnRebindButtonClick);
    }

    public void CheckBindConflict(bool isConflicted) {
        m_keyBindName.color = isConflicted ? Color.red : Color.white;
    }

    private void OnRebindButtonClick() {
        popUp.Setup(PopUpTitle, bodyText, "", "", null, null);
        popUp.OpenPopUp();

        Singleton.Instance.RebindManager.StartRebinding(
            key, OnRebound, () => popUp.ClosePopUp(),
            settingsControl.RefreshConflictsUI);
    }

    private void OnRebound(string keyName) {
        m_keyBindName.SetText(keyName);
        popUp.ClosePopUp();
    }

    public void OnPointerEnter(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(1, 0.25f);
    }

    public void OnPointerExit(PointerEventData eventData) {
        thisCanvasGroup.DOKill();
        thisCanvasGroup.DOFade(0, 0.25f);
    }
}

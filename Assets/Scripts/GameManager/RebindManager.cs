using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class RebindManager : MonoBehaviour {
    private InputSystem_Actions inputActions;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    private InputAction currentAction;
    private InputControl currentBinding;
    private int currentBindingIndex;

    public bool OnRebinding { get; private set; }

    private const string KeyboardExclude = "Keyboard/escape";
    private const string AnyKeyExclude = "Keyboard/anyKey";

    private UnityAction currentOnRebinded;
    private UnityAction currentOnComplete;

    private string oldBindingPath;
    public List<(InputAction action, int index, string displayName)> conflictingBindings { get; private set; }

    private List<UI_KeyBinder> allBinders;

    #region Initialization and misc
    private void Awake() {
        Singleton.Instance.GameEvents.OnDataLoaded.AddListener(LoadBinds);
        Singleton.Instance.GameEvents.OnPlayerInputLoaded.AddListener(OnPlayerInputLoaded);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnDataLoaded.RemoveListener(LoadBinds);
        Singleton.Instance.GameEvents.OnPlayerInputLoaded.RemoveListener(OnPlayerInputLoaded);
    }

    private void OnPlayerInputLoaded(InputSystem_Actions input) {
        inputActions = input;
    }

    public void OnKeyBinderListSet(List<UI_KeyBinder> allBinders) {
        this.allBinders = allBinders;
    }    

    public InputAction GetInputAction(string mapActionName) {
        return inputActions?.FindAction(mapActionName);
    }

    private string NormalizePath(string path) {
        if (string.IsNullOrEmpty(path)) return "";
        if (path.StartsWith("/")) path = path.Substring(1);
        return path.Replace("<", "").Replace(">", "");
    }

    private UI_KeyBinder FindKeyBinderForAction(string actionName) {
        return allBinders.FirstOrDefault(b => b.GetKey().m_actionReference.action.name == actionName);
    }

    private void Print(string message, string color = "white") { Debug.Log($"<color={color}>{message}</color>"); }
    #endregion

    #region Main
    public void StartRebinding(UI_KeyBinder keyBinder, UnityAction onKeyAlreadyBound, UnityAction onRebinded, UnityAction onComplete, float time) {
        KeyBind keyBind = keyBinder.GetKey();
        var action = inputActions.FindAction($"{keyBind.m_actionReference.action.actionMap.name}/{keyBind.m_actionReference.action.name}", true);

        currentOnRebinded = onRebinded;
        currentOnComplete = onComplete;

        OnRebinding = true;

        rebindingOperation?.Cancel();
        rebindingOperation?.Dispose();

        action.Disable();

        Print("Waiting player input...", "green");

        int bindingIndex = 0;

        oldBindingPath = action.bindings[bindingIndex].effectivePath;
        conflictingBindings = new List<(InputAction, int, string)>();

        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough(KeyboardExclude)
            .WithControlsExcluding(AnyKeyExclude)
            .WithTimeout(time)
            .OnMatchWaitForAnother(.1f)
            .OnComplete(operation => {
                currentBinding = operation.selectedControl;
                currentAction = action;
                currentBindingIndex = bindingIndex;

                operation.Dispose();

                if (CheckConflicts()) {
                    onKeyAlreadyBound?.Invoke();
                    currentAction.RemoveBindingOverride(currentBindingIndex);
                    return;
                }

                currentAction.ApplyBindingOverride(currentBindingIndex, currentBinding.path);
                SaveBinds();

                Print("New key binded: " + currentBinding.displayName, "green");

                onRebinded?.Invoke();

                OnOperationComplete(onComplete);
            }).OnCancel(operation => {
                currentAction = action;
                operation.Dispose();

                CancelRebind("Key not changed, keeping the original: " + action.bindings[bindingIndex].effectivePath,
                    onComplete);
            })
            .Start();
    }

    public void CancelRebind(string reason, UnityAction onComplete = default) {
        currentAction.RemoveBindingOverride(currentBindingIndex);
        OnOperationComplete(onComplete);
        Print(reason, "red");

        conflictingBindings.Clear();
        oldBindingPath = null;
        currentOnRebinded = null;
        currentOnComplete = null;
    }

    private void OnOperationComplete(UnityAction onComplete) {
        currentAction.Enable();
        inputActions.Enable();

        OnRebinding = false;
        rebindingOperation = null;

        Singleton.Instance.GameEvents.LockNavigationInputs?.Invoke(false);

        onComplete?.Invoke();
    }

    public void ForceRebind() {
        if (conflictingBindings.Count == 0) return;

        foreach (var (otherAction, i, _) in conflictingBindings) {
            if (conflictingBindings.Count == 1 && !string.IsNullOrEmpty(oldBindingPath)) {
                otherAction.ApplyBindingOverride(i, oldBindingPath);
                Print($"Swapped binding on {otherAction.name} to {oldBindingPath}", "yellow");
            }
            else {
                otherAction.ApplyBindingOverride(i, "");
                Print($"Unbound conflicting binding on {otherAction.name} at index {i}", "yellow");
            }

            var conflictBinder = FindKeyBinderForAction(otherAction.name);
            if (conflictBinder != null) {
                conflictBinder.UpdateKeyDisplay();
                Print($"Manually updated UI for conflicting action {otherAction.name}", "blue");
            }
        }

        currentAction.ApplyBindingOverride(currentBindingIndex, currentBinding.path);
        SaveBinds();

        currentOnRebinded?.Invoke();

        Print($"Key overwritten with {currentBinding.displayName}", "green");

        OnOperationComplete(currentOnComplete);

        conflictingBindings.Clear();
        oldBindingPath = null;
        currentOnRebinded = null;
        currentOnComplete = null;
    }

    private bool CheckConflicts() {
        conflictingBindings.Clear();
        var rebindableActions = new HashSet<string>(allBinders.Select(b => b.GetKey().m_actionReference.action.name));

        foreach (var map in inputActions.asset.actionMaps) {
            foreach (var otherAction in map.actions) {
                if (!rebindableActions.Contains(otherAction.name)) continue;

                for (int i = 0; i < otherAction.bindings.Count; i++) {
                    string effPath = otherAction.bindings[i].effectivePath;
                    if (string.IsNullOrEmpty(effPath)) continue;

                    if (otherAction == currentAction && i == currentBindingIndex) continue;

                    if (NormalizePath(effPath) == NormalizePath(currentBinding.path)) {
                        var control = InputSystem.FindControls<InputControl>(effPath).FirstOrDefault();
                        string displayName = control == null ? "Unknown" : (control.device is Mouse ? control.shortDisplayName : control.displayName);
                        conflictingBindings.Add((otherAction, i, displayName));
                        Print($"Conflict: {currentBinding.path} already bound at {otherAction.name} (current key: {displayName})", "red");
                    }
                }
            }
        }
        return conflictingBindings.Count > 0;
    }
    #endregion

    #region Data management
    public void ResetToDefault() {
        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;
        data.settings.savedBinds = string.Empty;

        SaveSystemHandler.SaveData(data);

        inputActions.RemoveAllBindingOverrides();
        Singleton.Instance.GameEvents.OnBindsUpdated?.Invoke(true);
    }

    private void SaveBinds() {
        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;
        data.settings.savedBinds = inputActions.SaveBindingOverridesAsJson();

        SaveSystemHandler.SaveData(data);
    }

    private void LoadBinds(PlayerSaveData data) {
        string rebinds = data.settings.savedBinds;
        if (string.IsNullOrEmpty(rebinds)) return;

        inputActions.LoadBindingOverridesFromJson(rebinds);
        Singleton.Instance.GameEvents.OnBindsUpdated?.Invoke(false);
    }
    #endregion
}

[System.Serializable]
public struct KeyBind { 
    public string m_actionName;
    public InputActionReference m_actionReference;
}
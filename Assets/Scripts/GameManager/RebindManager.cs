using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RebindManager : MonoBehaviour {
    private InputSystem_Actions inputActions;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

    #region Initialization
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

    public InputSystem_Actions GetInputAsset() => inputActions;
    #endregion

    public void StartRebinding(KeyBind keyBind, UnityAction<string> onRebinded, UnityAction onCancel, UnityAction onKeyAlreadyBound) {
        var action = inputActions.FindAction("Player/" + keyBind.m_actionReference.action.name, true);

        rebindingOperation?.Cancel();
        rebindingOperation?.Dispose();

        action.Disable();

        print("Waiting player input...");

        int bindingIndex = 0;

        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("Keyboard/escape")
            .WithControlsExcluding("Mouse")
            .WithControlsExcluding("Keyboard/anyKey")
            .OnMatchWaitForAnother(.1f)
            .OnComplete(operation =>
            {
                var newBinding = operation.selectedControl;

                if (IsKeyAlreadyBound(newBinding.path, action, bindingIndex)) {
                    onKeyAlreadyBound?.Invoke();
                    return;
                }

                action.ApplyBindingOverride(bindingIndex, newBinding.path);

                OnOperationComplete(action, operation, "New key binded: " + newBinding.name);
                onRebinded?.Invoke(newBinding.displayName);
            }).OnCancel(operation => {
                OnOperationComplete(action, operation, "Key not changed, keeping the original: " + action.bindings[bindingIndex].effectivePath);
                onCancel?.Invoke();
            })
            .Start();
    }

    [Button]
    public void ResetToDefault() {
        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;
        data.settings.savedBinds = string.Empty;

        SaveSystemHandler.SaveData(data);

        inputActions.RemoveAllBindingOverrides();
        Singleton.Instance.GameEvents.OnBindsUpdated?.Invoke();
    }

    private void OnOperationComplete(InputAction action, InputActionRebindingExtensions.RebindingOperation operation, string message) {
        operation.Dispose();
        action.Enable();

        inputActions.Enable();

        print(message);

        rebindingOperation = null;
    }

    bool IsKeyAlreadyBound(string newPath, InputAction ignoreAction = null, int ignoreBindingIndex = -1) {
        var map = inputActions.asset.FindActionMap("Player");

        foreach (var action in map.actions)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action == ignoreAction && i == ignoreBindingIndex)
                    continue;

                if (action.bindings[i].effectivePath == newPath)
                    return true;
            }
        }
        return false;
    }

    #region Data management
    private void SaveBinds() {
        PlayerSaveData data = Singleton.Instance.SaveManager.PlayerData;
        data.settings.savedBinds = inputActions.SaveBindingOverridesAsJson();

        SaveSystemHandler.SaveData(data);
    }

    private void LoadBinds(PlayerSaveData data) {
        string rebinds = data.settings.savedBinds;
        if (string.IsNullOrEmpty(rebinds)) return;

        inputActions.LoadBindingOverridesFromJson(rebinds);
    }
    #endregion
}

[System.Serializable]
public struct KeyBind { 
    public string m_actionName;
    public InputActionReference m_actionReference;
}
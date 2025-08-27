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
    #endregion

    public void StartRebinding(KeyBind keyBind, UnityAction<string> onRebinded, UnityAction onCancel, UnityAction onKeyAlreadyBound, float time) {
        var action = inputActions.FindAction("Player/" + keyBind.m_actionReference.action.name, true);

        rebindingOperation?.Cancel();
        rebindingOperation?.Dispose();

        action.Disable();

        print("Waiting player input...");

        int bindingIndex = 0;

        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .WithCancelingThrough("Keyboard/escape")
            .WithControlsExcluding("Keyboard/anyKey")
            .WithTimeout(time)
            .OnMatchWaitForAnother(.1f)
            .OnComplete(operation => {
                var newBinding = operation.selectedControl;

                //Adicionar um return e abrir um pop up se a tecla ja existir nas ações

                action.ApplyBindingOverride(bindingIndex, newBinding.path);

                SaveBinds();

                OnOperationComplete(action, operation, "New key binded: " + newBinding.name);
                onRebinded?.Invoke(GetBindingName(newBinding));
            }).OnCancel(operation => {
                OnOperationComplete(action, operation, "Key not changed, keeping the original: " + action.bindings[bindingIndex].effectivePath);
                onCancel?.Invoke();
            })
            .Start();
    }

    private string GetBindingName(InputControl control) {
        if (control.device is Mouse) 
            return control.shortDisplayName;

        if (control.device is Keyboard) 
            return control.displayName;

        return string.IsNullOrEmpty(control.shortDisplayName)
            ? control.displayName
            : control.shortDisplayName;
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

    /// <summary>
    /// [TODO] Update method
    /// Prompt: Tenho essa função para rebindar teclas no meu jogo Unity:
    ///     copiar func StartRebinding
    ///     
    /// Porém preciso que quando o jogador pressionar uma tecla que já esta bindada em outra ação,
    /// ele retorne e não binde a tecla, porém de a opção de dar overwrite naquela tecla, mudando também, a outra ação
    ///
    /// </summary>
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
    }  //[TODO] Update method

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
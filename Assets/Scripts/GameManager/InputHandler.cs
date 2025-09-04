using UnityEngine;

public class InputHandler : MonoBehaviour {
    private InputSystem_Actions Input;

    public bool Pause { get; private set; }
    public bool Inventory { get; private set; }
    public bool NextTab { get; private set; }
    public bool PreviousTab { get; private set; }
    public bool SkipDialogue { get; private set; }

    private void OnEnable() {
        if (Input == null) AddInputListeners();        
    }

    private void OnDisable() {
        if (Input != null) RemoveInputListeners();        
    }

    private void AddInputListeners() {
        Input = new InputSystem_Actions();

        Input.Menu.Pause.performed += i => Pause = true;
        Input.Menu.Inventory.performed += i => Inventory = true;
        Input.UI.NextTab.performed += i => NextTab = true;
        Input.UI.PreviousTab.performed += i => PreviousTab = true;
        Input.Menu.SkipDialogue.performed += i => SkipDialogue = true;

        Input.Enable();
    }

    private void RemoveInputListeners() {
        Input.Menu.Pause.performed -= i => Pause = true;
        Input.Menu.Inventory.performed -= i => Inventory = true;
        Input.UI.NextTab.performed -= i => NextTab = true;
        Input.UI.PreviousTab.performed -= i => PreviousTab = true;
        Input.Menu.SkipDialogue.performed -= i => SkipDialogue = true;

        Input.Disable();
        Input = null;
    }

    private void LateUpdate() {
        Pause = false;
        Inventory = false;
        NextTab = false;
        PreviousTab = false;
        SkipDialogue = false;
    }
}

using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_InputHandler : NetworkBehaviour {
    private InputSystem_Actions Input;

    [Header("Output")]
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool Sprint { get; private set; }
    public bool Jump { get; private set; }
    public bool Crouch { get; private set; }
    public bool JumpHold { get; private set; }
    public bool Zoom { get; private set; }
    public bool Attack { get; private set; }
    public bool Reload { get; private set; }
    public bool Interact { get; private set; }
    public bool Drop { get; private set; }
    public bool Slot1 { get; private set; }
    public bool Slot2 { get; private set; }
    public bool Slot3 { get; private set; }
    public bool Slot4 { get; private set; }
    public bool LastSlotUsed { get; private set; }


    private void OnEnable() {
        if (Input == null) {
            AddInputListeners();
        }        
    }

    private void OnDisable() {
        if (Input != null) {
            RemoveInputListeners();
        }
    }

    private void AddInputListeners() {
        Input = new InputSystem_Actions();

        Input.Player.Move.performed += OnMove;
        Input.Player.Move.canceled += OnMove;

        Input.Player.Look.performed += OnLook;
        Input.Player.Look.canceled += OnLook;

        Input.Player.Sprint.performed += i => Sprint = true;
        Input.Player.Sprint.canceled += i => Sprint = false;

        Input.Player.Zoom.performed += i => Zoom = true;
        Input.Player.Zoom.canceled += i => Zoom = false;

        Input.Player.Crouch.performed += i => Crouch = true;
        Input.Player.Crouch.canceled += i => Crouch = false;

        Input.Player.Jump.performed += i => JumpHold = true;
        Input.Player.Jump.canceled += i => JumpHold = false; 

        Input.Player.Reload.performed += i => Reload = true;

        Input.Player.Jump.performed += i => Jump = true;

        Input.Player.Attack.performed += i => Attack = true;

        Input.Player.Interact.performed += i => Interact = true;

        Input.Player.Drop.performed += i => Drop = true;

        Input.Player.Slot1.performed += i => Slot1 = true;
        Input.Player.Slot2.performed += i => Slot2 = true;
        Input.Player.Slot3.performed += i => Slot3 = true;
        Input.Player.Slot4.performed += i => Slot4 = true;
        Input.Player.LastSlotUsed.performed += i => LastSlotUsed = true;

        Input.Enable();
    }

    private void RemoveInputListeners() {
        Input.Player.Move.performed -= OnMove;
        Input.Player.Move.canceled -= OnMove;

        Input.Player.Look.performed -= OnLook;
        Input.Player.Look.canceled -= OnLook;

        Input.Player.Sprint.performed -= i => Sprint = true;
        Input.Player.Sprint.canceled -= i => Sprint = false;

        Input.Player.Zoom.performed -= i => Zoom = true;
        Input.Player.Zoom.canceled -= i => Zoom = false;

        Input.Player.Crouch.performed -= i => Crouch = true;
        Input.Player.Crouch.canceled -= i => Crouch = false;

        Input.Player.Jump.performed -= i => JumpHold = true;
        Input.Player.Jump.canceled -= i => JumpHold = false;

        Input.Player.Reload.performed -= i => Reload = true;

        Input.Player.Jump.performed -= i => Jump = true;

        Input.Player.Attack.performed -= i => Attack = true;

        Input.Player.Interact.performed -= i => Interact = true;

        Input.Player.Drop.performed -= i => Drop = true;

        Input.Player.Slot1.performed -= i => Slot1 = true;
        Input.Player.Slot2.performed -= i => Slot2 = true;
        Input.Player.Slot3.performed -= i => Slot3 = true;
        Input.Player.Slot4.performed -= i => Slot4 = true;
        Input.Player.LastSlotUsed.performed -= i => LastSlotUsed = true;

        Input.Disable();
        Input = null;
    }

    private void OnMove(InputAction.CallbackContext context) { MoveInput = context.ReadValue<Vector2>(); }

    private void OnLook(InputAction.CallbackContext context) { LookInput = context.ReadValue<Vector2>(); }

    private void LateUpdate() {
        if (!IsOwner) return;

        Jump = false;
        Attack = false;
        Slot1 = false;
        Slot2 = false;
        Slot3 = false;
        Slot4 = false;
        LastSlotUsed = false;
        Interact = false;
        Drop = false;
        Reload = false;
    }
}

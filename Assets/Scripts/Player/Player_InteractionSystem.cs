using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_InteractionSystem : NetworkBehaviour {
    private Player_InputHandler Input;
    private Player_HealthSystem HealthSystem;
    private Player_InventorySystem InventorySystem;

    [SerializeField] private Transform m_playerCamera;
    [SerializeField] private float m_interactionRadius;
    [SerializeField] private float m_interactionDistance;

    #region Private variables 
    private IInteractable _actualInteractable;
    private IInteractable _lastInteractable;
    private RaycastHit _hit;
    private Vector3 _target;

    private int _lastSelectedSlotIndex;
    private int _lastSlot;
    private float _slotSelectionCooldown;

    private bool[] _occupiedSlots;
    #endregion

    #region Public variables
    public int ActualSlotSelected { get; private set; }
    #endregion

    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        HealthSystem = GetComponent<Player_HealthSystem>();
        InventorySystem = GetComponent<Player_InventorySystem>();
        _occupiedSlots = new bool[UI_PlayerHUD.GetSlotsCount()];  
    }

    public override void OnDestroy() {
        _occupiedSlots = null;
    }
    #endregion

    public List<Item_SO> GetPlayerInventory() => InventorySystem.Inventory();

    #region Network Initialization
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSlotItemCollected.AddListener(OnSlotItemCollected);
            Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnRespawn);
        }
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSlotItemCollected.RemoveListener(OnSlotItemCollected);
            Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnRespawn);
        }
    }
    #endregion

    #region Object detection variables
    Collider[] result;
    IInteractable newInteractable;

    Collider nearestInteractionObject;
    float shortestDistanceBetweenObjects;
    float distanceBetweenObjects;
    #endregion

    #region Object detection
    private void DetectInteractable() {
        _target = m_playerCamera.position + (m_playerCamera.forward * m_interactionDistance);
        if (Physics.Raycast(m_playerCamera.position, m_playerCamera.forward, out _hit, m_interactionDistance))
            _target = _hit.point;        
        result = Physics.OverlapSphere(_target, m_interactionRadius);

        newInteractable = result.Length > 0 ? NearestObject(result, _target).GetComponent<IInteractable>() : null;

        if (_lastInteractable != null && !_lastInteractable.Equals(null) && _lastInteractable != newInteractable) {
            _lastInteractable.OnHoverOverItem(false);
        }

        _actualInteractable = newInteractable;

        if (_actualInteractable != _lastInteractable) {
            _actualInteractable?.OnHoverOverItem(true);
            _lastInteractable = _actualInteractable;
        }
    }

    private Collider NearestObject(Collider[] colliders, Vector3 hit) {
        nearestInteractionObject = null;
        shortestDistanceBetweenObjects = Mathf.Infinity;
        foreach (Collider col in colliders) {
            distanceBetweenObjects = Vector3.Distance(hit, col.transform.position);
            if (distanceBetweenObjects < shortestDistanceBetweenObjects) {
                shortestDistanceBetweenObjects = distanceBetweenObjects;
                nearestInteractionObject = col;
            }
        }
        return nearestInteractionObject;
    }
    #endregion

    private void HandleInteract() {
        if (Input.Interact && _actualInteractable != null && !_occupiedSlots[ActualSlotSelected]) {
            _actualInteractable.Interact(this);
        }
    }

    private void HandleItemDrop() {
        if (Input.Drop && _occupiedSlots[ActualSlotSelected]) {
            _occupiedSlots[ActualSlotSelected] = false;

            Singleton.Instance.GameEvents.OnSlotItemDropped?.Invoke(ActualSlotSelected);
        }
    }

    private void HandleSlotSelection() {
        if (Time.time < _slotSelectionCooldown) return;

        if (Input.Slot1) ActualSlotSelected = 0;
        if (Input.Slot2) ActualSlotSelected = 1;
        if (Input.Slot3) ActualSlotSelected = 2;
        if (Input.Slot4) ActualSlotSelected = 3;
        if (Input.LastSlotUsed) ActualSlotSelected = _lastSlot;

        SelectSlot(ActualSlotSelected);
    }

    private void SelectSlot(int index) {
        if (index == _lastSelectedSlotIndex) return;
        OnSlotSelected(index);
    }

    private void OnSlotSelected(int index) { //[TODO] Call it and Handle on load, the slot the player was using when quit
        _slotSelectionCooldown = Time.time + 0.025f;
        _lastSlot = _lastSelectedSlotIndex;
        _lastSelectedSlotIndex = index;

        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(index);
    }

    private void OnRespawn() {
        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(ActualSlotSelected);
    }

    private void OnSlotItemCollected(Item_SO item, int index) {
        _occupiedSlots[index] = true;
    }

    public Vector3 GetTargetAim() => _target;

    private void Update() {
        if (!IsOwner || HealthSystem.IsDead) return;
       
        DetectInteractable();
        HandleItemDrop();
        HandleSlotSelection();
        HandleInteract();
    }

    private void OnDrawGizmos() {
        Gizmos.DrawSphere(_target, m_interactionRadius);
        Gizmos.color = _actualInteractable == null ? Color.white : Color.green;
    }
}

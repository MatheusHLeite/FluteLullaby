using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Player_InteractionSystem : NetworkBehaviour {
    private Player_InputHandler Input;
    private Player_HealthSystem HealthSystem;
    private Player_CombatSystem CombatSystem;
    private Player_AnimationSystem AnimatorSystem;

    [Header("References")]
    [SerializeField] private Transform m_playerCamera;
    [SerializeField] private Transform m_rightHand;

    [Header("Setup")]
    [SerializeField] private float m_interactionRadius;
    [SerializeField] private float m_interactionDistance;
    [SerializeField] private LayerMask m_interactionLayer;

    public static readonly Dictionary<ulong, Player_InteractionSystem> Players = new();

    #region Private variables 
    private IInteractable _actualInteractable;
    private IInteractable _lastInteractable;
    private RaycastHit _hit;
    private Vector3 _target;

    private int _lastSelectedSlotIndex;
    private int _lastSlot;
    private float _slotSelectionCooldown;
    #endregion

    #region Public variables
    public int ActualSlotSelected { get; private set; }
    public Transform GetRightPlayerHand => m_rightHand;
    public Vector3 GetTargetAim() => _target;
    #endregion

    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        HealthSystem = GetComponent<Player_HealthSystem>();
        CombatSystem = GetComponent<Player_CombatSystem>();
        AnimatorSystem = GetComponent<Player_AnimationSystem>();
    }
    #endregion

    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        Players[OwnerClientId] = this;

        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnRespawn);
        Singleton.Instance.GameEvents.OnInteractionReset.AddListener(OnInteractionReset);
    }

    public void DeinitializeNetwork(bool isOwner) {
        Players.Remove(OwnerClientId);

        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnRespawn);
        Singleton.Instance.GameEvents.OnInteractionReset.RemoveListener(OnInteractionReset);
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
        result = Physics.OverlapSphere(_target, m_interactionRadius, m_interactionLayer);

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

    public void OnInteractionReset() { _lastInteractable = null; }

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
        if (Input.Interact && _actualInteractable != null) 
            _actualInteractable.Interact(this);    
    }

    private void HandleItemDrop() {
        if (Input.Drop && Singleton.Instance.InventoryManager.GetItemFromSlot(ActualSlotSelected) != null) {
            Singleton.Instance.GameEvents.OnItemDropped?.Invoke(ActualSlotSelected);
        }
    }

    private void HandleSlotSelection() {
        if (Time.time < _slotSelectionCooldown) 
            return;

        if (Input.Slot1) 
            ActualSlotSelected = 0;
        if (Input.Slot2) 
            ActualSlotSelected = 1;
        if (Input.Slot3) 
            ActualSlotSelected = 2;
        if (Input.Slot4) 
            ActualSlotSelected = 3;
        if (Input.LastSlotUsed) 
            ActualSlotSelected = _lastSlot;

        SelectSlot(ActualSlotSelected);
    }

    private void SelectSlot(int index) {
        if (index == _lastSelectedSlotIndex) return;

        OnSlotSelected(index);
    }

    private void OnSlotSelected(int index) {
        _slotSelectionCooldown = Time.time + 0.025f;
        _lastSlot = _lastSelectedSlotIndex;
        _lastSelectedSlotIndex = index;

        Singleton.Instance.GameEvents.OnSlotSelected?.Invoke(index, false);
    }

    private void OnRespawn() {
        OnSlotSelected(ActualSlotSelected);
    }

    #region RPC
    
    #endregion
    public void Tick(bool isOwner) {
        if (!isOwner || HealthSystem.IsDead || GameManager.GetGameState() != GameState.Resumed) return;
       
        DetectInteractable();
        HandleInteract();

        if (!CombatSystem.GetCanSwitch()) return;

        HandleItemDrop();
        HandleSlotSelection();        
    }

    private void OnDrawGizmos() {
        Gizmos.DrawSphere(_target, m_interactionRadius);
        Gizmos.color = _actualInteractable == null ? Color.white : Color.green;
    }
}

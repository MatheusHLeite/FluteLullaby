using DelightStudio.Data;
using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Player_AnimationSystem : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private Animator m_fullBodyAnimator;
    [SerializeField] private Animator m_handsAnimator;

    [Header("Animation settings")]
    [SerializeField] private float m_animationSmoothness;

    [Header("Ragdoll")]
    [SerializeField] private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    private bool isPaused;

    #region Private references
    private Rigidbody m_rb;
    private Collider m_collider;

    private Player_InputHandler Input;
    private Player_MovementSystem Movement;
    private Player_CombatSystem Combat;
    #endregion

    #region Inputs
    private float inputMagnitude;
    private float inputX;
    private float inputY;

    private float rawInputX;
    private float rawInputY;
    #endregion

    #region Const strings
    private const string MovementMagnitude = "MovementMagnitude";
    private const string MovementX = "MovementX";
    private const string IsGrounded = "IsGrounded";
    private const string MovementY = "MovementY";
    private const string Jump = "Jump";
    private const string Crouch = "IsCrouch";
    private const string Attack = "Attack_";

    private const string Reload = "Reload";
    private const string Shot = "Shot";
    private const string Collect = "Collect";
    private const string Draw = "Draw";
    private const string Holster = "Holster";
    private const string Drop = "Drop";
    #endregion

    private Coroutine changeWeaponRoutine;

    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        Movement = GetComponent<Player_MovementSystem>();
        Combat = GetComponent<Player_CombatSystem>();

        m_rb = GetComponent<Rigidbody>();
        m_collider = GetComponent<CapsuleCollider>();

        ragdollColliders = new Collider[ragdollBodies.Length];
        for (int i = 0; i < ragdollBodies.Length; i++) {
            ragdollColliders[i] = ragdollBodies[i].GetComponent<Collider>();
        }
    }

    public override void OnDestroy() {
        ragdollColliders = null;
    }

    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        SetRagdollState(true);
        SetRigidbodyState(true, true);

        if (!isOwner) return;

        SetRagdollState(false);

        Singleton.Instance.GameEvents.OnPlayerDie.AddListener(OnPlayerDie);
        Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnPlayerRespawn);
        Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
    }

    public void DeinitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnPlayerDie.RemoveListener(OnPlayerDie);
        Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnPlayerRespawn);
        Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
        Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
    }
    #endregion

    #region Event calls
    private void OnGamePaused() => isPaused = true;

    private void OnGameResumed() => isPaused = false;
    #endregion

    #region Ragdoll
    private void OnPlayerDie(Vector3 hitPoint, Vector3 hitDirection, float impact) {
        ActivateRagdollWithImpact(hitPoint, hitDirection, impact, true);
        NotifyRagdollActivationServerRpc(hitPoint, hitDirection, impact);
    }

    private void OnPlayerRespawn() {
        DeactivateRagdoll(true);
        NotifyRagdollDeactivationServerRpc();
    }

    public void ActivateRagdollWithImpact(Vector3 hitPoint, Vector3 hitDirection, float impact, bool isLocal) {
        ActivateRagdoll(isLocal);

        if (!isLocal) SetRigidbodyState(true, false);

        Rigidbody closestRb = null;
        float closestDistance = float.MaxValue;

        foreach (var rb in ragdollBodies) {
            float distance = Vector3.Distance(rb.worldCenterOfMass, hitPoint);
            if (distance < closestDistance) {
                closestDistance = distance;
                closestRb = rb;
                break;
            }
        }

        closestRb.AddForce(hitDirection * impact, ForceMode.Impulse);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyRagdollActivationServerRpc(Vector3 hitPoint, Vector3 hitDirection, float impact) {
        var rpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = NetworkManager.ConnectedClientsIds
                    .Where(id => id != NetworkObject.OwnerClientId)
                    .ToArray()
            }
        };
        ActivateRagdollClientRpc(hitPoint, hitDirection, impact, rpcParams);
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyRagdollDeactivationServerRpc() {
        var rpcParams = new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = NetworkManager.ConnectedClientsIds
                    .Where(id => id != NetworkObject.OwnerClientId)
                    .ToArray()
            }
        };
        DeactivateRagdollClientRpc(rpcParams);
    }

    [ClientRpc]
    public void ActivateRagdollClientRpc(Vector3 hitPoint, Vector3 hitDirection, float impact, ClientRpcParams rpcParams = default) => ActivateRagdollWithImpact(hitPoint, hitDirection, impact, false);

    [ClientRpc]
    public void DeactivateRagdollClientRpc(ClientRpcParams rpcParams = default) => DeactivateRagdoll(false);

    private void ActivateRagdoll(bool isLocal) {
        if (isLocal) SetRagdollState(true);
        m_fullBodyAnimator.enabled = false;
    }

    private void DeactivateRagdoll(bool isLocal) {
        if (isLocal) SetRagdollState(false);
        else SetRigidbodyState(true, true);
        m_fullBodyAnimator.enabled = true;
    }

    private void SetRagdollState(bool state) {
        m_collider.enabled = !state;
        m_rb.isKinematic = state;

        foreach (var rb in ragdollBodies)
            rb.isKinematic = !state;

        foreach (var col in ragdollColliders)
            col.enabled = state;
    }

    private void SetRigidbodyState(bool mainRb, bool othersRb) {
        m_rb.isKinematic = mainRb;
        foreach (var rb in ragdollBodies)
            rb.isKinematic = othersRb;
    }
    #endregion

    #region Actions
    public void OnAttack(int comboStep) => RequestAnimationServerRpc(Attack + comboStep); 
 
    public void OnCrouch(bool crouch) => RequestAnimationStateServerRpc(Crouch, crouch);

    public void OnJump() => RequestAnimationServerRpc(Jump);

    public void OnShot() {
        m_handsAnimator.SetTrigger(Shot);
        RequestAnimationServerRpc(Shot); 
    }

    public void OnReload() {
        m_handsAnimator.SetTrigger(Reload);
        RequestAnimationServerRpc(Reload); 
    }

    public void OnCollect() {
        m_handsAnimator.SetTrigger(Collect);
        RequestAnimationServerRpc(Collect);
    }

    public void OnDrop() {
        ChangeIdleState(null, false);

        m_handsAnimator.SetTrigger(Drop);
        RequestAnimationServerRpc(Drop);
    }

    public void ChangeIdleState(Weapon currentWeapon, bool hasItemPreviously) { 
        if (changeWeaponRoutine != null)
            StopCoroutine(changeWeaponRoutine);
        changeWeaponRoutine = StartCoroutine(ChangeWeapon(currentWeapon, hasItemPreviously)); 
    }

    private IEnumerator ChangeWeapon(Weapon currentWeapon, bool hasItemPreviously) {
        if (hasItemPreviously) {
            m_handsAnimator.SetTrigger(Holster);
            RequestAnimationServerRpc(Holster);

            yield return new WaitForSeconds(0.4f);
        }

        if (currentWeapon != null) {
            m_handsAnimator.runtimeAnimatorController = currentWeapon.m_overrideController;

            m_handsAnimator.SetTrigger(Draw);
            RequestAnimationServerRpc(Draw);
        }
        else {
            m_handsAnimator.Play("Weapon Empty");
            Combat.SetCanSwitch(true);            
        }
    }
    #endregion

    #region Network calls
    [ServerRpc]
    void RequestAnimationServerRpc(string animationTrigger) => PlayAnimationClientRpc(animationTrigger);

    [ClientRpc]
    void PlayAnimationClientRpc(string animationTrigger) => m_fullBodyAnimator.SetTrigger(animationTrigger);
    
    [ServerRpc(RequireOwnership = false)]
    void RequestAnimationStateServerRpc(string state, bool condition) => SetAnimationStateClientRpc(state, condition);
    
    [ClientRpc]
    void SetAnimationStateClientRpc(string state, bool condition) => m_fullBodyAnimator.SetBool(state, condition);
    
    [ServerRpc(RequireOwnership = false)]
    void RequestAnimatorSyncServerRpc(MovementAnimationParameters parameters) => SetAnimatorValuesClientRpc(parameters);

    [ClientRpc]
    void SetAnimatorValuesClientRpc(MovementAnimationParameters parameters) {
        m_fullBodyAnimator.SetFloat(MovementMagnitude, parameters.m_moveMagnitude);
        m_fullBodyAnimator.SetFloat(MovementX, parameters.m_moveX);
        m_fullBodyAnimator.SetFloat(MovementY, parameters.m_moveY);
        m_fullBodyAnimator.SetBool(IsGrounded, parameters.m_isGrounded);
    }
    #endregion

    #region Updates
    private void UpdateAnimator() {
        rawInputX = Input.Sprint ? Input.MoveInput.x * 2 : Input.MoveInput.x;
        rawInputY = Input.Sprint ? Input.MoveInput.y * 2 : Input.MoveInput.y;

        inputMagnitude = Input.MoveInput.magnitude;
        inputX = Mathf.Lerp(inputX, rawInputX, m_animationSmoothness * Time.deltaTime);
        inputY = Mathf.Lerp(inputY, rawInputY, m_animationSmoothness * Time.deltaTime);

        MovementAnimationParameters parameters = new MovementAnimationParameters {
            m_moveMagnitude = isPaused ? 0 : inputMagnitude,
            m_moveX = isPaused ? 0 : inputX,
            m_moveY = isPaused ? 0 : inputY,
            m_isGrounded = Movement.IsGrounded
        };

        RequestAnimatorSyncServerRpc(parameters);
    }

    public void Tick(bool isOwner) {
        if (!isOwner) return;

        UpdateAnimator();
    }
    #endregion
}
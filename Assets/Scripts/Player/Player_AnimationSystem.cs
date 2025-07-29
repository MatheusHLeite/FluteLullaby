using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Player_AnimationSystem : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private Animator m_fullBodyAnimator;
    [SerializeField] private Animator m_firstPersonAnimator;

    [Header("Animation settings")]
    [SerializeField] private float m_animationSmoothness;

    [Header("Ragdoll")]
    [SerializeField] private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

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
    private const string MovementY = "MovementY";
    private const string IsGrounded = "IsGrounded";
    private const string Jump = "Jump";
    private const string Attack_01 = "Attack_01";
    private const string Attack_02 = "Attack_02";
    private const string Attack_03 = "Attack_03";
    private const string Attack_04 = "Attack_04";
    #endregion

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
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (IsOwner) {
            m_rb = GetComponent<Rigidbody>();
            m_collider = GetComponent<CapsuleCollider>();

            SetRagdollState(false);

            Singleton.Instance.GameEvents.OnPlayerDie.AddListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnPlayerRespawn);            
            return;
        }

        SetRagdollState(true);
        SetRigidbodyState(true, true);
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnPlayerDie.RemoveListener(OnPlayerDie);
            Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnPlayerRespawn);
        }
    }
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
    public void OnAttack(int comboStep) {
        RequestAnimationServerRpc(Attack_01); //Change to events [TODO]

        switch (comboStep) {
            case 0:
                m_firstPersonAnimator.SetTrigger(Attack_01);
                break;
            case 1:
                m_firstPersonAnimator.SetTrigger(Attack_02);
                break;
            case 2:
                m_firstPersonAnimator.SetTrigger(Attack_03);
                break;
            case 3:
                m_firstPersonAnimator.SetTrigger(Attack_04);
                break;
        }
    } //[TODO] CHANGE ALL TO PLAY ONLY FOR THE CLIENT NOT FOR THE OWNER

    public void OnCrouch(bool crouch) {
        RequestAnimationStateServerRpc("IsCrouch", crouch);
    }

    public void OnJump() {
        RequestAnimationServerRpc(Jump);
    }//[TODO] CHANGE ALL TO PLAY ONLY FOR THE CLIENT NOT FOR THE OWNER

    public void OnShot() {
        RequestAnimationServerRpc("Shot_r");
    }//[TODO] CHANGE ALL TO PLAY ONLY FOR THE CLIENT NOT FOR THE OWNER

    public void OnReload() { 
        RequestAnimationServerRpc("ReloadRevolver");
    } //[TODO] CHANGE ALL TO PLAY ONLY FOR THE CLIENT NOT FOR THE OWNER

    public void ChangeIdleState() { //[TODO]Add animation name on the weapon SO
        RequestAnimationServerRpc("SetIdle");
    }//[TODO] CHANGE ALL TO PLAY ONLY FOR THE CLIENT NOT FOR THE OWNER
    #endregion

    #region Network calls
    [ServerRpc]
    void RequestAnimationServerRpc(string animationTrigger) {
        PlayAnimationClientRpc(animationTrigger);
    }

    [ClientRpc]
    void PlayAnimationClientRpc(string animationTrigger) {
        m_fullBodyAnimator.SetTrigger(animationTrigger);
    }

    [ServerRpc]
    void RequestAnimationStateServerRpc(string state, bool condition) {
        SetAnimationStateClientRpc(state, condition);
    }

    [ClientRpc]
    void SetAnimationStateClientRpc(string state, bool condition) {
        m_fullBodyAnimator.SetBool(state, condition);
    }

    [ServerRpc]
    void RequestAnimatorSyncServerRpc(MovementAnimationParameters parameters) {
        SetAnimatorValuesClientRpc(parameters);
    }

    [ClientRpc]
    void SetAnimatorValuesClientRpc(MovementAnimationParameters parameters) {
        m_fullBodyAnimator.SetFloat(MovementMagnitude, parameters.m_moveMagnitude);
        m_fullBodyAnimator.SetFloat(MovementX, parameters.m_moveX);
        m_fullBodyAnimator.SetFloat(MovementY, parameters.m_moveY);
        m_fullBodyAnimator.SetBool(IsGrounded, parameters.m_isGrounded);
        m_fullBodyAnimator.SetBool("Revolver", parameters.m_holdingRevolver); //[TODO] Add const string and change for more weapon types
    }
    #endregion

    #region Updates
    private void UpdateAnimator() {
        rawInputX = Input.Sprint ? Input.MoveInput.x * 2 : Input.MoveInput.x;
        rawInputY = Input.Sprint ? Input.MoveInput.y * 2 : Input.MoveInput.y;

        inputMagnitude = Input.MoveInput.magnitude;
        inputX = Mathf.Lerp(inputX, rawInputX, m_animationSmoothness * Time.deltaTime);
        inputY = Mathf.Lerp(inputY, rawInputY, m_animationSmoothness * Time.deltaTime);

        m_fullBodyAnimator.SetFloat(MovementMagnitude, inputMagnitude);
        m_fullBodyAnimator.SetFloat(MovementX, inputX);
        m_fullBodyAnimator.SetFloat(MovementY, inputY);
        m_fullBodyAnimator.SetBool(IsGrounded, Movement.IsGrounded);
        m_fullBodyAnimator.SetBool("Revolver", Combat.GetActualEquippedWeapon() != null ? Combat.GetActualEquippedWeapon().m_itemType == ItemType.Firearm : false); //[TODO] Add const string and change for more weapon types

        MovementAnimationParameters parameters = new MovementAnimationParameters {
            m_moveMagnitude = inputMagnitude,
            m_moveX = inputX,
            m_moveY = inputY,
            m_isGrounded = Movement.IsGrounded,
            m_holdingRevolver = Combat.GetActualEquippedWeapon() != null ? Combat.GetActualEquippedWeapon().m_itemType == ItemType.Firearm : false
        };

        RequestAnimatorSyncServerRpc(parameters);
    }

    private void Update() {
        if (!IsOwner) return;

        UpdateAnimator();
    }
    #endregion

    #region Audio
    public AudioClip[] revolverShot;
    public AudioClip[] shotgunShot;
    public AudioSource _as;

    internal void Play3DAudio(Weapons weapon) { //[TODO] ADD a Player_AudioSystem 
        RequestShootSoundServerRpc(weapon);
    }

    [ServerRpc]
    void RequestShootSoundServerRpc(Weapons weapon, ServerRpcParams rpcParams = default) {
        PlayShootSoundClientRpc(weapon);
    }
    
    [ClientRpc]
    void PlayShootSoundClientRpc(Weapons weapon) {
        _as.pitch = Random.Range(0.8f, 1.4f);
        AudioClip clip;

        switch (weapon) {
            case Weapons.Revolver:
                clip = revolverShot[Random.Range(0, revolverShot.Length)];
                break;
            case Weapons.Shotgun:
                clip = shotgunShot[Random.Range(0, shotgunShot.Length)];
                break;
            default:
                clip = null;
                break;
        }

        _as.PlayOneShot(clip);
    }
    #endregion
}

public struct MovementAnimationParameters : INetworkSerializable {
    public float m_moveMagnitude;
    public float m_moveX;
    public float m_moveY;
    public bool m_isGrounded;
    public bool m_holdingRevolver;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref m_moveMagnitude);
        serializer.SerializeValue(ref m_moveX);
        serializer.SerializeValue(ref m_moveY);
        serializer.SerializeValue(ref m_isGrounded);
        serializer.SerializeValue(ref m_holdingRevolver);
    }
}

public enum Weapons { Revolver, Shotgun }
using Unity.Netcode;
using UnityEngine;

public class Player_MovementSystem : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private PlayerParameters_SO m_playerParameters;

    [Header("Crouch system")]
    [SerializeField] private float m_crouchHeight = 1.1f;
    [SerializeField] private float m_crouchCenter = -0.35f;
    [Space(15)]
    [SerializeField] private float m_cameraCrouchY = -0.1f;
    [SerializeField] private float m_cameraStandY = 0.6f;

    [Header("Raycast")]
    [SerializeField] private float m_raycastDistance = .445f;
    [SerializeField] private LayerMask m_groundLayerMask;

    [Header("Debug")]
    [SerializeField] private bool m_unlimitedSprint = false;

    #region Variables
    #region Private references
    private Rigidbody _rb;
    private CapsuleCollider _thisCollider;
    private Transform _cameraPivot;
    private Player_InputHandler Input;
    private Player_AnimationSystem Animation;
    private Player_HealthSystem HealthSystem;
    private Player_CameraMovementSystem CameraMovementSystem;
    #endregion

    #region Private variables         
    private float m_maxVelocityChange = 10f;
    private float m_maxAirVelocityChange;
    private float m_acceleration;
    private float m_deceleration;        
    private float m_coyoteTime;        
    private float m_sprintCooldown;
    private float m_toCrouchSpeed;

    //private float _actualPlayerSpeed;
    private float _sprintRemaining;
    private float _sprintCooldownReset;
    private bool _sprintOnCooldown;
    private RaycastHit _groundHit;
    #endregion

    #region Private upgradable/modifiable variables
    private float m_walkSpeed;
    private float m_sprintSpeed;
    private float m_crouchSpeed;

    private float m_jumpPower;

    private float m_sprintRecoverSpeed;
    private float m_sprintDuration;

    private bool m_playerCanMove;
    private bool m_enableSprint;
    private bool m_enableJump;
    private bool m_enableCrouch;
    #endregion

    #region Public variables
    public Collider GetPlayerCollider => _thisCollider;
    public bool IsGrounded { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsCrouched { get; private set; }
    #endregion

    #region Private movement variables
    private Vector3 targetVelocity;
    private Vector3 movementInput;
    private Vector3 movementDirection;
    private Vector3 desiredVelocity;
    private Vector3 inputBeforeJumping;
    private bool canSprint;
    private bool canStandUp;
    private bool sprintFlag;
    private float speedMultiplierBase;
    private float speedMultiplier;
    private float finalAcceleration;    
    private float coyoteTimer;
    private float maxVelocityChange;

    private float standHeight;
    private float camPosY;
    private float colliderHeight;
    private Vector3 colliderCenter;
    private Vector3 camPos;
    #endregion
    #endregion

    #region Initialization
    private void Awake() {
        _rb = GetComponent<Rigidbody>();
        _thisCollider = GetComponent<CapsuleCollider>();
        Input = GetComponent<Player_InputHandler>();
        Animation = GetComponent<Player_AnimationSystem>();
        HealthSystem = GetComponent<Player_HealthSystem>();
        CameraMovementSystem = GetComponent<Player_CameraMovementSystem>();
    }

    public void SetPlayerParameters() {
        m_walkSpeed = m_playerParameters.m_walkSpeed;
        m_sprintSpeed = m_playerParameters.m_sprintSpeed;
        m_crouchSpeed = m_playerParameters.m_crouchSpeed;
        m_maxVelocityChange = 10f;
        m_maxAirVelocityChange = m_playerParameters.m_maxAirVelocityChange;
        m_acceleration = m_playerParameters.m_acceleration;
        m_deceleration = m_playerParameters.m_deceleration;
        m_jumpPower = m_playerParameters.m_jumpPower;
        m_coyoteTime = m_playerParameters.m_coyoteTime;
        m_sprintDuration = m_playerParameters.m_sprintDuration;
        m_sprintCooldown = m_playerParameters.m_sprintCooldown;
        m_sprintRecoverSpeed = m_playerParameters.m_sprintRecoverSpeed;
        m_toCrouchSpeed = m_playerParameters.m_toCrouchSpeed;

        standHeight = _thisCollider.height;
        colliderHeight = standHeight;
        colliderCenter = Vector3.zero;
        camPosY = m_cameraStandY;

        _cameraPivot = CameraMovementSystem.GetPlayerCameraHolder;

        m_playerCanMove = true;
        m_enableSprint = true;
        m_enableJump = true;
        m_enableCrouch = true;

        if (!m_unlimitedSprint) {
            _sprintRemaining = m_sprintDuration;
            _sprintCooldownReset = m_sprintCooldown;
        }
    }
    #endregion

    #region Network Initialization
    public override void OnNetworkSpawn()  {
        if (IsOwner) {
            SetPlayerParameters(); //[TODO] Add to a manager  
            return;
        }

        GetComponent<CapsuleCollider>().enabled = false;        
    }
    #endregion

    #region Movement
    private void HandleMovement() {
        if (!m_playerCanMove) return;

        movementInput = new Vector3(Input.MoveInput.x, 0, Input.MoveInput.y);
        canSprint = m_enableSprint && _sprintRemaining > 0f && !_sprintOnCooldown && !IsCrouched;
        sprintFlag = Input.Sprint && IsGrounded && (canSprint || m_unlimitedSprint);
        speedMultiplierBase = sprintFlag ? m_sprintSpeed : m_walkSpeed;
        speedMultiplier = IsCrouched ? m_crouchSpeed : speedMultiplierBase;

        if (IsGrounded) inputBeforeJumping = movementInput;
        Vector3 correctInput = !IsGrounded && movementInput.magnitude == 0f ? inputBeforeJumping : movementInput;

        #region Old
        /*
        finalAcceleration = Input.MoveInput.magnitude != 0 ? m_acceleration : m_deceleration;

        targetVelocity = Vector3.Lerp(targetVelocity, transform.TransformDirection(movementInput) * speedMultiplier, finalAcceleration * Time.deltaTime);

        movementDirection = (targetVelocity - _rb.linearVelocity);
        movementDirection.x = Mathf.Clamp(movementDirection.x, -m_maxVelocityChange, m_maxVelocityChange);
        movementDirection.z = Mathf.Clamp(movementDirection.z, -m_maxVelocityChange, m_maxVelocityChange);
        movementDirection.y = 0;*/
            #endregion

        #region New
        finalAcceleration = IsGrounded ? (Input.MoveInput.magnitude != 0 ? m_acceleration : m_deceleration) : (m_acceleration / 1.35f);
        maxVelocityChange = IsGrounded ? m_maxVelocityChange : m_maxAirVelocityChange;
        desiredVelocity = transform.TransformDirection(correctInput) * speedMultiplier;

        targetVelocity = Vector3.Lerp(targetVelocity, desiredVelocity, finalAcceleration * Time.deltaTime);
 
        movementDirection = (targetVelocity - _rb.linearVelocity);
        movementDirection.x = Mathf.Clamp(movementDirection.x, -maxVelocityChange, maxVelocityChange);
        movementDirection.z = Mathf.Clamp(movementDirection.z, -maxVelocityChange, maxVelocityChange);
        movementDirection.y = 0f;
        #endregion

        _rb.AddForce(movementDirection, ForceMode.VelocityChange);

        IsSprinting = sprintFlag && movementInput.magnitude > 0;

        //_actualPlayerSpeed = movementDirection.magnitude; //[TODO] remove it
    }

    private void HandleSprint() {
        if (!m_enableSprint || m_unlimitedSprint) return;

        Singleton.Instance.GameEvents.OnStaminaUsage?.Invoke(_sprintRemaining, m_sprintDuration);

        if (IsSprinting) {
            _sprintRemaining -= Time.deltaTime;
            if (_sprintRemaining <= 0) {
                IsSprinting = false;
                _sprintOnCooldown = true;
            }
            return;
        }

        _sprintRemaining = Mathf.Clamp(_sprintRemaining += Time.deltaTime * m_sprintRecoverSpeed, 0, m_sprintDuration);

        if (_sprintOnCooldown) {
            m_sprintCooldown -= Time.deltaTime;
            if (m_sprintCooldown <= 0)
                _sprintOnCooldown = false;
            return;
        }

        m_sprintCooldown = _sprintCooldownReset;
    }

    private void HandleJump() {
        if (!m_enableJump) return;

        if (IsGrounded) coyoteTimer = m_coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        if ((Input.Jump) && (IsGrounded || coyoteTimer > 0))
            Jump();
    }

    private void HandleCrouch() {
        if (m_enableCrouch && IsGrounded) {
            if (Input.Crouch)            
                Crouch();            
            else if (!Input.Crouch)            
                StandUp();            
        }

        camPos.y = camPosY;

        _thisCollider.center = Vector3.Lerp(_thisCollider.center, colliderCenter, Time.deltaTime * m_toCrouchSpeed);
        _thisCollider.height = Mathf.Lerp(_thisCollider.height, colliderHeight, Time.deltaTime * m_toCrouchSpeed);     
        _cameraPivot.localPosition = Vector3.Lerp(_cameraPivot.localPosition, camPos, Time.deltaTime * m_toCrouchSpeed);
    }

    private void Crouch() {
        if (IsCrouched) return;
        IsCrouched = true;

        colliderCenter = new Vector3(0, m_crouchCenter, 0);
        colliderHeight = m_crouchHeight;        
        camPosY = m_cameraCrouchY;        
    }

    private void StandUp() {
        if (!IsCrouched || !canStandUp) return;
        IsCrouched = false;

        colliderCenter = Vector3.zero;
        colliderHeight = standHeight;
        camPosY = m_cameraStandY;
    }

    private void Jump() {
        Animation.OnJump();

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        _rb.AddForce(0f, m_jumpPower, 0f, ForceMode.Impulse);

        IsGrounded = false;
        coyoteTimer = 0f;
    }
    #endregion

    private void RaycastCheck() {
        Vector3 baseCenter = transform.position + _thisCollider.center - (Vector3.up * ((_thisCollider.height / 2f) - _thisCollider.radius));
        Vector3 boxHalfExtents = new Vector3(_thisCollider.radius * 0.9f, .1f, _thisCollider.radius * 0.9f);    
        IsGrounded = Physics.BoxCast(baseCenter, boxHalfExtents, Vector3.down, out RaycastHit bottomHit, Quaternion.identity, m_raycastDistance, m_groundLayerMask);
        if (coyoteTimer == 0)
            IsGrounded = false;

        Vector3 topCenter = transform.position + _thisCollider.center + (Vector3.up * ((_thisCollider.height / 2f) - _thisCollider.radius));

        boxHalfExtents.y *= 3.5f;
        canStandUp = !Physics.BoxCast(topCenter, boxHalfExtents, Vector3.up, out RaycastHit topHit, Quaternion.identity, m_raycastDistance * 2, m_groundLayerMask);
    }

    private void Update() {
        if (!IsOwner || HealthSystem.IsDead) return;

        HandleJump();
        HandleSprint();
        HandleCrouch();
    }

    private void FixedUpdate() {
        if (!IsOwner || HealthSystem.IsDead) return;

        HandleMovement();
        RaycastCheck();
    }


#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if (_thisCollider == null)
            _thisCollider = GetComponent<CapsuleCollider>();

        // BASE (GROUND CHECK)
        Vector3 baseCenter = transform.position + _thisCollider.center - (Vector3.up * ((_thisCollider.height / 2f) - _thisCollider.radius));
        Vector3 boxHalfExtents = new Vector3(_thisCollider.radius * 0.9f, 0.1f, _thisCollider.radius * 0.9f);

        Gizmos.color = Color.green;
        DrawBoxCastGizmo(baseCenter, boxHalfExtents, Vector3.down, m_raycastDistance);

        // TOPO (STAND UP CHECK)
        Vector3 topCenter = transform.position + _thisCollider.center + (Vector3.up * ((_thisCollider.height / 2f) - _thisCollider.radius));
        Vector3 topBoxHalfExtents = new Vector3(_thisCollider.radius * 0.9f, 0.1f * 3.5f, _thisCollider.radius * 0.9f);

        Gizmos.color = Color.red;
        DrawBoxCastGizmo(topCenter, topBoxHalfExtents, Vector3.up, m_raycastDistance * 2);
    }

    private void DrawBoxCastGizmo(Vector3 start, Vector3 halfExtents, Vector3 direction, float distance) {
        Quaternion orientation = Quaternion.identity;
        Matrix4x4 cubeTransform = Matrix4x4.TRS(start + direction.normalized * distance / 2f, orientation, halfExtents * 2);
        Gizmos.matrix = cubeTransform;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
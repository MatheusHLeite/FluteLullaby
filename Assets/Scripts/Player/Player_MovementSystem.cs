using Unity.Netcode;
using UnityEngine;

public class Player_MovementSystem : NetworkBehaviour {

    [Header("Sprint System")]
    [Tooltip("Tempo em segundos que o jogador precisa segurar Shift para começar a correr")]
    [SerializeField] private float m_sprintHoldDelay = 0.3f;
    [SerializeField] private float m_slideCameraTiltAngle = 8f;

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

    private bool _sprintToggle;
    bool sprintButton;

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

    private float _actualPlayerSpeed;
    private float _staminaRemaining;
    private float _sprintCooldownReset;
    private bool _sprintOnCooldown;
    private RaycastHit _groundHit;

    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private Vector3 _dashDirection;

    private bool _isSliding;
    private float _slideTimer;
    private float _slideCooldownTimer;
    private Vector3 _slideDirection;
    private float _slideSpeed;
    private bool _crouchInputHeldDuringSlide;
    private bool _isOnDownhill;

    private float _sprintHoldTimer;
    private bool _sprintHoldActive;

    private float _staminaRegenCooldown;
    private float _lastStaminaConsumption;
    #endregion

    #region Private upgradable/modifiable variables
    private float m_walkSpeed;
    private float m_sprintSpeed;
    private float m_crouchSpeed;

    private float m_staminaRegenCooldownTime;
    private float m_dashStaminaCost;
    private float m_runningStaminaCost;

    private float m_dashForce;
    private float m_dashDuration;
    private float m_dashCooldown;

    private float m_slideDeceleration;
    private float m_slideSideControl;
    private float m_slideDuration;
    private float m_slideCooldown;
    private float m_slideStaminaCost;
    private float m_minSprintSpeedToSlide;
    private float m_slideSpeedBoost;
    private float m_slideDownhillAcceleration;
    private float m_slideDownhillAngleThreshold;
    private float m_slideMaxDownhillSpeed;    
    
    private float m_jumpPower;

    private float m_staminaRecoverSpeed;
    private float m_maxStamina;

    private bool m_playerCanMove;
    private bool m_enableSprint;
    private bool m_enableJump;
    private bool m_enableCrouch;
    #endregion

    #region Public variables
    public Collider GetPlayerCollider => _thisCollider;
    public Rigidbody GetRigidbody => _rb;
    public bool IsGrounded { get; private set; }
    public bool IsSprinting { get; private set; }
    public bool IsCrouched { get; private set; }
    public bool IsDashing => _isDashing;
    public bool IsSliding => _isSliding;
    #endregion

    #region Network variables
    private NetworkVariable<bool> isCrouched_NV = new NetworkVariable<bool>(
    false,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
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

    public void SetPlayerParameters(PlayerParameters_SO playerParameters) {
        m_walkSpeed = playerParameters.m_walkSpeed;
        m_sprintSpeed = playerParameters.m_sprintSpeed;
        m_crouchSpeed = playerParameters.m_crouchSpeed;
        m_maxVelocityChange = 10f;
        m_maxAirVelocityChange = playerParameters.m_maxAirVelocityChange;
        m_acceleration = playerParameters.m_acceleration;
        m_deceleration = playerParameters.m_deceleration;
        m_jumpPower = playerParameters.m_jumpPower;
        m_coyoteTime = playerParameters.m_coyoteTime;
        m_maxStamina = playerParameters.m_maxStamina;
        m_sprintCooldown = playerParameters.m_sprintCooldown;
        m_runningStaminaCost = playerParameters.m_runningStaminaCost;
        m_staminaRecoverSpeed = playerParameters.m_staminaRecoverSpeed;
        m_staminaRegenCooldownTime = playerParameters.m_staminaRegenCooldownTime;
        m_toCrouchSpeed = playerParameters.m_toCrouchSpeed;
        m_dashForce = playerParameters.m_dashForce;
        m_dashDuration = playerParameters.m_dashDuration;
        m_dashCooldown = playerParameters.m_dashCooldown;
        m_dashStaminaCost = playerParameters.m_dashStaminaCost;

        m_slideDeceleration = playerParameters.m_slideDeceleration;
        m_slideSideControl = playerParameters.m_slideSideControl;
        m_slideDuration = playerParameters.m_slideDuration;
        m_slideCooldown = playerParameters.m_slideCooldown;
        m_slideStaminaCost = playerParameters.m_slideStaminaCost;
        m_minSprintSpeedToSlide = playerParameters.m_minSprintSpeedToSlide;
        m_slideSpeedBoost = playerParameters.m_slideSpeedBoost;
        m_slideDownhillAcceleration = playerParameters.m_slideDownhillAcceleration;
        m_slideDownhillAngleThreshold = playerParameters.m_slideDownhillAngleThreshold;
        m_slideMaxDownhillSpeed = playerParameters.m_slideMaxDownhillSpeed;

        _dashCooldownTimer = 0f;
        _slideCooldownTimer = 0f;

        standHeight = _thisCollider.height;
        colliderHeight = standHeight;
        colliderCenter = Vector3.zero;
        camPosY = m_cameraStandY;

        m_playerCanMove = true;
        m_enableSprint = true;
        m_enableJump = true;
        m_enableCrouch = true;

        if (!m_unlimitedSprint) {
            _staminaRemaining = m_maxStamina;
            _sprintCooldownReset = m_sprintCooldown;
        }

        Singleton.Instance.GameEvents.OnStaminaUISet?.Invoke(m_maxStamina);
    }
    #endregion

    #region Network Initialization
    public override void OnNetworkSpawn()  {
        if (IsOwner) {
            _cameraPivot = CameraMovementSystem.GetPlayerCameraHolder;

            Singleton.Instance.GameEvents.OnSprintToggleChanged.AddListener(OnSprintToggleChanged);
            Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
            return; 
        }

        isCrouched_NV.OnValueChanged += OnCrouchStateChanged;      
    }

    public override void OnNetworkDespawn() {
        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSprintToggleChanged.RemoveListener(OnSprintToggleChanged);
            Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
            return;
        }

        isCrouched_NV.OnValueChanged -= OnCrouchStateChanged;
    }

    private void OnCrouchStateChanged(bool oldValue, bool newValue) => Animation.OnCrouch(newValue);

    private void OnSprintToggleChanged(int i) {
        _sprintToggle = i == 1;
    }

    private void OnGamePaused() {
        _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
    }
    #endregion

    #region Movement
    private void HandleMovement() {
        if (!m_playerCanMove || _isDashing) return;

        if (_isSliding) {
            HandleSlideMovement();
            return;
        }
        
        if (_sprintToggle) {
            if (Input.Sprint && movementInput.magnitude > 0) sprintButton = true;
            else if (movementInput.magnitude <= 0 || !canSprint) sprintButton = false;
        }
        else {
            if (Input.Sprint && movementInput.magnitude > 0) {
                _sprintHoldTimer += Time.deltaTime;
                if (_sprintHoldTimer >= m_sprintHoldDelay) {
                    _sprintHoldActive = true;
                }
            }
            else {
                _sprintHoldTimer = 0f;
                _sprintHoldActive = false;
            }
            
            sprintButton = _sprintHoldActive;
        }

        movementInput = new Vector3(Input.MoveInput.x, 0, Input.MoveInput.y);
        canSprint = m_enableSprint && _staminaRemaining > 0f && !_sprintOnCooldown && !IsCrouched;
        sprintFlag = sprintButton && IsGrounded && (canSprint || m_unlimitedSprint);
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
    }

    private void HandleSprint() {
        if (!m_enableSprint || m_unlimitedSprint) return;

        if (IsSprinting) {
            _staminaRemaining -= Time.deltaTime * m_runningStaminaCost;
            _staminaRegenCooldown = Time.time + m_staminaRegenCooldownTime;
            if (_staminaRemaining <= 0) {
                IsSprinting = false;
                _sprintOnCooldown = true;
            }
            return;
        }

        if (_staminaRegenCooldown < Time.time) 
            _staminaRemaining = Mathf.Clamp(_staminaRemaining += Time.deltaTime * m_staminaRecoverSpeed, 0, m_maxStamina);        

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
        if (m_enableCrouch && IsGrounded && !_isSliding) {
            if (Input.Crouch) {
                bool canSlide = CanInitiateSlide();
                if (canSlide) {
                    InitiateSlide();
                } else {
                    Crouch();
                }
            }       
            else if (!Input.Crouch) {
                StandUp(); 
            }                     
        }

        camPos.y = camPosY;

        _thisCollider.center = Vector3.Lerp(_thisCollider.center, colliderCenter, Time.deltaTime * m_toCrouchSpeed);
        _thisCollider.height = Mathf.Lerp(_thisCollider.height, colliderHeight, Time.deltaTime * m_toCrouchSpeed);
        _cameraPivot.localPosition = Vector3.Lerp(_cameraPivot.localPosition, camPos, Time.deltaTime * m_toCrouchSpeed);
    }

    private void HandleDash() {
        if (_isDashing) {
            _dashTimer -= Time.deltaTime;

            if (_dashTimer <= 0f) {
                _isDashing = false;
                _dashCooldownTimer = m_dashCooldown;
            }
            return;
        }

        if (_dashCooldownTimer > 0f)        
            _dashCooldownTimer -= Time.deltaTime;        

        bool hasMovementInput = movementInput.magnitude > 0.1f;
        bool isNotHoldingSprint = !_sprintHoldActive;
        bool hasEnoughStamina = m_unlimitedSprint || _staminaRemaining >= m_dashStaminaCost;

        if (Input.Dash && _dashCooldownTimer <= 0f && !IsCrouched && !_isSliding && hasMovementInput && isNotHoldingSprint && hasEnoughStamina) {
            PerformDash();
            Input.ConsumeDash();
        }
        else if (Input.Dash)        
            Input.ConsumeDash();        
    }

    private void HandleSlide() {
        if (_isSliding) {
            if (!_isOnDownhill) {
                _slideTimer -= Time.deltaTime;
            }

            if (Input.Crouch) {
                _crouchInputHeldDuringSlide = true;
            }

            bool jumpPressed = Input.Jump;
            Vector3 moveInput = new Vector3(Input.MoveInput.x, 0, Input.MoveInput.y);
            bool backwardInput = Vector3.Dot(transform.TransformDirection(moveInput), _slideDirection) < -0.5f;

            bool timerExpired = !_isOnDownhill && _slideTimer <= 0f;

            if (jumpPressed || backwardInput || timerExpired) {
                CancelSlide(jumpPressed);
            }
            return;
        }

        if (_slideCooldownTimer > 0f) {
            _slideCooldownTimer -= Time.deltaTime;
        }
    }

    private void HandleSlideMovement() {
        _isOnDownhill = false;

        if (IsGrounded && _groundHit.normal != Vector3.zero) {
            float groundAngle = Vector3.Angle(Vector3.up, _groundHit.normal);
            Vector3 slopeDirection = Vector3.ProjectOnPlane(Vector3.down, _groundHit.normal).normalized;
            float slopeDot = Vector3.Dot(_slideDirection, slopeDirection);

            if (groundAngle >= m_slideDownhillAngleThreshold && slopeDot > 0.3f) {
                _isOnDownhill = true;
                _slideSpeed = Mathf.Min(_slideSpeed + m_slideDownhillAcceleration * Time.deltaTime, m_slideMaxDownhillSpeed);
            }
        }

        if (!_isOnDownhill) {
            _slideSpeed = Mathf.Lerp(_slideSpeed, 0f, m_slideDeceleration * Time.deltaTime);
            
            if (_slideSpeed < 0.1f) {
                _slideSpeed = 0f;
            }
        }

        Vector3 sideInput = new Vector3(Input.MoveInput.x, 0, 0);
        Vector3 sideDirection = transform.TransformDirection(sideInput);

        Vector3 slideVelocity = _slideDirection * _slideSpeed;
        Vector3 sideVelocity = sideDirection * m_slideSideControl;

        Vector3 targetVelocity = slideVelocity + sideVelocity;
        Vector3 velocityChange = targetVelocity - new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        velocityChange.y = 0f;

        _rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    private bool CanInitiateSlide() {
        if (_isSliding || _isDashing || IsCrouched) return false;
        if (_slideCooldownTimer > 0f) return false;

        Vector3 horizontalVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float currentSpeed = horizontalVelocity.magnitude;

        Vector3 forwardVelocity = transform.InverseTransformDirection(horizontalVelocity);
        bool isMovingForward = forwardVelocity.z > 0.1f;
        bool isMovingFastEnough = currentSpeed >= m_minSprintSpeedToSlide;

        return isMovingFastEnough && isMovingForward;
    }

    private void InitiateSlide() {
        _isSliding = true;
        _slideTimer = m_slideDuration;
        _crouchInputHeldDuringSlide = false;

        Vector3 currentVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _slideDirection = currentVelocity.normalized;
        _slideSpeed = currentVelocity.magnitude + m_slideSpeedBoost;

        colliderCenter = new Vector3(0, m_crouchCenter, 0);
        colliderHeight = m_crouchHeight;
        camPosY = m_cameraCrouchY;

        CameraMovementSystem.PlaySlideEffect(m_slideCameraTiltAngle);
    }

    private void CancelSlide(bool shouldJump = false) {
        _isSliding = false;
        _slideCooldownTimer = m_slideCooldown;
        _slideSpeed = 0f;

        CameraMovementSystem.ResetSlideEffect();

        if (shouldJump && m_enableJump && IsGrounded) {
            colliderCenter = Vector3.zero;
            colliderHeight = standHeight;
            camPosY = m_cameraStandY;
            Jump();
            return;
        }

        if (_crouchInputHeldDuringSlide && Input.Crouch) {
            IsCrouched = true;
            SetCrouchStateServerRpc(true);
        } else {
            if (canStandUp) {
                colliderCenter = Vector3.zero;
                colliderHeight = standHeight;
                camPosY = m_cameraStandY;
            } else {
                IsCrouched = true;
                SetCrouchStateServerRpc(true);
            }
        }
    }

    private void HandleStamina() {
        if (_lastStaminaConsumption != _staminaRemaining) {
            _lastStaminaConsumption = _staminaRemaining;
            Singleton.Instance.GameEvents.OnStaminaConsume?.Invoke(_staminaRemaining);
        }
    }

    private void Crouch() {
        if (IsCrouched) return;
        IsCrouched = true;

        SetCrouchStateServerRpc(true);

        colliderCenter = new Vector3(0, m_crouchCenter, 0);
        colliderHeight = m_crouchHeight; 
        camPosY = m_cameraCrouchY;        
    }

    private void StandUp() {
        if (!IsCrouched || !canStandUp) return;
        IsCrouched = false;

        SetCrouchStateServerRpc(false);

        colliderCenter = Vector3.zero;
        colliderHeight = standHeight;
        camPosY = m_cameraStandY;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetCrouchStateServerRpc(bool crouched) {
        isCrouched_NV.Value = crouched;
    }

    private void Jump() {
        Animation.OnJump();

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
        _rb.AddForce(0f, m_jumpPower, 0f, ForceMode.Impulse);

        IsGrounded = false;
        coyoteTimer = 0f;
    }

    private void PerformDash() {
        Vector3 dashDir = transform.TransformDirection(movementInput).normalized;
        _dashDirection = new Vector3(dashDir.x, 0f, dashDir.z).normalized;
        
        _isDashing = true;
        _dashTimer = m_dashDuration;

        if (!m_unlimitedSprint) {
            _staminaRemaining -= m_dashStaminaCost;
            _staminaRemaining = Mathf.Max(_staminaRemaining, 0f);
        }

        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(_dashDirection * m_dashForce, ForceMode.VelocityChange);

        CameraMovementSystem.PlayDashEffect(_dashDirection);

        _staminaRegenCooldown = Time.time + m_staminaRegenCooldownTime;
    }
    #endregion

    #region Raycast
    private void RaycastCheck() {
        Vector3 baseCenter = transform.position + _thisCollider.center - (Vector3.up * ((_thisCollider.height / 2f) - _thisCollider.radius));
        Vector3 boxHalfExtents = new Vector3(_thisCollider.radius * 0.9f, .1f, _thisCollider.radius * 0.9f);    
        IsGrounded = Physics.BoxCast(baseCenter, boxHalfExtents, Vector3.down, out _groundHit, Quaternion.identity, m_raycastDistance, m_groundLayerMask);
        if (coyoteTimer == 0)
            IsGrounded = false;

        Vector3 topCenter = transform.position + _thisCollider.center + (Vector3.up * ((_thisCollider.height / 2f) - _thisCollider.radius));

        boxHalfExtents.y *= 3.5f;
        canStandUp = !Physics.BoxCast(topCenter, boxHalfExtents, Vector3.up, out RaycastHit topHit, Quaternion.identity, m_raycastDistance * 2, m_groundLayerMask);
    }
    #endregion

    #region Update
    private void Update() {
        if (!IsOwner || HealthSystem.IsDead || GameManager.GetGameState() == GameState.Paused) return;

        HandleCrouch();
        HandleJump();
        HandleDash();
        HandleSlide();

        if (_isSliding) return;

        HandleSprint();
        HandleStamina();
    }

    private void FixedUpdate() {
        if (!IsOwner || HealthSystem.IsDead) return;

        RaycastCheck();

        if (GameManager.GetGameState() == GameState.Paused) return;

        HandleMovement();        
    }
    #endregion

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
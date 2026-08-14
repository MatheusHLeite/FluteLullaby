using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Player_CameraMovementSystem : NetworkBehaviour {
    [Header("References")]
    [SerializeField] private Transform m_playerCameraHolder;
    [SerializeField] private Transform m_weaponsHolder;
    [SerializeField] private Camera m_playerCamera;

    [Header("Sway")]
    [SerializeField] private float swayIntensity = 0.075f;
    [SerializeField] private float swaySmoothness = 4f;
    [SerializeField] private float swayMovementIntensity = 0.01f;
    [SerializeField] private float swayMovementSpeed = 14f;

    [Header("Weapon Switch Animation")]
    [SerializeField] private float weaponSwitchTiltAngle = 2f;
    [SerializeField] private float weaponSwitchSideAngle = 0.8f;
    [SerializeField] private float weaponSwitchDuration = 0.2f;

    [Header("Dash Effect")]
    [SerializeField] private float dashFOVIncrease = 10f;
    [SerializeField] private float dashEffectDuration = 0.3f;
    [SerializeField] private float dashSideEffectDuration = 0.8f;
    [SerializeField] private float dashSideTiltAngle = 8f;
    [SerializeField] private float dashBackwardTiltAngle = 5f;
    [SerializeField] private float dashSideYawOffset = 6f;
    [SerializeField] private float dashBackwardFOVReduction = 8f;
    [SerializeField] private float dashMotionBlurIntensity = 0.5f;

    [Header("Slide Effect")]
    [SerializeField] private float slideEffectDuration = 0.3f;

    [Header("Speed Shake")]
    [SerializeField] private bool enableSpeedShake = true;
    [SerializeField] private float shakeSpeedThreshold = 10f;
    [SerializeField] private float shakeMaxSpeed = 20f;
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private float shakeFrequency = 25f;

    [Header("Head Bob")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float jumpBobAmount = 0.4f;
    [SerializeField] private float landBobAmount = 0.6f;

    #region Parameters
    private bool m_cameraCanMove;
    private bool m_enableZoom;
    #endregion
    
    #region Weapon Switch Offset
    private float weaponSwitchPitchOffset;
    private float weaponSwitchYawOffset;
    #endregion

    #region Dash Effect Offset
    private float dashFOVOffset;
    private float dashTiltOffset;
    private float dashPitchOffset;
    private float dashYawOffset;
    #endregion

    #region Slide Effect Offset
    private float slideCameraRollOffset;
    #endregion

    #region Speed Shake Offset
    private float speedShakeTimer;
    private float speedShakeOffsetX;
    private float speedShakeOffsetY;
    #endregion
    
    #region Head Bob Offset
    private float headBobTimer;
    private float headBobVerticalOffset;
    private float headBobHorizontalOffset;
    private bool wasGrounded = true;
    #endregion 

    #region Private references
    private Player_InputHandler Input;
    private Player_MovementSystem Movement;
    private Player_HealthSystem HealthSystem;
    private MotionBlur motionBlur;
    private VolumeProfile postProcessingVolumeProfile;
    #endregion

    #region Performance Cache
    private Vector3 cachedVelocity;
    private float cachedVelocityMagnitude;
    #endregion

    #region Private variables  
    private float m_mouseSensitivity;
    private float m_sensitivityMultiplier;
    private float m_maxPositiveLookAngle;
    private float m_maxNegativeLookAngle;    
    private float m_defaultFov;
    private float m_zoomFOV;
    private float m_zoomStepTime;
    private float m_actualFov;

    private bool m_invertCamera;
    private bool m_cameraBalance = true;

    private float m_maxCameraZRotation = 6;
    private float m_cameraZRotationTime = 3;
    private float m_cameraZRotationMultiplier;

    private float _yaw;
    private float _pitch;
    private float _zRotation;

    private Quaternion originalRotation;
    private Vector3 initialPosition;
    private float counter;
    #endregion

    #region Public variables
    public bool IsZoomed { get; private set; }
    public Camera GetPlayerCamera => m_playerCamera;
    public Transform GetPlayerCameraHolder => m_playerCameraHolder;
    #endregion

    #region Network variables
    private NetworkVariable<Quaternion> cameraRotation = new(
        writePerm: NetworkVariableWritePermission.Owner
    );
    #endregion

    #region Initialization
    private void Awake() {
        Input = GetComponent<Player_InputHandler>();
        Movement = GetComponent<Player_MovementSystem>();
        HealthSystem = GetComponent<Player_HealthSystem>();

        originalRotation = m_weaponsHolder.transform.localRotation;
        initialPosition = m_weaponsHolder.transform.localPosition;

        postProcessingVolumeProfile = Singleton.Instance.SettingsManager.VolumeProfile;

        if (postProcessingVolumeProfile != null && postProcessingVolumeProfile.TryGet(out MotionBlur mb)) 
            motionBlur = mb;        
    }

    public void SetPlayerParameters(PlayerParameters_SO playerParameters) {
        m_sensitivityMultiplier = playerParameters.m_sensitivityMultiplier;
        m_maxPositiveLookAngle = playerParameters.m_maxPositiveLookAngle;
        m_maxNegativeLookAngle = playerParameters.m_maxNegativeLookAngle;
        m_defaultFov = playerParameters.m_defaultFov;
        m_zoomFOV = playerParameters.m_zoomFOV;
        m_zoomStepTime = playerParameters.m_zoomStepTime;

        enableSpeedShake = playerParameters.m_enableSpeedShake;
        shakeSpeedThreshold = playerParameters.m_shakeSpeedThreshold;
        shakeMaxSpeed = playerParameters.m_shakeMaxSpeed;
        shakeIntensity = playerParameters.m_shakeIntensity;
        shakeFrequency = playerParameters.m_shakeFrequency;

        m_cameraCanMove = true;
        m_enableZoom = true;
        m_playerCamera.fieldOfView = m_defaultFov;
    }
    #endregion
    
    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        if (!isOwner) 
            m_playerCamera.gameObject.SetActive(false);        
        else {
            Singleton.Instance.GameEvents.OnSensitivityChanged.AddListener(OnSensitivityChanged);
            Singleton.Instance.GameEvents.OnInvertAxisChanged.AddListener(CheckInvertCameraEnabled);
            Singleton.Instance.GameEvents.OnCameraBobEnabledChanged.AddListener(CheckCameraBalanceEnabled);
        }                  
    }

    public void DeinitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        Singleton.Instance.GameEvents.OnSensitivityChanged.RemoveListener(OnSensitivityChanged);
        Singleton.Instance.GameEvents.OnInvertAxisChanged.RemoveListener(CheckInvertCameraEnabled);
        Singleton.Instance.GameEvents.OnCameraBobEnabledChanged.RemoveListener(CheckCameraBalanceEnabled);
    }
    #endregion

    private void OnSensitivityChanged(float value) {
        m_mouseSensitivity = value;
    }

    private void CheckInvertCameraEnabled(int i) {
        m_invertCamera = i == 1;
    }

    private void CheckCameraBalanceEnabled(int i) {
        m_cameraBalance = i == 1;
    }

    public void SetCameraGameObjectActive(bool active) {
        m_playerCamera.gameObject.SetActive(active);
    }

    private void HandleCameraMovement() {
        if (!m_cameraCanMove) {
            ResetCameraBalance();
            return;
        }

        HandleWeaponSway();
        HandleHeadBob();
        HandleSpeedShake();

        _yaw += (Input.LookInput.x * m_sensitivityMultiplier) * m_mouseSensitivity;

        _pitch += m_invertCamera ? m_mouseSensitivity * (Input.LookInput.y * m_sensitivityMultiplier) : m_mouseSensitivity * (-Input.LookInput.y * m_sensitivityMultiplier);
        _pitch = Mathf.Clamp(_pitch, -m_maxNegativeLookAngle, m_maxPositiveLookAngle);

        m_cameraZRotationMultiplier = Input.MoveInput.y != 0 ? m_maxCameraZRotation / 2 : m_maxCameraZRotation;
        _zRotation = Mathf.Lerp(_zRotation, Movement.IsGrounded && !Movement.IsSliding ? (Input.MoveInput.x * m_cameraZRotationMultiplier) : 0, Time.deltaTime * m_cameraZRotationTime);
        
        float finalZRotation = -_zRotation + slideCameraRollOffset + dashTiltOffset;
        
        transform.localEulerAngles = new Vector3(0, _yaw + weaponSwitchYawOffset + headBobHorizontalOffset + speedShakeOffsetY + dashYawOffset, 0);
        m_playerCameraHolder.localEulerAngles = new Vector3(_pitch + weaponSwitchPitchOffset + headBobVerticalOffset + speedShakeOffsetX + dashPitchOffset, 0, m_cameraBalance ? finalZRotation : 0);
    }

    private void HandleWeaponSway() {
        Quaternion rotX = Quaternion.AngleAxis(-Input.LookInput.y * swayIntensity, Vector3.right);
        Quaternion rotY = Quaternion.AngleAxis(-Input.LookInput.x * swayIntensity, Vector3.up);

        Quaternion finalRot = originalRotation * rotX * rotY;
        Vector3 finalPos;        

        if (Mathf.Abs(Input.MoveInput.x) > 0.1f || Mathf.Abs(Input.MoveInput.y) > 0.1f) {
            counter += Time.deltaTime * swayMovementSpeed * (Movement.IsSprinting ? 1.45f : 1f);
            float intensity = swayMovementIntensity * (Movement.IsSprinting ? 1.35f : 1f);

            float offsetY = Mathf.Cos(counter) * intensity;
            float offsetX = Mathf.Cos(counter / 2) * intensity;

            finalPos = initialPosition + new Vector3(offsetX, offsetY, 0);
        }
        else {
            finalPos = initialPosition;
            counter = 0f;
        }

        m_weaponsHolder.transform.localRotation = Quaternion.Slerp(m_weaponsHolder.transform.localRotation, finalRot, Time.deltaTime * swaySmoothness);
        m_weaponsHolder.transform.localPosition = Vector3.Lerp(m_weaponsHolder.transform.localPosition, finalPos, Time.deltaTime * swayMovementSpeed);
    }

    private void HandleSpeedShake() {
        if (!enableSpeedShake) {
            speedShakeOffsetX = 0f;
            speedShakeOffsetY = 0f;
            return;
        }

        float currentSpeed = cachedVelocityMagnitude;

        if (currentSpeed >= shakeSpeedThreshold) {
            float speedRatio = Mathf.Clamp01((currentSpeed - shakeSpeedThreshold) / (shakeMaxSpeed - shakeSpeedThreshold));
            float currentIntensity = shakeIntensity * speedRatio;

            speedShakeTimer += Time.deltaTime * shakeFrequency;

            speedShakeOffsetX = Mathf.Sin(speedShakeTimer) * currentIntensity;
            speedShakeOffsetY = Mathf.Cos(speedShakeTimer * 1.3f) * currentIntensity * 0.7f;
        }
        else {
            speedShakeOffsetX = Mathf.Lerp(speedShakeOffsetX, 0f, Time.deltaTime * 8f);
            speedShakeOffsetY = Mathf.Lerp(speedShakeOffsetY, 0f, Time.deltaTime * 8f);

            if (Mathf.Abs(speedShakeOffsetX) < 0.01f && Mathf.Abs(speedShakeOffsetY) < 0.01f) {
                speedShakeOffsetX = 0f;
                speedShakeOffsetY = 0f;
                speedShakeTimer = 0f;
            }
        }
    }

    private void HandleHeadBob() {
        bool isMoving = Mathf.Abs(Input.MoveInput.x) > 0.1f || Mathf.Abs(Input.MoveInput.y) > 0.1f;

        if (!Movement.IsGrounded) {
            if (wasGrounded) {
                DOTween.Kill("headBob");
                DOTween.To(() => headBobVerticalOffset, x => headBobVerticalOffset = x, jumpBobAmount, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetId("headBob");
                wasGrounded = false;
            }
            return;
        }

        if (!wasGrounded) {
            DOTween.Kill("headBob");
            
            Sequence landSequence = DOTween.Sequence();
            landSequence.SetId("headBob");
            landSequence.Append(DOTween.To(() => headBobVerticalOffset, x => headBobVerticalOffset = x, -landBobAmount, 0.1f).SetEase(Ease.OutQuad));
            landSequence.Append(DOTween.To(() => headBobVerticalOffset, x => headBobVerticalOffset = x, 0f, 0.2f).SetEase(Ease.OutBack));
            
            wasGrounded = true;
            return;
        }

        if (!isMoving) {
            headBobTimer = 0f;
            headBobVerticalOffset = Mathf.Lerp(headBobVerticalOffset, 0f, Time.deltaTime * 8f);
            headBobHorizontalOffset = Mathf.Lerp(headBobHorizontalOffset, 0f, Time.deltaTime * 8f);
            return;
        }

        float bobSpeed = Movement.IsSprinting ? sprintBobSpeed : walkBobSpeed;
        float bobAmount = Movement.IsSprinting ? sprintBobAmount : walkBobAmount;

        headBobTimer += Time.deltaTime * bobSpeed;

        float targetVertical = Mathf.Sin(headBobTimer) * bobAmount;
        float targetHorizontal = Mathf.Cos(headBobTimer * 0.5f) * bobAmount * 0.5f;

        headBobVerticalOffset = Mathf.Lerp(headBobVerticalOffset, targetVertical, Time.deltaTime * 10f);
        headBobHorizontalOffset = Mathf.Lerp(headBobHorizontalOffset, targetHorizontal, Time.deltaTime * 10f);
    }

    private void HandleCameraZoom() {
        if (!m_enableZoom) return;

        IsZoomed = Input.Zoom;

        m_actualFov = IsZoomed ? m_zoomFOV : m_defaultFov;

        if (Movement.IsSprinting)
            m_actualFov = m_defaultFov;

        float targetFOV = m_actualFov + dashFOVOffset;

        if (m_playerCamera.fieldOfView != targetFOV)
            m_playerCamera.fieldOfView = Mathf.Lerp(m_playerCamera.fieldOfView, targetFOV, m_zoomStepTime * Time.deltaTime);
    }

    private void HandleNetworkCameraRotation() {
        if (IsOwner) 
            cameraRotation.Value = m_playerCameraHolder.localRotation;        
        else
            m_playerCameraHolder.localRotation = cameraRotation.Value;        
    }

    private void ResetCameraBalance() {
        if (!m_cameraBalance) return;

        _zRotation = Mathf.Lerp(_zRotation, 0, Time.deltaTime * m_cameraZRotationTime);
        Vector3 localEA = new Vector3(_pitch, 0, -_zRotation);

        if (m_playerCameraHolder.localEulerAngles != localEA) { 
            m_playerCameraHolder.localEulerAngles = localEA; 
        }
    }

    public void PlayWeaponSwitchAnimation() {
        if (!IsOwner) return;

        DOTween.Kill(this);
        
        weaponSwitchPitchOffset = 0f;
        weaponSwitchYawOffset = 0f;

        float randomSideAngle = Random.Range(-weaponSwitchSideAngle, weaponSwitchSideAngle);

        Sequence switchSequence = DOTween.Sequence();
        switchSequence.SetTarget(this);

        switchSequence.Append(DOTween.To(() => weaponSwitchPitchOffset, x => weaponSwitchPitchOffset = x, -weaponSwitchTiltAngle, weaponSwitchDuration).SetEase(Ease.OutQuad));
        switchSequence.Join(DOTween.To(() => weaponSwitchYawOffset, x => weaponSwitchYawOffset = x, randomSideAngle, weaponSwitchDuration).SetEase(Ease.OutQuad));

        switchSequence.Append(DOTween.To(() => weaponSwitchPitchOffset, x => weaponSwitchPitchOffset = x, 0f, weaponSwitchDuration).SetEase(Ease.OutBack));
        switchSequence.Join(DOTween.To(() => weaponSwitchYawOffset, x => weaponSwitchYawOffset = x, 0f, weaponSwitchDuration).SetEase(Ease.OutBack));
        
        switchSequence.OnComplete(() => {
            weaponSwitchPitchOffset = 0f;
            weaponSwitchYawOffset = 0f;
        });
        
        switchSequence.OnKill(() => {
            weaponSwitchPitchOffset = 0f;
            weaponSwitchYawOffset = 0f;
        });
    }

    public void PlayDashEffect(Vector3 dashDirection) {
        if (!IsOwner) return;

        DOTween.Kill("dashEffect");

        dashFOVOffset = 0f;
        dashTiltOffset = 0f;
        dashPitchOffset = 0f;
        dashYawOffset = 0f;

        Vector3 localDashDirection = transform.InverseTransformDirection(dashDirection);
        
        float forwardAmount = localDashDirection.z;
        float rightAmount = localDashDirection.x;

        bool isDashingForward = forwardAmount > 0.7f;
        bool isDashingBackward = forwardAmount < -0.7f;
        bool isDashingRight = rightAmount > 0.7f;
        bool isDashingLeft = rightAmount < -0.7f;

        Sequence dashSequence = DOTween.Sequence();
        dashSequence.SetId("dashEffect");

        if (isDashingForward) {
            dashSequence.Append(DOTween.To(() => dashFOVOffset, x => dashFOVOffset = x, dashFOVIncrease, dashEffectDuration * 0.3f).SetEase(Ease.OutQuad));
            dashSequence.Append(DOTween.To(() => dashFOVOffset, x => dashFOVOffset = x, 0f, dashEffectDuration * 0.7f).SetEase(Ease.OutCubic));
            
            if (motionBlur != null) {
                float originalIntensity = motionBlur.intensity.value;
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, dashMotionBlurIntensity, dashEffectDuration * 0.15f).SetEase(Ease.OutQuad));
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, originalIntensity, dashEffectDuration * 0.85f).SetEase(Ease.OutCubic).SetDelay(dashEffectDuration * 0.15f));
            }
        }
        else if (isDashingBackward) {
            dashSequence.Append(DOTween.To(() => dashPitchOffset, x => dashPitchOffset = x, dashBackwardTiltAngle, dashEffectDuration * 0.35f).SetEase(Ease.OutQuad));
            dashSequence.Append(DOTween.To(() => dashPitchOffset, x => dashPitchOffset = x, 0f, dashEffectDuration * 0.65f).SetEase(Ease.InOutCubic));
            
            dashSequence.Join(DOTween.To(() => dashFOVOffset, x => dashFOVOffset = x, -dashBackwardFOVReduction, dashEffectDuration * 0.25f).SetEase(Ease.OutQuad));
            dashSequence.Join(DOTween.To(() => dashFOVOffset, x => dashFOVOffset = x, 0f, dashEffectDuration * 0.75f).SetEase(Ease.InOutCubic).SetDelay(dashEffectDuration * 0.25f));
            
            dashSequence.Join(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, -dashBackwardTiltAngle * 0.4f, dashEffectDuration * 0.2f).SetEase(Ease.OutQuad));
            dashSequence.Join(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, dashBackwardTiltAngle * 0.4f, dashEffectDuration * 0.4f).SetEase(Ease.InOutSine).SetDelay(dashEffectDuration * 0.2f));
            dashSequence.Join(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, 0f, dashEffectDuration * 0.4f).SetEase(Ease.OutCubic).SetDelay(dashEffectDuration * 0.6f));
            
            if (motionBlur != null) {
                float originalIntensity = motionBlur.intensity.value;
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, dashMotionBlurIntensity * 0.6f, dashEffectDuration * 0.2f).SetEase(Ease.OutQuad));
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, originalIntensity, dashEffectDuration * 0.8f).SetEase(Ease.OutCubic).SetDelay(dashEffectDuration * 0.2f));
            }
        }
        else if (isDashingRight) {
            float tiltAngle = -dashSideTiltAngle;
            float yawOffset = -dashSideYawOffset;
            
            dashSequence.Append(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, tiltAngle, dashSideEffectDuration * 0.2f).SetEase(Ease.OutQuad));
            dashSequence.Append(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, 0f, dashSideEffectDuration * 0.8f).SetEase(Ease.OutCubic));
            
            dashSequence.Join(DOTween.To(() => dashYawOffset, x => dashYawOffset = x, yawOffset, dashSideEffectDuration * 0.25f).SetEase(Ease.OutSine));
            dashSequence.Join(DOTween.To(() => dashYawOffset, x => dashYawOffset = x, 0f, dashSideEffectDuration * 0.75f).SetEase(Ease.OutCubic).SetDelay(dashSideEffectDuration * 0.25f));

            if (motionBlur != null) {
                float originalIntensity = motionBlur.intensity.value;
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, dashMotionBlurIntensity * 0.8f, dashSideEffectDuration * 0.12f).SetEase(Ease.OutQuad));
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, originalIntensity, dashSideEffectDuration * 0.88f).SetEase(Ease.OutCubic).SetDelay(dashSideEffectDuration * 0.12f));
            }
        }
        else if (isDashingLeft) {
            float tiltAngle = dashSideTiltAngle;
            float yawOffset = dashSideYawOffset;
            
            dashSequence.Append(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, tiltAngle, dashSideEffectDuration * 0.2f).SetEase(Ease.OutQuad));
            dashSequence.Append(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, 0f, dashSideEffectDuration * 0.8f).SetEase(Ease.OutCubic));
            
            dashSequence.Join(DOTween.To(() => dashYawOffset, x => dashYawOffset = x, yawOffset, dashSideEffectDuration * 0.25f).SetEase(Ease.OutSine));
            dashSequence.Join(DOTween.To(() => dashYawOffset, x => dashYawOffset = x, 0f, dashSideEffectDuration * 0.75f).SetEase(Ease.OutCubic).SetDelay(dashSideEffectDuration * 0.25f));
            
            if (motionBlur != null) {
                float originalIntensity = motionBlur.intensity.value;
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, dashMotionBlurIntensity * 0.8f, dashSideEffectDuration * 0.12f).SetEase(Ease.OutQuad));
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, originalIntensity, dashSideEffectDuration * 0.88f).SetEase(Ease.OutCubic).SetDelay(dashSideEffectDuration * 0.12f));
            }
        }
        else {
            float diagonalTilt = rightAmount * dashSideTiltAngle * -0.5f;
            float diagonalYaw = rightAmount * dashSideYawOffset * -0.5f;

            dashSequence.Append(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, diagonalTilt, dashEffectDuration * 0.2f).SetEase(Ease.OutQuad));
            dashSequence.Append(DOTween.To(() => dashTiltOffset, x => dashTiltOffset = x, 0f, dashEffectDuration * 0.8f).SetEase(Ease.OutCubic));
            
            dashSequence.Join(DOTween.To(() => dashYawOffset, x => dashYawOffset = x, diagonalYaw, dashEffectDuration * 0.25f).SetEase(Ease.OutSine));
            dashSequence.Join(DOTween.To(() => dashYawOffset, x => dashYawOffset = x, 0f, dashEffectDuration * 0.75f).SetEase(Ease.OutCubic).SetDelay(dashEffectDuration * 0.25f));

            dashSequence.Join(DOTween.To(() => dashFOVOffset, x => dashFOVOffset = x, dashFOVIncrease * 0.6f, dashEffectDuration * 0.3f).SetEase(Ease.OutQuad));
            dashSequence.Join(DOTween.To(() => dashFOVOffset, x => dashFOVOffset = x, 0f, dashEffectDuration * 0.7f).SetEase(Ease.OutCubic));
            
            if (motionBlur != null) {
                float originalIntensity = motionBlur.intensity.value;
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, dashMotionBlurIntensity * 0.7f, dashEffectDuration * 0.15f).SetEase(Ease.OutQuad));
                dashSequence.Join(DOTween.To(() => motionBlur.intensity.value, x => motionBlur.intensity.value = x, originalIntensity, dashEffectDuration * 0.85f).SetEase(Ease.OutCubic).SetDelay(dashEffectDuration * 0.15f));
            }
        }

        dashSequence.OnComplete(() => {
            dashFOVOffset = 0f;
            dashTiltOffset = 0f;
            dashPitchOffset = 0f;
            dashYawOffset = 0f;
        });
        dashSequence.OnKill(() => {
            dashFOVOffset = 0f;
            dashTiltOffset = 0f;
            dashPitchOffset = 0f;
            dashYawOffset = 0f;
        });
    }

    public void PlaySlideEffect(float tiltAngle) {
        if (!IsOwner) return;

        DOTween.Kill("slideEffect");
        DOTween.To(() => slideCameraRollOffset, x => slideCameraRollOffset = x, tiltAngle, slideEffectDuration)
            .SetEase(Ease.OutQuad)
            .SetId("slideEffect");
    }

    public void ResetSlideEffect() {
        if (!IsOwner) return;

        DOTween.Kill("slideEffect");

        DOTween.To(() => slideCameraRollOffset, x => slideCameraRollOffset = x, 0f, slideEffectDuration)
            .SetEase(Ease.OutBack)
            .SetId("slideEffect")
            .OnComplete(() => {
                slideCameraRollOffset = 0f;
            })
            .OnKill(() => {
                slideCameraRollOffset = 0f;
            });
    }

    public void Tick(bool isOwner) {
        HandleNetworkCameraRotation();

        if (!IsOwner || HealthSystem.IsDead || GameManager.GetGameState() != GameState.Resumed) {
            ResetCameraBalance();
            return; 
        }

        if (Cursor.lockState == CursorLockMode.None) return;
 
        HandleCameraMovement();
        HandleCameraZoom();
    }

    public void FixedTick(bool isOwner) {
        if (Movement != null && Movement.GetRigidbody != null) {
            cachedVelocity = Movement.GetRigidbody.linearVelocity;
            cachedVelocityMagnitude = new Vector3(cachedVelocity.x, 0f, cachedVelocity.z).magnitude;
        }
    }
}
using Unity.Netcode;
using UnityEngine;

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

    #region Parameters
    private bool m_cameraCanMove;
    private bool m_enableZoom;
    #endregion 

    #region Private references
    private Player_InputHandler Input;
    private Player_MovementSystem Movement;
    private Player_HealthSystem HealthSystem;
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
    }

    public void SetPlayerParameters(PlayerParameters_SO playerParameters) {
        m_sensitivityMultiplier = playerParameters.m_sensitivityMultiplier;
        m_maxPositiveLookAngle = playerParameters.m_maxPositiveLookAngle;
        m_maxNegativeLookAngle = playerParameters.m_maxNegativeLookAngle;
        m_defaultFov = playerParameters.m_defaultFov;
        m_zoomFOV = playerParameters.m_zoomFOV;
        m_zoomStepTime = playerParameters.m_zoomStepTime;

        m_cameraCanMove = true;
        m_enableZoom = true;
        m_playerCamera.fieldOfView = m_defaultFov;
    }
    #endregion
    //TROCAR DE ARMA FAZ A TELA MEXER
    #region Network Initialization
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (!IsOwner) 
            m_playerCamera.gameObject.SetActive(false);        
        else {
            Singleton.Instance.GameEvents.OnSensitivityChanged.AddListener(OnSensitivityChanged);
            Singleton.Instance.GameEvents.OnInvertAxisChanged.AddListener(CheckInvertCameraEnabled);
            Singleton.Instance.GameEvents.OnCameraBobEnabledChanged.AddListener(CheckCameraBalanceEnabled);
        }                  
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSensitivityChanged.RemoveListener(OnSensitivityChanged);
            Singleton.Instance.GameEvents.OnInvertAxisChanged.RemoveListener(CheckInvertCameraEnabled);
            Singleton.Instance.GameEvents.OnCameraBobEnabledChanged.RemoveListener(CheckCameraBalanceEnabled);
        }
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

        _yaw = transform.localEulerAngles.y + (Input.LookInput.x * m_sensitivityMultiplier) * m_mouseSensitivity;

        _pitch += m_invertCamera ? m_mouseSensitivity * (Input.LookInput.y * m_sensitivityMultiplier) : m_mouseSensitivity * (-Input.LookInput.y * m_sensitivityMultiplier);
        _pitch = Mathf.Clamp(_pitch, -m_maxNegativeLookAngle, m_maxPositiveLookAngle);

        m_cameraZRotationMultiplier = Input.MoveInput.y != 0 ? m_maxCameraZRotation / 2 : m_maxCameraZRotation;
        _zRotation = Mathf.Lerp(_zRotation, Movement.IsGrounded ? (Input.MoveInput.x * m_cameraZRotationMultiplier) : 0, Time.deltaTime * m_cameraZRotationTime);
        
        transform.localEulerAngles = new Vector3(0, _yaw, 0);
        m_playerCameraHolder.localEulerAngles = new Vector3(_pitch, 0, m_cameraBalance  ? -_zRotation: 0);
    }

    private void HandleWeaponSway() {
        Quaternion rotX = Quaternion.AngleAxis(-Input.LookInput.y * swayIntensity, Vector3.right);
        Quaternion rotY = Quaternion.AngleAxis(-Input.LookInput.x * swayIntensity, Vector3.up);

        Quaternion finalRot = originalRotation * rotX * rotY;
        Vector3 finalPos;        

        if (Mathf.Abs(Input.MoveInput.x) > 0.1f || Mathf.Abs(Input.MoveInput.y) > 0.1f) {
            counter += Time.deltaTime * swayMovementSpeed;
            float offsetY = Mathf.Cos(counter) * swayMovementIntensity;
            float offsetX = Mathf.Cos(counter / 2) * swayMovementIntensity;

            finalPos = initialPosition + new Vector3(offsetX, offsetY, 0);
        }
        else {
            finalPos = initialPosition;
            counter = 0f;
        }

        m_weaponsHolder.transform.localRotation = Quaternion.Slerp(m_weaponsHolder.transform.localRotation, finalRot, Time.deltaTime * swaySmoothness);
        m_weaponsHolder.transform.localPosition = Vector3.Lerp(m_weaponsHolder.transform.localPosition, finalPos, Time.deltaTime * swayMovementSpeed);
    }

    private void HandleCameraZoom() {
        if (!m_enableZoom) return;

        IsZoomed = Input.Zoom;

        m_actualFov = IsZoomed ? m_zoomFOV : m_defaultFov;

        if (Movement.IsSprinting)
            m_actualFov = m_defaultFov;

        if (m_playerCamera.fieldOfView != m_actualFov)
            m_playerCamera.fieldOfView = Mathf.Lerp(m_playerCamera.fieldOfView, m_actualFov, m_zoomStepTime * Time.deltaTime);
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

    private void Update() {
        HandleNetworkCameraRotation();

        if (!IsOwner || HealthSystem.IsDead || GameManager.GetGameState() != GameState.Resumed) {
            ResetCameraBalance();
            return; 
        }

        if (Cursor.lockState == CursorLockMode.None) return;
 
        HandleCameraMovement();
        HandleCameraZoom();
    }
}
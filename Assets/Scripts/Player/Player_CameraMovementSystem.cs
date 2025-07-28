using Unity.Netcode;
using UnityEngine;

public class Player_CameraMovementSystem : NetworkBehaviour {
    [SerializeField] private PlayerParameters_SO m_playerParameters;

    [Header("References")]
    [SerializeField] private Transform m_playerCameraHolder;
    [SerializeField] private Camera m_playerCamera;

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

    private Vector3 originalPosition;
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

        originalPosition = m_playerCamera.transform.localPosition;
    }

    public void SetPlayerParameters() {
        m_mouseSensitivity = m_playerParameters.m_mouseSensitivity;
        m_sensitivityMultiplier = m_playerParameters.m_sensitivityMultiplier;
        m_maxPositiveLookAngle = m_playerParameters.m_maxPositiveLookAngle;
        m_maxNegativeLookAngle = m_playerParameters.m_maxNegativeLookAngle;
        m_invertCamera = m_playerParameters.m_invertCamera;
        m_defaultFov = m_playerParameters.m_defaultFov;
        m_zoomFOV = m_playerParameters.m_zoomFOV;
        m_zoomStepTime = m_playerParameters.m_zoomStepTime;

        m_cameraCanMove = true;
        m_enableZoom = true;
        m_playerCamera.fieldOfView = m_defaultFov;

        UpdatePlayerPreferences();
    }
    #endregion

    #region Network Initialization
    public override void OnNetworkSpawn() {
        base.OnNetworkSpawn();

        if (!IsOwner) 
            m_playerCamera.gameObject.SetActive(false);        
        else {
            Singleton.Instance.GameEvents.OnSensitivityChange.AddListener(OnSensitivityChanged);
            SetPlayerParameters(); //[TODO] Add to a manager  
        }                  
    }

    public override void OnNetworkDespawn() {
        base.OnNetworkDespawn();

        if (IsOwner) {
            Singleton.Instance.GameEvents.OnSensitivityChange.RemoveListener(OnSensitivityChanged);
        }
    }
    #endregion

    private void UpdatePlayerPreferences() {
        //m_mouseSensitivity = m_mouseSensitivity;
    }

    private void OnSensitivityChanged(float value) {
        m_mouseSensitivity = value;
    }

    public void SetCameraGameObjectActive(bool active) {
        m_playerCamera.gameObject.SetActive(active);
    }

    private void HandleCameraMovement() {
        if (!m_cameraCanMove) return;

        _yaw = transform.localEulerAngles.y + (Input.LookInput.x * m_sensitivityMultiplier) * m_mouseSensitivity;

        _pitch += m_invertCamera ? m_mouseSensitivity * (Input.LookInput.y * m_sensitivityMultiplier) : m_mouseSensitivity * (-Input.LookInput.y * m_sensitivityMultiplier);
        _pitch = Mathf.Clamp(_pitch, -m_maxNegativeLookAngle, m_maxPositiveLookAngle);

        m_cameraZRotationMultiplier = Input.MoveInput.y != 0 ? m_maxCameraZRotation / 2 : m_maxCameraZRotation;
        _zRotation = Mathf.Lerp(_zRotation, Movement.IsGrounded ? (Input.MoveInput.x * m_cameraZRotationMultiplier) : 0, Time.deltaTime * m_cameraZRotationTime);
        
        transform.localEulerAngles = new Vector3(0, _yaw, 0);
        m_playerCameraHolder.localEulerAngles = new Vector3(_pitch, 0, m_cameraBalance  ? -_zRotation: 0);
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

    private void Update() {
        HandleNetworkCameraRotation();

        if (!IsOwner || HealthSystem.IsDead) return;

        if (Cursor.lockState == CursorLockMode.None) return;
 
        HandleCameraMovement();
        HandleCameraZoom();
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "Default Player Parameters", menuName = "Data/Player Parameters")]
public class PlayerParameters_SO : ScriptableObject {
    [Header("Movement")]
    public float m_walkSpeed = 5f;
    public float m_sprintSpeed = 9f;
    public float m_crouchSpeed = 2f;
    [Space(5)]
    public float m_acceleration = 5f;
    public float m_deceleration = 10;
    public float m_toCrouchSpeed = 12f;

    [Header("Jump")]
    public float m_jumpPower = 5.8f;
    public float m_coyoteTime = 0.245f;
    public float m_maxAirVelocityChange = 0.15f;

    [Header("Sprint")]
    public float m_sprintDuration = 6f;
    public float m_sprintCooldown = .5f;
    public float m_sprintRecoverSpeed = 0.35f;

    [Header("Camera settings")]
    public float m_mouseSensitivity = 2f;
    public bool m_invertCamera;
    public float m_sensitivityMultiplier = .045f; 
    [Space(5)]
    public float m_maxPositiveLookAngle = 75f;
    public float m_maxNegativeLookAngle = 60f;
    
    [Space(5)]
    public float m_defaultFov = 60f;
    public float m_zoomFOV = 30f;
    public float m_zoomStepTime = 5f;
}

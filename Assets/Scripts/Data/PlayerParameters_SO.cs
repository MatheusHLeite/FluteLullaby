using UnityEngine;

[CreateAssetMenu(fileName = "Default Player Parameters", menuName = "Data/Player Parameters")]
public class PlayerParameters_SO : ScriptableObject {
    [Header("Health")]
    public float m_maxHealth = 100f;

    [Header("Stamina")]
    public float m_maxStamina = 6f;
    public float m_staminaRecoverSpeed = 0.35f;
    public float m_staminaRegenCooldownTime = 1.85f;
    public float m_dashStaminaCost = 2f;
    public float m_runningStaminaCost = .6f;

    [Header("Movement")]
    public float m_walkSpeed = 5f;
    public float m_sprintSpeed = 9f;
    public float m_sprintCooldown = .5f;
    public float m_crouchSpeed = 2f;
    [Space(5)]
    public float m_acceleration = 5f;
    public float m_deceleration = 10;
    public float m_toCrouchSpeed = 12f;

    [Header("Jump")]
    public float m_jumpPower = 5.8f;
    public float m_coyoteTime = 0.245f;
    public float m_maxAirVelocityChange = 0.15f;

    [Header("Dash")]
    public float m_dashForce = 15f;
    public float m_dashDuration = 0.2f;
    public float m_dashCooldown = 1f;

    [Header("Slide")]
    public float m_slideDuration = 0.8f;
    public float m_slideDeceleration = 2f;
    public float m_slideSideControl = 0.3f;
    public float m_slideCooldown = 1.2f;
    public float m_slideStaminaCost = 0.5f;
    public float m_minSprintSpeedToSlide = 4f;
    public float m_slideSpeedBoost = 2f;
    public float m_slideDownhillAcceleration = 4f;
    public float m_slideDownhillAngleThreshold = 10f;
    public float m_slideMaxDownhillSpeed = 20f;

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

    [Header("Camera Shake (Speed Based)")]
    public bool m_enableSpeedShake = true;
    public float m_shakeSpeedThreshold = 10f;
    public float m_shakeMaxSpeed = 20f;
    public float m_shakeIntensity = 0.15f;
    public float m_shakeFrequency = 25f;
}

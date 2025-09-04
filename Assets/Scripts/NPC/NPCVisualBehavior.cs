using UnityEngine;

public class NPCVisualBehavior : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private Transform m_head;
    [SerializeField] private Transform[] m_eyes;
    [SerializeField] private Transform[] m_eyesClosed;

    [Header("Values")]
    [SerializeField] private float lookRange = 8f;
    [SerializeField] private float rotationSpeed = 3.75f;
    [SerializeField] private float maxEyeRotationAngle = 105f;
    [SerializeField] private float maxRotationAngle = 48f;

    private Transform playerCamera;

    private bool isDead;

    private Quaternion initialRotation;
    private Vector3 initialForward;
    private Quaternion[] initialEyeRotations;
    private Vector3[] initialEyeForwards;

    private Vector3 direction;
    private float angleToPlayer;
    private Quaternion targetRotation;
    private Quaternion limitedRotation;

    private float eyeAngleToPlayer;
    private Quaternion eyeTargetRotation;
    private Quaternion limitedEyeRotation;

    void Start() {
        initialRotation = m_head.rotation;
        initialForward = initialRotation * Vector3.forward;

        initialEyeRotations = new Quaternion[m_eyes.Length];
        initialEyeForwards = new Vector3[m_eyes.Length];

        for (int i = 0; i < m_eyes.Length; i++) {
            initialEyeRotations[i] = m_eyes[i].rotation;
            initialEyeForwards[i] = initialEyeRotations[i] * Vector3.forward;
        }

        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = lookRange;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out Player_Manager player)) {
            playerCamera = player.GetPlayerCamera().transform;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.TryGetComponent(out Player_Manager player)) {
            playerCamera = null;
        }
    }

    private void HandleVariables() {
        if (isDead) {
            if (m_head.localRotation != Quaternion.identity)
                m_head.localRotation = Quaternion.Slerp(m_head.localRotation, Quaternion.identity, rotationSpeed * Time.deltaTime);
            return;
        }

        if (playerCamera == null) {
            if (m_head.rotation != initialRotation)
                m_head.rotation = Quaternion.Slerp(m_head.rotation, initialRotation, rotationSpeed * Time.deltaTime);

            for (int i = 0; i < m_eyes.Length; i++) {
                if (m_eyes[i].rotation != initialEyeRotations[i])
                    m_eyes[i].rotation = Quaternion.Slerp(m_eyes[i].rotation, initialEyeRotations[i], rotationSpeed * 2.15f * Time.deltaTime);
            }
            return;
        }

        direction = playerCamera.position - m_head.position;
        direction.Normalize();

        angleToPlayer = Vector3.Angle(initialForward, direction);
        targetRotation = Quaternion.LookRotation(direction);
    }

    private void RotateEyes() {
        if (playerCamera == null) { return; }

        for (int i = 0; i < m_eyes.Length; i++) {
            eyeAngleToPlayer = Vector3.Angle(initialEyeForwards[i], direction);
            eyeTargetRotation = Quaternion.LookRotation(direction);

            if (eyeAngleToPlayer <= maxEyeRotationAngle)            
                m_eyes[i].rotation = Quaternion.Slerp(m_eyes[i].rotation, eyeTargetRotation, rotationSpeed * 2.15f * Time.deltaTime);            
            else {
                limitedEyeRotation = Quaternion.Slerp(Quaternion.LookRotation(initialEyeForwards[i]), eyeTargetRotation, maxEyeRotationAngle / eyeAngleToPlayer);
                m_eyes[i].rotation = Quaternion.Slerp(m_eyes[i].rotation, limitedEyeRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    private void LookAtPlayer() {
        if (playerCamera == null) { return; }

        if (angleToPlayer <= maxRotationAngle) {            
            if (m_head.rotation != targetRotation)
                m_head.rotation = Quaternion.Slerp(m_head.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            return;
        }

        limitedRotation = Quaternion.Slerp(Quaternion.LookRotation(initialForward), targetRotation, maxRotationAngle / angleToPlayer);
        m_head.rotation = Quaternion.Slerp(m_head.rotation, limitedRotation, rotationSpeed * Time.deltaTime);
    }

    public void OnDie() {
        foreach (var e in m_eyesClosed) {
            e.gameObject.SetActive(true);
        }

        isDead = true;
    }

    void LateUpdate() {
        HandleVariables();

        if (isDead) return;

        RotateEyes();
        LookAtPlayer();
    }
}

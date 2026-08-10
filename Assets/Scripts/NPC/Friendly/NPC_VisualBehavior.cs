using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class NPC_VisualBehavior : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private Transform m_head;
    [SerializeField] private Transform[] m_eyes;
    [SerializeField] private Transform[] m_eyesClosed;

    [Header("Values")]
    [SerializeField] private float lookRange = 8f;
    [SerializeField] private float rotationSpeed = 3.75f;
    [SerializeField] private float maxEyeRotationAngle = 105f;
    [SerializeField] private float maxRotationAngle = 48f;

    private Global_HealthHandler healthHandler;

    private Transform playerCamera;
    private bool isDead;

    private Quaternion initialHeadRotation;
    private Vector3 initialHeadForward;

    private Quaternion[] initialEyeRotations;
    private Vector3[] initialEyeForwards;

    private Vector3 localDirection;
    private float angleToPlayer;

    private bool targetOutOfView;

    private Coroutine playerCheckRoutine;

    private NetworkList<ulong> playersIds = new NetworkList<ulong>();
    private NetworkVariable<ulong> selectedPlayerId = new NetworkVariable<ulong>(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    #region Network
    public override void OnNetworkSpawn() {
        playersIds.OnListChanged += OnPlayersListChanged;
        selectedPlayerId.OnValueChanged += OnSelectedPlayerChanged;
    }

    public override void OnNetworkDespawn() {
        playersIds.OnListChanged -= OnPlayersListChanged;
        selectedPlayerId.OnValueChanged -= OnSelectedPlayerChanged;
    }

    private void OnPlayersListChanged(NetworkListEvent<ulong> changeEvent) {
        if (!IsServer) return;

        if (playersIds.Count > 0 && playerCheckRoutine == null)
            playerCheckRoutine = StartCoroutine(CheckCamera());

        if (playersIds.Count == 0 && playerCheckRoutine != null) {
            StopCoroutine(playerCheckRoutine);
            playerCheckRoutine = null;
        }

        CheckPlayersList();
    }

    private void OnSelectedPlayerChanged(ulong previousId, ulong newId) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newId, out var netObj))
            playerCamera = netObj.GetComponent<Player_CameraMovementSystem>().GetPlayerCamera.transform;
        else
            playerCamera = null;
    }
    #endregion

    #region Initialization
    void Start() {
        healthHandler = GetComponentInChildren<Global_HealthHandler>();
        if (healthHandler != null)
            healthHandler.m_onDie.AddListener(OnDie);

        initialHeadRotation = m_head.localRotation;
        initialHeadForward = Vector3.forward;

        initialEyeRotations = new Quaternion[m_eyes.Length];
        initialEyeForwards = new Vector3[m_eyes.Length];

        for (int i = 0; i < m_eyes.Length; i++) {
            initialEyeRotations[i] = m_eyes[i].localRotation;
            initialEyeForwards[i] = Vector3.forward;
        }

        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = lookRange;
    }
    #endregion

    #region Trigger
    private void OnTriggerEnter(Collider other) {
        if (!NetworkManager.IsListening) return;

        if (other.TryGetComponent(out Player_Manager player))
            HandlePlayerEnter(player);
    }

    private void OnTriggerExit(Collider other) {
        if (!NetworkManager.IsListening) return;

        if (other.TryGetComponent(out Player_Manager player))
            HandlePlayerExit(player);
    }

    private void HandlePlayerEnter(Player_Manager player) {
        if (IsServer)
            if (!playersIds.Contains(player.NetworkObjectId))
                playersIds.Add(player.NetworkObjectId);
            else
                AddPlayerServerRpc(player.NetworkObjectId);
    }

    private void HandlePlayerExit(Player_Manager player) {
        if (IsServer)
            playersIds.Remove(player.NetworkObjectId);
        else
            RemovePlayerServerRpc(player.NetworkObjectId);
    }
    #endregion

    #region RPC
    [ServerRpc(RequireOwnership = false)]
    private void AddPlayerServerRpc(ulong playerId) {
        if (!playersIds.Contains(playerId))
            playersIds.Add(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RemovePlayerServerRpc(ulong playerId) {
        playersIds.Remove(playerId);
    }
    #endregion

    #region Logic
    private IEnumerator CheckCamera() {
        while (playersIds.Count > 0) {
            yield return new WaitForSeconds(Random.Range(5, 10));
            CheckPlayersList();
        }
    }

    private void CheckPlayersList() {
        selectedPlayerId.Value = playersIds.Count > 0
            ? playersIds[Random.Range(0, playersIds.Count)]
            : 0;
    }

    private void UpdateDirection() {
        if (playerCamera == null || isDead) {
            ResetRotations();
            return;
        }

        Vector3 worldDir = playerCamera.position - m_head.position;
        localDirection = m_head.InverseTransformDirection(worldDir).normalized;

        angleToPlayer = Vector3.Angle(initialHeadForward, localDirection);
        targetOutOfView = angleToPlayer > maxRotationAngle;

        if (targetOutOfView) {
            ResetRotations();
            return;
        }
    }

    private void RotateHead() {
        if (playerCamera == null || targetOutOfView) return;

        Quaternion targetRotation =
            Quaternion.FromToRotation(initialHeadForward, localDirection) * initialHeadRotation;

        if (angleToPlayer > maxRotationAngle) {
            targetRotation = Quaternion.Slerp(
                initialHeadRotation,
                targetRotation,
                maxRotationAngle / angleToPlayer
            );
        }

        m_head.localRotation = Quaternion.Slerp(
            m_head.localRotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void RotateEyes() {
        if (playerCamera == null || targetOutOfView) return;

        for (int i = 0; i < m_eyes.Length; i++) {
            float eyeAngle = Vector3.Angle(initialEyeForwards[i], localDirection);

            Quaternion eyeTargetRotation =
                Quaternion.FromToRotation(initialEyeForwards[i], localDirection) * initialEyeRotations[i];

            if (eyeAngle > maxEyeRotationAngle) {
                eyeTargetRotation = Quaternion.Slerp(
                    initialEyeRotations[i],
                    eyeTargetRotation,
                    maxEyeRotationAngle / eyeAngle
                );
            }

            m_eyes[i].localRotation = Quaternion.Slerp(
                m_eyes[i].localRotation,
                eyeTargetRotation,
                rotationSpeed * 2.15f * Time.deltaTime
            );
        }
    }

    private void ResetRotations() {
        m_head.localRotation = Quaternion.Slerp(
            m_head.localRotation,
            initialHeadRotation,
            rotationSpeed * Time.deltaTime
        );

        for (int i = 0; i < m_eyes.Length; i++) {
            m_eyes[i].localRotation = Quaternion.Slerp(
                m_eyes[i].localRotation,
                initialEyeRotations[i],
                rotationSpeed * 2.15f * Time.deltaTime
            );
        }
    }
    #endregion

    private void OnDie(Vector3 dir, float impact) {
        foreach (var e in m_eyesClosed) {
            e.gameObject.SetActive(true);
        }

        isDead = true;
    }

    void LateUpdate() {
        UpdateDirection();

        if (isDead) return;

        RotateHead();
        RotateEyes();
    }
}

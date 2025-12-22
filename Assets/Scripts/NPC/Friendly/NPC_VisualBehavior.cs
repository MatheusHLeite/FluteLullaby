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

    private Coroutine playerCheckRoutine;

    private NetworkList<ulong> playersIds = new NetworkList<ulong>();
    private NetworkVariable<ulong> selectedPlayerId = new NetworkVariable<ulong>(
    0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn() {
        playersIds.OnListChanged += OnPlayersListChanged;
        selectedPlayerId.OnValueChanged += OnSelectedPlayerChanged;
    }

    public override void OnNetworkDespawn() { 
        playersIds.OnListChanged -= OnPlayersListChanged;
        selectedPlayerId.OnValueChanged -= OnSelectedPlayerChanged;
    }

    void Start() {
        initialRotation = m_head.rotation;
        initialForward = initialRotation * Vector3.forward;

        initialEyeRotations = new Quaternion[m_eyes.Length];
        initialEyeForwards = new Vector3[m_eyes.Length];

        healthHandler = GetComponentInChildren<Global_HealthHandler>();
        if (healthHandler != null)
            healthHandler.m_onDie.AddListener(OnDie);

        for (int i = 0; i < m_eyes.Length; i++) {
            initialEyeRotations[i] = m_eyes[i].rotation;
            initialEyeForwards[i] = initialEyeRotations[i] * Vector3.forward;
        }

        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = lookRange;
    }

    private void OnPlayersListChanged(NetworkListEvent<ulong> changeEvent) {
        if (!IsServer) return;

        if (playersIds.Count > 0 && playerCheckRoutine == null) 
            playerCheckRoutine = StartCoroutine(CheckCamera());
        if (playersIds.Count <= 0 && playerCheckRoutine != null) {
            StopCoroutine(playerCheckRoutine);
            playerCheckRoutine = null;
        }

        CheckPlayersList();
    }

    private void OnSelectedPlayerChanged(ulong previousId, ulong newId) {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(newId, out var netObj))
            playerCamera = netObj.GetComponent<Player_Manager>().GetPlayerCamera().transform;
        else
            playerCamera = null;
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out Player_Manager player))
            OnPlayerEnterTrigger(player);
    }

    private void OnTriggerExit(Collider other) {
        if (other.TryGetComponent(out Player_Manager player))
            OnPlayerExitTrigger(player);
    }

    private void OnPlayerEnterTrigger(Player_Manager player) {
        if (IsServer) {
            if (!playersIds.Contains(player.NetworkObjectId))
                playersIds.Add(player.NetworkObjectId);
            return;
        }

        AddPlayerServerRpc(player.NetworkObjectId);
    }

    private void OnPlayerExitTrigger(Player_Manager player) {
        if (IsServer) {
            if (playersIds.Contains(player.NetworkObjectId))
                playersIds.Remove(player.NetworkObjectId);
            return;
        }

        RemovePlayerServerRpc(player.NetworkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddPlayerServerRpc(ulong playerId) {
        if (!playersIds.Contains(playerId))
            playersIds.Add(playerId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemovePlayerServerRpc(ulong playerId) {
        if (playersIds.Contains(playerId))
            playersIds.Remove(playerId);
    }

    private IEnumerator CheckCamera() {
        while (playersIds.Count > 0) {
            int randomDelay = Random.Range(5, 10);
            yield return new WaitForSeconds(randomDelay);

            CheckPlayersList();
        }
    }

    private void CheckPlayersList() {
        if (playersIds.Count > 0)
            selectedPlayerId.Value = playersIds[Random.Range(0, playersIds.Count)];
        else
            selectedPlayerId.Value = 0;

        //if (playersIds.Count > 0) {
        //    var randomId = playersIds[Random.Range(0, playersIds.Count)];
        //    if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(randomId, out var netObj))            
        //        playerCamera = netObj.GetComponent<Player_Manager>().GetPlayerCamera().transform;            
        //}
        //else playerCamera = null;
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

    private void OnDie(Vector3 dir, float impact) {
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

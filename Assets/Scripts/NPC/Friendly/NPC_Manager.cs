using Unity.Netcode;
using UnityEngine;

public class NPC_Manager : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private float m_maxHealth;

    private Rigidbody rgbd;
    private Global_HealthHandler healthHandler;

    private void Awake() {
        rgbd = GetComponent<Rigidbody>();
        healthHandler = GetComponent<Global_HealthHandler>();
    }

    public override void OnNetworkSpawn() {
        if (!IsServer) 
            return;

        healthHandler.m_onDie.AddListener(OnDie);
        healthHandler.SetHealth(m_maxHealth);
    }

    public override void OnNetworkDespawn() {
        if (!IsServer)
            return;

        healthHandler.m_onDie.RemoveListener(OnDie);
    }

    private void OnDie(Vector3 hitPoint, Vector3 hitDirection, float impact) {
        rgbd.isKinematic = false;
        rgbd.AddForce(hitDirection * impact, ForceMode.Impulse);
    }
}

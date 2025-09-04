using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class NPC_Health : NetworkBehaviour {
    

    [SerializeField] private float m_maxHealth;
    [SerializeField] private UnityEvent m_onDie;

    private Rigidbody rgbd;
    private Dialogue_Interactor dialogue;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitPoint = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitDirection = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> impact = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake() {
        rgbd = GetComponent<Rigidbody>();
        dialogue = GetComponent<Dialogue_Interactor>();
    }

    #region Network Initialization
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            currentHealth.OnValueChanged += OnHealthChanged;

            SetHealth(m_maxHealth, m_maxHealth);
        }
    }

    public override void OnNetworkDespawn() {
        if (IsOwner) {
            currentHealth.OnValueChanged -= OnHealthChanged;
        }
    }

    private void SetHealth(float actualHealth, float maxHealth) {
        if (IsServer) {
            OnHealthSet(actualHealth);
            return;
        }

        SetHealthServerRpc(actualHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHealthServerRpc(float actualHealth) {
        OnHealthSet(actualHealth);
    }

    private void OnHealthSet(float actualHealth) {
        currentHealth.Value = actualHealth;
    }
    #endregion

    private void OnHealthChanged(float previousValue, float newValue) {
        if (newValue <= 0f && !isDead.Value) {
            Die(hitPoint.Value, hitDirection.Value, impact.Value);
        }
    }

    internal void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact) {
        if (currentHealth.Value <= 0) return;

        Singleton.Instance.GameEvents.OnHit?.Invoke();

        TakeDamageServerRpc(damage, hitPoint, hitDirection, impact);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, ServerRpcParams rpcParams = default) {
        HandleDamage(damage, hitPoint, hitDirection, impact, rpcParams.Receive.SenderClientId);
    }

    private void HandleDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, ulong killerClientId) {
        this.hitPoint.Value = hitPoint;
        this.hitDirection.Value = hitDirection;
        this.impact.Value = impact;
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0f && isDead.Value == false) {
            isDead.Value = true;

            var clientParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new[] { killerClientId }
                }
            };
            NotifyKillClientRpc(clientParams);
        }
    }

    [ClientRpc]
    private void NotifyKillClientRpc(ClientRpcParams clientRpcParams = default) {
        Singleton.Instance.GameEvents.OnKill?.Invoke();
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection, float impact) {
        rgbd.isKinematic = false;
        rgbd.AddForce(hitDirection * impact, ForceMode.Impulse);

        dialogue.StopImmediately();
        m_onDie?.Invoke();
    }
}

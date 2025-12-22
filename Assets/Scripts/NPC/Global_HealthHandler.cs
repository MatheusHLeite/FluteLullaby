using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Global_HealthHandler : NetworkBehaviour, IDamageable {
    [Header("Setup")]
    [SerializeField] private float m_maxHealth;
    public UnityEvent<Vector3, float> m_onDie;

    private bool isDead;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitPoint = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitDirection = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> impact = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnDestroy() {
        m_onDie.RemoveAllListeners();
    }

    #region Network Initialization
    public override void OnNetworkSpawn() {
        if (IsServer) {
            SetHealth(m_maxHealth, m_maxHealth);
        }
    }

    public override void OnNetworkDespawn() {
        
    }

    private void SetHealth(float actualHealth, float maxHealth) {
        if (IsServer) {
            OnHealthSet(actualHealth);
            return;
        }

        SetHealthServerRpc(actualHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHealthServerRpc(float actualHealth) => OnHealthSet(actualHealth);

    private void OnHealthSet(float actualHealth) => currentHealth.Value = actualHealth;
    #endregion

    #region Damage
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact) {
        if (currentHealth.Value <= 0) return;

        Singleton.Instance.GameEvents.OnHit?.Invoke();

        if (IsServer && NetworkManager.Singleton.LocalClientId == OwnerClientId)
            HandleDamage(damage, hitPoint, hitDirection, impact, OwnerClientId);
        else
            TakeDamageServerRpc(damage, hitPoint, hitDirection, impact);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, ServerRpcParams rpcParams = default) =>
        HandleDamage(damage, hitPoint, hitDirection, impact, rpcParams.Receive.SenderClientId);

    private void HandleDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, ulong killerClientId) {
        this.hitPoint.Value = hitPoint;
        this.hitDirection.Value = hitDirection;
        this.impact.Value = impact;
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0f && !isDead) {
            isDead = true;

            DieClientRpc(hitPoint, hitDirection, impact);

            var clientParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new[] { killerClientId }
                }
            };
            NotifyKillClientRpc(clientParams);
        }
    }

    [ClientRpc]
    private void NotifyKillClientRpc(ClientRpcParams clientRpcParams = default) => 
        Singleton.Instance.GameEvents.OnKill?.Invoke();

    [ClientRpc]
    private void DieClientRpc(Vector3 hitPoint, Vector3 hitDirection, float impact) => 
        Die(hitPoint, hitDirection, impact);
 
    private void Die(Vector3 hitPoint, Vector3 hitDirection, float impact) => m_onDie?.Invoke(hitDirection, impact);    
    #endregion;
}

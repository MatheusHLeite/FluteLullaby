using DelightStudio.AI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Global_HealthHandler : NetworkBehaviour, IDamageable {
    public UnityEvent<Vector3, Vector3, float, BodyPart> m_onDie;
    public UnityEvent m_onTargetKilled;
    public UnityEvent<DamageParameters> m_damageTaken;

    private bool isDead;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitPoint = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitDirection = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> impact = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Enemy_Combat enemyCombat;

    private void Awake() {
        enemyCombat = GetComponent<Enemy_Combat>();
    }

    public override void OnDestroy() {
        m_onDie.RemoveAllListeners();
        m_damageTaken.RemoveAllListeners();
    }

    #region Network Initialization
    public void SetHealth(float actualHealth) {
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
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part, NetworkObject networkObject) {
        if (IsServer)
            HandleDamage(damage, hitPoint, hitDirection, impact, part, NetworkManager.Singleton.LocalClientId);
        else
            TakeDamageServerRpc(damage, hitPoint, hitDirection, impact, part);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageServerRpc(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part, ServerRpcParams rpcParams = default) =>
        HandleDamage(damage, hitPoint, hitDirection, impact, part, rpcParams.Receive.SenderClientId);

    private void HandleDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part, ulong killerClientId) {
        if (enemyCombat != null && enemyCombat.IsStaggered)
            damage *= 2f;

        DamageParameters damageParameters = new DamageParameters {
            hitPosition = hitPoint,
            hitDirection = hitDirection,
            impact = impact,
            staggerAmount = impact,
            currentHp = currentHealth.Value,
            killerClientId = killerClientId,
            bodyPart = part
        };

        TakeDamageClientRpc(damageParameters);

        if (isDead || currentHealth.Value <= 0) 
            return;

        this.hitPoint.Value = hitPoint;
        this.hitDirection.Value = hitDirection;
        this.impact.Value = impact;
        currentHealth.Value -= damage;

        if (currentHealth.Value <= 0f) {
            isDead = true;

            var clientParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new[] { killerClientId }
                }
            };

            NotifyKillClientRpc(clientParams);
            DieClientRpc(hitPoint, hitDirection, impact, part);
        }
    }

    [ClientRpc]
    private void NotifyKillClientRpc(ClientRpcParams clientRpcParams = default) => m_onTargetKilled?.Invoke();

    [ClientRpc]
    private void DieClientRpc(Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part) => m_onDie?.Invoke(hitPoint, hitDirection, impact, part);

    [ClientRpc]
    private void TakeDamageClientRpc(DamageParameters damageParams) => m_damageTaken?.Invoke(damageParams);
    #endregion;
}

public struct DamageParameters : INetworkSerializable {
    public Vector3 hitPosition;
    public Vector3 hitDirection;
    public float impact;
    public float staggerAmount;
    public float currentHp;
    public ulong killerClientId;
    public BodyPart bodyPart;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref hitPosition);
        serializer.SerializeValue(ref hitDirection);
        serializer.SerializeValue(ref impact);
        serializer.SerializeValue(ref staggerAmount);
        serializer.SerializeValue(ref currentHp);
        serializer.SerializeValue(ref killerClientId);
        serializer.SerializeValue(ref bodyPart);
    }
}
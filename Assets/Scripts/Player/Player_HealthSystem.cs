using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class Player_HealthSystem : NetworkBehaviour {
    [Header("Setup")]
    [SerializeField] private int maxHealth; //[TODO] add to SO
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    [SerializeField] private float respawnDelay = 3f;

    private NetworkVariable<Vector3> hitPoint = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitDirection = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> impact = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsDead { get; private set; }

    private NetworkTransform NTransform;

    private void Awake() {
        NTransform = GetComponent<NetworkTransform>();
    }

    #region Network Initialization
    public override void OnNetworkSpawn() {
        if (IsOwner) {
            currentHealth.OnValueChanged += OnHealthChanged;

            Singleton.Instance.GameEvents.OnHealthSet.AddListener(SetHealth);
            Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnSpawn);

            OnSpawn();
        }
    }

    public override void OnNetworkDespawn() {
        if (IsOwner) {
            currentHealth.OnValueChanged -= OnHealthChanged;

            Singleton.Instance.GameEvents.OnHealthSet.RemoveListener(SetHealth);
            Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnSpawn);
        }
    }
    #endregion

    private void OnHealthChanged(float previousValue, float newValue) {
        if (newValue <= 0f && !IsDead) {
            Die(hitPoint.Value, hitDirection.Value, impact.Value);
        }

        Singleton.Instance.GameEvents.OnDamageTaken?.Invoke(newValue, maxHealth);
    }

    private void OnSpawn() {
        Singleton.Instance.GameEvents.OnHealthSet?.Invoke(maxHealth, maxHealth);        
    }

    private void SetHealth(int actualHealth, int maxHealth) {
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

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact) {
        if (currentHealth.Value <= 0) return;

        Singleton.Instance.GameEvents.OnHit?.Invoke();

        if (IsServer && NetworkManager.Singleton.LocalClientId == OwnerClientId)
            HandleDamage(damage, hitPoint, hitDirection, impact, OwnerClientId);   
        else 
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
        Singleton.Instance.GameEvents.OnPlayerDie?.Invoke(hitPoint, hitDirection, impact);
        IsDead = true;

        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine() {
        yield return new WaitForSeconds(respawnDelay);

        RequestTeleportServerRpc();

        Singleton.Instance.GameEvents.OnPlayerRespawn?.Invoke();
        IsDead = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestTeleportServerRpc() {
        Vector3 randomPos = Singleton.Instance.GameManager.GetRandomSpawnPos();
        Quaternion randomRot = Quaternion.identity;

        isDead.Value = false;

        TeleportClientRpc(randomPos, randomRot, Vector3.one, new ClientRpcParams {
            Send = new ClientRpcSendParams {
                TargetClientIds = new ulong[] { NetworkObject.OwnerClientId }
            }
        });
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 pos, Quaternion rot, Vector3 scale, ClientRpcParams clientParams = default) {
        NTransform.Teleport(pos, rot, scale);
    }
}
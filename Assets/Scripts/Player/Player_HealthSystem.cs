using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using static Steamworks.InventoryItem;

public class Player_HealthSystem : NetworkBehaviour, IDamageable {
    [Header("Setup")]
    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<Vector3> hitPoint = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitDirection = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> impact = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsDead { get; private set; }

    private Player_MovementSystem movementSystem;
    private Player_CombatSystem combatSystem;
    private NetworkTransform NTransform;

    private float maxHealth;
    private bool isBlocking;

    private void Awake() {
        movementSystem = GetComponent<Player_MovementSystem>();
        combatSystem = GetComponent<Player_CombatSystem>();
        NTransform = GetComponent<NetworkTransform>();
    }

    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        currentHealth.OnValueChanged += OnHealthChanged;

        //Singleton.Instance.GameEvents.OnPlayerRespawn.AddListener(OnSpawn);
    }

    public void DeinitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        currentHealth.OnValueChanged -= OnHealthChanged;

        //Singleton.Instance.GameEvents.OnPlayerRespawn.RemoveListener(OnSpawn);
    }
    #endregion

    internal void SetPlayerParameters(PlayerParameters_SO playerParameters) {
        maxHealth = playerParameters.m_maxHealth;

        SetHealth(maxHealth);
    }

    private void OnHealthChanged(float previousValue, float newValue) {
        if (newValue <= 0f && !IsDead) 
            Die(hitPoint.Value, hitDirection.Value, impact.Value);        

        Singleton.Instance.GameEvents.OnDamageTaken?.Invoke(newValue, maxHealth, isBlocking);
    }

    private void SetHealth(float maxHealth) {
        Singleton.Instance.GameEvents.OnHealthSet?.Invoke(maxHealth, maxHealth);

        if (IsServer) {
            OnHealthSet(maxHealth);
            return;
        }

        SetHealthServerRpc(maxHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHealthServerRpc(float maxHealth) => OnHealthSet(maxHealth);

    private void OnHealthSet(float maxHealth) => currentHealth.Value = maxHealth;

    public bool CanDrinkFlask() => currentHealth.Value < maxHealth;

    public void Heal(float amount) {
        if (IsServer && NetworkManager.Singleton.LocalClientId == OwnerClientId)
            HandleHeal(amount);
        else
            HandleHealRpc(amount);
    }

    private void HandleHeal(float amount) {
        currentHealth.Value += amount;
        currentHealth.Value = Mathf.Clamp(currentHealth.Value, 0, maxHealth);
    }

    [Rpc(SendTo.Server)]
    private void HandleHealRpc(float amount) 
        => HandleHeal(amount);
    
    private bool IsLookingAtEnemy() {
        //[TODO] adicionar check se estiver olhando para o inimigo
        return true;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part, NetworkObject attacker) {
        if (currentHealth.Value <= 0) 
            return;

        bool isExtremelyClose = IsLookingAtEnemy();
        isBlocking = combatSystem.IsBlocking_NV.Value;
        bool isParrying = combatSystem.IsParryActive_NV.Value;

        if (isExtremelyClose) {
            if (isParrying) {
                combatSystem.PerformParry(attacker, hitPoint);
                return;
            }

            if (isBlocking) {
                if (movementSystem.ConsumeStamina(2.5f, () => {
                    combatSystem.BreakDefense(hitPoint);
                })) {
                    damage *= 0.3f;
                }
            }
        }

        if (IsServer && NetworkManager.Singleton.LocalClientId == OwnerClientId)
            HandleDamage(damage, hitPoint, hitDirection, impact, OwnerClientId);   
        else
            TakeDamageRpc(damage, hitPoint, hitDirection, impact);        
    }

    [Rpc(SendTo.Server)]
    private void TakeDamageRpc(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, RpcParams rpcParams = default) 
        => HandleDamage(damage, hitPoint, hitDirection, impact, rpcParams.Receive.SenderClientId);

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

        //StartCoroutine(RespawnCoroutine());
    }

    /*private IEnumerator RespawnCoroutine() {
        yield return new WaitForSeconds(respawnDelay);

        RequestTeleportServerRpc();

        Singleton.Instance.GameEvents.OnPlayerRespawn?.Invoke();
        IsDead = false;
    }*/

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
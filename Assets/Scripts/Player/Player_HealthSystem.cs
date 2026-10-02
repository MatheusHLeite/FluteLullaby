using Unity.Netcode;
using UnityEngine;

public class Player_HealthSystem : NetworkBehaviour, IDamageable {
    [Header("Setup")]
    [SerializeField] [Range(1, 100)] private float m_reviveHealthPercentageAmount = 40;
    [SerializeField] private float m_staminaConsumptionWhenBlocking = 2.35f;

    private NetworkVariable<float> currentHealth = new NetworkVariable<float>(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<Vector3> hitPoint = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector3> hitDirection = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> impact = new NetworkVariable<float>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> isDead = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsDead => isDead.Value;

    private Player_Manager manager;
    private Player_AnimationSystem animationSystem;
    private Player_MovementSystem movementSystem;
    private Player_CombatSystem combatSystem;

    private float maxHealth;
    private bool isBlocking;

    #region Initialization
    private void Awake() {
        manager = GetComponent<Player_Manager>();
        animationSystem = GetComponent<Player_AnimationSystem>();
        movementSystem = GetComponent<Player_MovementSystem>();
        combatSystem = GetComponent<Player_CombatSystem>();
    }

    internal void SetPlayerParameters(PlayerParameters_SO playerParameters) {
        maxHealth = playerParameters.m_maxHealth;
        SetHealth(maxHealth, maxHealth);
    }
    #endregion

    #region Network Initialization
    public void InitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        currentHealth.OnValueChanged += OnHealthChanged;
    }

    public void DeinitializeNetwork(bool isOwner) {
        if (!isOwner) return;

        currentHealth.OnValueChanged -= OnHealthChanged;
    }
    #endregion

    #region Events
    private void OnHealthChanged(float previousValue, float newValue) {
        if (newValue <= 0f && !IsDead) 
            Die(hitPoint.Value, hitDirection.Value, impact.Value);
    }
    #endregion

    #region Health set
    private void SetHealth(float currentHealth, float maxHealth) {
        Singleton.Instance.GameEvents.OnHealthSet?.Invoke(currentHealth, maxHealth);

        if (IsServer) {
            OnHealthSet(currentHealth);
            return;
        }

        SetHealthServerRpc(currentHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHealthServerRpc(float maxHealth) => OnHealthSet(maxHealth);

    private void OnHealthSet(float maxHealth) => currentHealth.Value = maxHealth;
    #endregion

    #region Helpers
    public bool CanDrinkFlask() => currentHealth.Value < maxHealth;

    private float GetHealthPercentage(float percentage) {
        float percentageCalculated = percentage / 100;
        return maxHealth * percentageCalculated;
    }

    private bool IsLookingAtEnemy() {
        //[TODO] adicionar check se estiver olhando para o inimigo
        return true;
    }
    #endregion

    #region Healing
    public void Heal(float amount) {
        float healthAfterHeal = currentHealth.Value + amount;

        if (healthAfterHeal > 10)
            Singleton.Instance.GameEvents.OnCriticalIndicatorShow?.Invoke(CriticalIndicator.Health, false);

        Singleton.Instance.GameEvents.OnDamageTaken?.Invoke(healthAfterHeal, maxHealth, false);

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
    #endregion

    #region Damage
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part, NetworkObject attacker) {
        if (currentHealth.Value <= 0 || combatSystem.IsInvulnerable) 
            return;
        
        bool isOnRange = IsLookingAtEnemy();
        bool successfullyBlocked = false;
    
        isBlocking = combatSystem.IsBlocking;

        if (isOnRange) {
            if (combatSystem.ParryWindowOpened) {
                combatSystem.PerformParry(attacker, hitPoint);
                return;
            }

            if (isBlocking) {
                if (movementSystem.ConsumeStamina(m_staminaConsumptionWhenBlocking, true, () => {
                    combatSystem.BreakDefense(hitPoint);
                })) {
                    damage *= 0.3f;
                    successfullyBlocked = true;
                }
            }
        }

        float healthAfterDamage = currentHealth.Value - damage;

        Singleton.Instance.GameEvents.OnDamageTaken?.Invoke(healthAfterDamage, maxHealth, isBlocking);

        if (healthAfterDamage <= 10)
            Singleton.Instance.GameEvents.OnCriticalIndicatorShow?.Invoke(CriticalIndicator.Health, true);

        animationSystem.OnDamageTaken(successfullyBlocked);

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

        if (currentHealth.Value <= 0f && !IsDead) {
            isDead.Value = true;

            var clientParams = new ClientRpcParams {
                Send = new ClientRpcSendParams {
                    TargetClientIds = new[] { killerClientId }
                }
            };
            NotifyKillClientRpc(clientParams);
        }
    }
    #endregion

    #region Death
    [ClientRpc]
    private void NotifyKillClientRpc(ClientRpcParams rpcParams = default) {
        Singleton.Instance.GameEvents.OnKill?.Invoke();
    }

    private void Die(Vector3 hitPoint, Vector3 hitDirection, float impact) {
        Singleton.Instance.GameEvents.OnPlayerDie?.Invoke(hitPoint, hitDirection, impact);
    }
    #endregion
    
    public void RevivePlayer() {
        float health = GetHealthPercentage(m_reviveHealthPercentageAmount);
        float revivedHealthAmount = Mathf.Clamp(health, 0, maxHealth);

        Singleton.Instance.GameEvents.OnPlayerRespawn?.Invoke();

        manager.OnPlayerRevived();
        SetHealth(revivedHealthAmount, maxHealth);
        RevivePlayerRpc();
    }

    [Rpc(SendTo.Server)]
    private void RevivePlayerRpc() {
        isDead.Value = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            RevivePlayer();
    }
}
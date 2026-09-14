using DelightStudio.Data;
using Unity.Netcode;
using UnityEngine;

namespace DelightStudio.AI {
    [RequireComponent(typeof(Global_HealthHandler))]
    public class Enemy_Manager : NetworkBehaviour {
        [Header("Setup")]
        [SerializeField] private Enemy_SO m_enemy;

        private Global_HealthHandler health;
        private Enemy_FOV fov;
        private Enemy_Movement movement;
        private Enemy_VisualHandler visualHandler;
        private Enemy_Ragdoll ragdoll;
        private Enemy_Combat combat;
        private Enemy_Animator animator;
        private ProceduralHitReaction hitReactionSystem;

        public bool IsDead { get; private set; }

        #region Initialization
        private void Awake() {
            health = GetComponent<Global_HealthHandler>();
            fov = GetComponent<Enemy_FOV>();
            movement = GetComponent<Enemy_Movement>();
            visualHandler = GetComponent<Enemy_VisualHandler>();
            ragdoll = GetComponent<Enemy_Ragdoll>();
            combat = GetComponent<Enemy_Combat>();
            hitReactionSystem = GetComponent<ProceduralHitReaction>();
        }
        #endregion

        #region Network initialization
        public override void OnNetworkSpawn() {
            IsDead = false;

            movement.Initialize();
            combat.Initialize(m_enemy);

            if (!IsServer)
                return;

            health.SetHealth(m_enemy.m_maxHealth);
            movement.ChangeState(EnemyState.Idle);

            health.m_onDie.AddListener(OnDie);
            health.m_damageTaken.AddListener(OnDamageTaken);
            health.m_onTargetKilled.AddListener(OnTargetKilled);
        }

        public override void OnNetworkDespawn() {
            if (!IsServer)
                return;

            health.m_onDie.RemoveListener(OnDie);
            health.m_damageTaken.RemoveListener(OnDamageTaken);
            health.m_onTargetKilled.RemoveListener(OnTargetKilled);
        }
        #endregion

        #region Events
        private void OnDie(Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart bodyPart) {
            IsDead = true;
   
            ragdoll.OnAIDied(hitPoint, hitDirection, impact, bodyPart);
            combat.OnDied();

            if (bodyPart == BodyPart.Head)
                visualHandler.OnCriticalDeath();
        }

        private void OnDamageTaken(DamageParameters damageParameters) {
            Transform hitBone = ragdoll.GetClosestBonePrecisely(damageParameters.hitPosition);
            if (hitBone != null)
                hitReactionSystem.PlayHitReaction(hitBone, damageParameters.hitDirection, damageParameters.staggerAmount);

            if (IsDead && damageParameters.bodyPart == BodyPart.Head)
                visualHandler.OnCriticalDeath(true);

            if (damageParameters.currentHp <= 0)
                return;

            Singleton.Instance.GameEvents.OnUpdateEnemyFound?.Invoke(m_enemy);

            visualHandler.OnHit(damageParameters.hitPosition, damageParameters.hitDirection);
            combat.ApplyStaggerAmount(damageParameters.staggerAmount);            

            if (!IsServer) 
                return;

            if (damageParameters.bodyPart == BodyPart.Leg && !ragdoll.IsRagdoll && damageParameters.impact > m_enemy.m_legResistanceAmount) {
                float chance = m_enemy.m_legShotReactionChance / 100f;
                float weaponPowerMultiplier = damageParameters.staggerAmount / 60;
                float multiplier = Mathf.Clamp(weaponPowerMultiplier, 1, 2.5f);

                float randomResult = Random.value;                

                float chanceCalculated = chance * multiplier;

                if (randomResult <= chanceCalculated) 
                    ragdoll.OnLegHit(damageParameters.hitPosition, damageParameters.hitDirection, damageParameters.impact);                
            }
            
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(damageParameters.killerClientId, out NetworkClient attackerClient))
                return;

            NetworkObject attackerObj = attackerClient.PlayerObject;

            if (attackerObj == null) 
                return;

            Transform attackerTransform = attackerObj.transform;
            movement.ReactToDamage(attackerTransform);
        }

        private void OnTargetKilled() {
            Singleton.Instance.GameEvents.OnEnemyKilled?.Invoke(m_enemy);
        }
        #endregion

        private void Update() {
            if (!IsServer) 
                return;
            
            if (IsDead || ragdoll.IsRagdoll)
                return;

            fov.Tick();
            movement.Tick();
            combat.Tick();
        }
    }
}
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
        private void OnDie(Vector3 hitPoint, Vector3 hitDirection, float impact) {
            IsDead = true;
   
            ragdoll.OnAIDied(hitPoint, hitDirection, impact);
            combat.OnDied();
        }

        private void OnDamageTaken(Vector3 hitPosition, Vector3 hitDirection, float staggerAmount, float currentHp) {
            Singleton.Instance.GameEvents.OnUpdateEnemyFound?.Invoke(m_enemy);

            if (currentHp <= 0) 
                return;

            Transform hitBone = ragdoll.GetClosestBonePrecisely(hitPosition);

            if (hitBone != null)
                hitReactionSystem.PlayHitReaction(hitBone, hitDirection, staggerAmount);

            visualHandler.PlayFlash();
            combat.ApplyStaggerAmount(staggerAmount);
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
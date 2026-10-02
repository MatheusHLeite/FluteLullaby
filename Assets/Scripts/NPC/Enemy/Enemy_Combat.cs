using DelightStudio.Data;
using Unity.Netcode;
using UnityEngine;

namespace DelightStudio.AI {
    public class Enemy_Combat : MonoBehaviour {
        [SerializeField] private DamageSource m_hitBox;
        [SerializeField] private GameObject m_stunEffect;

        private bool isDead;
        private float currentStaggerAmount;
        private float maxStaggerAmount;

        private float staggerTime;
        private float staggerMaxTime;

        public bool IsStaggered { get; private set; }

        private Enemy_Movement movement;
        private Enemy_Animator animator;
        private Enemy_MotionSync motionSync;

        public void Initialize(Enemy_SO enemy) {
            movement = GetComponent<Enemy_Movement>();
            animator = GetComponent<Enemy_Animator>();
            motionSync = GetComponent<Enemy_MotionSync>();

            DisableHitBox();

            float impact = enemy.m_attackDamage * 1.425f;
            maxStaggerAmount = enemy.m_maxStaggerAmount;
            staggerMaxTime = enemy.m_maxStaggerTime;

            m_hitBox.Setup(enemy.m_attackDamage, impact, GetComponent<NetworkObject>());
            m_stunEffect.SetActive(false);
        }

        public void DisableHitBox() {
            m_hitBox.SetHitBoxState(false);
            motionSync.EndAttack();
        }

        public void EnableHitBox() {
            if (isDead)
                return;

            m_hitBox.SetHitBoxState(true);            
        }

        public void OnDied() {
            isDead = true;
            m_hitBox.SetHitBoxState(false);

            m_stunEffect.SetActive(false);
        }

        internal void ApplyStaggerAmount(float staggerAmount) {
            if (IsStaggered) 
                return;

            currentStaggerAmount += staggerAmount;
        }

        private void ApplyStagger() {
            DisableHitBox();

            currentStaggerAmount = 0;

            staggerTime = staggerMaxTime;
            IsStaggered = true;
            
            movement.ChangeState(EnemyState.Staggered);
            animator.PlayStaggerAnimation();

            m_stunEffect.SetActive(true);
        }

        private void RemoveStagger() {
            staggerTime = 0;
            IsStaggered = false;

            m_stunEffect.SetActive(false);

            movement.ChangeState(EnemyState.Chasing);
            animator.ResetStagger();
        }

        private void HandleStaggerAmount() {
            if (currentStaggerAmount <= 0)
                return;

            currentStaggerAmount -= Time.deltaTime;

            if (currentStaggerAmount >= maxStaggerAmount)            
                ApplyStagger();
        }

        private void HandleStagger() {
            if (!IsStaggered)
                return;

            staggerTime -= Time.deltaTime;
            if (staggerTime <= 0)
                RemoveStagger();
        }

        public void Tick() {
            HandleStaggerAmount();
            HandleStagger();
        }
    }
}
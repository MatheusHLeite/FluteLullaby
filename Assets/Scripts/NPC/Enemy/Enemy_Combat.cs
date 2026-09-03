using DelightStudio.Data;
using UnityEngine;

namespace DelightStudio.AI {
    public class Enemy_Combat : MonoBehaviour {
        [SerializeField] private DamageSource m_hitBox;

        private bool isDead;
        private float currentStaggerAmount;
        private float maxStaggerAmount;

        private float staggerTime;
        private float staggerMaxTime;

        private bool isStaggered;

        private Enemy_Movement movement;
        private Enemy_Animator animator;

        public void Initialize(Enemy_SO enemy) {
            movement = GetComponent<Enemy_Movement>();
            animator = GetComponent<Enemy_Animator>();

            DisableHitBox();

            float impact = enemy.m_attackDamage * 1.425f;
            maxStaggerAmount = enemy.m_maxStaggerAmount;
            staggerMaxTime = enemy.m_maxStaggerTime;

            m_hitBox.Setup(enemy.m_attackDamage, impact);            
        }

        public void DisableHitBox() {
            m_hitBox.SetHitBoxState(false);
        }

        public void EnableHitBox() {
            if (isDead)
                return;

            m_hitBox.SetHitBoxState(true);
        }

        public void OnDied() {
            isDead = true;
            m_hitBox.SetHitBoxState(false);
        }

        internal void ApplyStaggerAmount(float staggerAmount) {
            if (isStaggered) 
                return;

            currentStaggerAmount += staggerAmount;
        }

        private void ApplyStagger() {
            currentStaggerAmount = 0;

            staggerTime = staggerMaxTime;
            isStaggered = true;
            
            movement.ChangeState(EnemyState.Staggered);
            animator.PlayStaggerAnimation();
        }

        private void RemoveStagger() {
            staggerTime = 0;
            isStaggered = false;
            
            movement.ChangeState(EnemyState.Chasing);
            animator.ResetStagger();
        }

        private void HandleStaggerAmount() {
            if (currentStaggerAmount <= 0)
                return;

            currentStaggerAmount -= Time.deltaTime;

            if (currentStaggerAmount >= maxStaggerAmount)            
                ApplyStagger();

            print($"{currentStaggerAmount}/{maxStaggerAmount}");
        }

        private void HandleStagger() {
            if (!isStaggered)
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
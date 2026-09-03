using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace DelightStudio.AI {
    public class Enemy_Movement : NetworkBehaviour {
        [Header("Setup")]
        [SerializeField] private float rotationSpeed = 16f;

        [Header("Wandering")]        
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float idleTime = 2f;

        [Header("Combat")]
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float attackCooldown = 1.5f;

        private EnemyState currentState;
        private Enemy_FOV fov;
        private Enemy_Animator animator;
        private NavMeshAgent agent;

        private float idleTimer;
        private float attackTimer;
        private float walkSpeed;

        private float currentSpeed;

        public event UnityAction<EnemyState> OnStateChanged;

        public void Initialize() {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Enemy_Animator>();

            if (!IsServer) {
                agent.enabled = false;
                return;
            }

            fov = GetComponent<Enemy_FOV>();
        }

        public void ChangeState(EnemyState newState) {
            currentState = newState;
            idleTimer = 0f;

            walkSpeed = newState == EnemyState.Chasing ? 1f : 2f;

            OnStateChanged?.Invoke(newState);

            if (!agent.isOnNavMesh) 
                return;

            switch (newState) {
                case EnemyState.Staggered:
                case EnemyState.Idle:
                    agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                    agent.updateRotation = false;
                    break;
                case EnemyState.Wandering:
                case EnemyState.Chasing:
                    agent.isStopped = false;
                    agent.updateRotation = true;
                    break;
                case EnemyState.Attacking:
                    agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;
                    agent.updateRotation = false;
                    break;
            }       
        }

        private void UpdateIdle() {
            idleTimer += Time.deltaTime;

            if (fov.CurrentTarget != null) {
                ChangeState(EnemyState.Chasing);
                return;
            }

            if (idleTimer >= idleTime)
                ChangeState(EnemyState.Wandering);
        }

        private void UpdateWandering() {
            if (fov.CurrentTarget != null) {
                ChangeState(EnemyState.Chasing);
                return;
            }

            if (!agent.hasPath && !agent.pathPending) {
                Vector3 point = RandomNavSphere(transform.position, wanderRadius);

                if (point != transform.position) {
                    agent.isStopped = false;
                    agent.SetDestination(point);
                }
                else {
                    ChangeState(EnemyState.Idle);
                    return;
                }
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)            
                ChangeState(EnemyState.Idle);            
        }

        private void UpdateChasing() {
            if (fov.CurrentTarget == null) {
                ChangeState(EnemyState.Searching);
                agent.isStopped = false;
                agent.SetDestination(fov.LastSeenPosition);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, fov.CurrentTarget.position);

            if (distanceToTarget <= attackRange) {
                if (attackTimer <= 0f)                
                    ChangeState(EnemyState.Attacking);                
                else {
                    agent.isStopped = true;
                    agent.ResetPath();
                    agent.velocity = Vector3.zero;

                    FaceTarget(fov.CurrentTarget.position);
                }
                return;
            }

            agent.isStopped = false;
            agent.SetDestination(fov.CurrentTarget.position);
        }

        private void UpdateSearching() {
            if (fov.CurrentTarget != null) {
                ChangeState(EnemyState.Chasing);
                return;
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                ChangeState(EnemyState.Idle);
        }

        private void UpdateAttacking() {
            if (fov.CurrentTarget != null)            
                FaceTarget(fov.CurrentTarget.position);

            if (attackTimer > 0) 
                return;

            animator.PlayAttackAnimation(() => {
                ChangeState(EnemyState.Chasing);
            });
            attackTimer = attackCooldown;            
        }

        private void UpdateAnimatorSpeed() {
            if (!agent.enabled || !agent.isOnNavMesh) 
                return;

            float targetSpeed = 0f;

            if (!agent.isStopped && (agent.hasPath || agent.pathPending)) {
                float rawSpeed = agent.desiredVelocity.magnitude;
                targetSpeed = rawSpeed / walkSpeed;
            }

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 8f);
            animator.UpdateAnimatorSpeed(currentSpeed);
        }

        private void FaceTarget(Vector3 targetPosition) {
            Vector3 direction = (targetPosition - transform.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero) {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 18f);
            }
        }

        private Vector3 RandomNavSphere(Vector3 origin, float dist) {
            for (int i = 0; i < 10; i++) {
                Vector3 randDirection = Random.insideUnitSphere * dist;
                randDirection += origin;

                NavMeshHit navHit;
                if (NavMesh.SamplePosition(randDirection, out navHit, dist, NavMesh.AllAreas)) {
                    NavMeshPath path = new NavMeshPath();
                    if (agent.CalculatePath(navHit.position, path) && path.status == NavMeshPathStatus.PathComplete) 
                        return navHit.position;                    
                }
            }

            return origin;
        }

        public void Tick() {
            if (attackTimer > 0) 
                attackTimer -= Time.deltaTime;

            UpdateAnimatorSpeed();

            switch (currentState) {
                case EnemyState.Idle: 
                    UpdateIdle(); break;
                case EnemyState.Wandering:
                    UpdateWandering(); break;
                case EnemyState.Chasing:
                    UpdateChasing(); break;
                case EnemyState.Searching:
                    UpdateSearching();  break;
                case EnemyState.Attacking: 
                    UpdateAttacking(); break;
            }
        }
    }
}
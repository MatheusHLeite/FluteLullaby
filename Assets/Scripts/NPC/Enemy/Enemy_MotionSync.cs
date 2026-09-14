using UnityEngine;
using UnityEngine.AI;

namespace DelightStudio.AI {
    public class Enemy_MotionSync : MonoBehaviour {
        [SerializeField] private float m_minAttackDistance = 1.3f;

        private Animator _animator;
        private NavMeshAgent _agent;

        private bool _isAttacking;
        private Transform _targetPlayer;

        private void Awake() {
            _animator = GetComponent<Animator>();
            _agent = GetComponent<NavMeshAgent>();

            _agent.updatePosition = false;
            _agent.updateRotation = true;
        }

        public void StartAttack(Transform target) {
            _isAttacking = true;
            _targetPlayer = target;
        }

        public void EndAttack() {
            _isAttacking = false;
            _targetPlayer = null;
        }

        private void OnAnimatorMove() {
            if (!_agent.enabled) 
                return;

            Vector3 deltaPosition = _animator.deltaPosition;

            if (_isAttacking && _targetPlayer != null) {
                float distance = Vector3.Distance(transform.position, _targetPlayer.position);

                if (distance <= m_minAttackDistance) {
                    deltaPosition.x = 0f;
                    deltaPosition.z = 0f;
                }
            }

            Vector3 newPosition = transform.position + deltaPosition;
            newPosition.y = _agent.nextPosition.y;

            transform.position = newPosition;
            _agent.nextPosition = transform.position;
        }
    }
}
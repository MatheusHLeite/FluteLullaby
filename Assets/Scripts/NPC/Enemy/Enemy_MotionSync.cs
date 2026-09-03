using UnityEngine;
using UnityEngine.AI;

namespace DelightStudio.AI {
    public class Enemy_MotionSync : MonoBehaviour {
        private Animator animator;
        private NavMeshAgent agent;

        private void Awake() {
            animator = GetComponent<Animator>();
            agent = GetComponent<NavMeshAgent>();

            agent.updatePosition = false;
            agent.updateRotation = true;
        }

        private void OnAnimatorMove() {
            if (animator == null || agent == null || !agent.enabled) 
                return;

            Vector3 position = animator.rootPosition;
            position.y = agent.nextPosition.y;

            transform.position = position;
            agent.nextPosition = transform.position;
        }
    }
}
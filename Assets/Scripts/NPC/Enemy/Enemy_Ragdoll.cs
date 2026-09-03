using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace DelightStudio.AI {
    public class Enemy_Ragdoll : MonoBehaviour {
        private Rigidbody[] ragdollRigidbodies;
        private Collider[] ragdollColliders;
        private NavMeshAgent agent;
        private Animator animator;

        public bool IsRagdoll { get; private set; }

        private void Awake() {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            ragdollColliders = GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();

            ToggleRagdoll(false);
        }

        public void ToggleRagdoll(bool state) {
            animator.enabled = !state;

            agent.isStopped = state;
            agent.enabled = !state;

            foreach (var rb in ragdollRigidbodies)            
                rb.isKinematic = !state;

            if (state) {
                Vector3 cachedSpeed = agent.velocity;

                foreach (var rb in ragdollRigidbodies)
                    rb.linearVelocity = cachedSpeed;
            }

            IsRagdoll = state;
        }

        public void OnAIDied(Vector3 hitPoint, Vector3 hitDirection, float impact) {
            ToggleRagdoll(true);

            Rigidbody closestBone = GetClosestBone(hitPoint);

            if (closestBone != null)            
                closestBone.AddForceAtPosition(hitDirection.normalized * impact, hitPoint, ForceMode.Impulse);            
        }

        public Rigidbody GetClosestBone(Vector3 point) {
            Rigidbody closest = null;
            float minDistance = float.MaxValue;

            foreach (var rb in ragdollRigidbodies) {
                float dist = Vector3.Distance(rb.transform.position, point);
                if (dist < minDistance) {
                    minDistance = dist;
                    closest = rb;
                }
            }

            return closest;
        }

        public Transform GetClosestBonePrecisely(Vector3 hitPosition) {
            Transform closestBone = null;
            float closestDistance = Mathf.Infinity;

            foreach (Collider col in ragdollColliders) {
                if (col == null || !col.enabled) continue;

                Vector3 closestPointOnBounds = col.ClosestPoint(hitPosition);
                float distance = Vector3.Distance(hitPosition, closestPointOnBounds);

                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestBone = col.transform;
                }
            }

            return closestBone;
        }
    }
}
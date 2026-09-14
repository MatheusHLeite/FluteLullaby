using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DelightStudio.AI {
    public class Enemy_FOV : MonoBehaviour {
        [Header("FOV Settings")]
        [SerializeField] private float m_viewRadius = 10f;
        [Range(0, 360)][SerializeField] private float m_viewAngle = 90f;

        [SerializeField] private LayerMask targetMask;
        [SerializeField] private LayerMask obstacleMask;

        [SerializeField] private Transform eyePoint;

        private bool isTargetAlerted;
        private float timeToForgetTarget = 10f;

        public Transform CurrentTarget { get; private set; }
        public Vector3 LastSeenPosition { get; private set; }

        public event UnityAction<ulong> OnFOVEntered;
        public event UnityAction OnFOVExit;

        private Coroutine alertRoutine;

        private void FindTargets() {
            if (isTargetAlerted) 
                return;

            if (CurrentTarget != null) {
                Vector3 dirToCurrent = (CurrentTarget.position - eyePoint.position).normalized;
                float distToCurrent = Vector3.Distance(eyePoint.position, CurrentTarget.position);

                if (distToCurrent <= m_viewRadius * 2.5f && !Physics.Raycast(eyePoint.position, dirToCurrent, distToCurrent, obstacleMask)) {
                    LastSeenPosition = CurrentTarget.position;
                    return;
                }
                else                
                    CurrentTarget = null;           
            }

            Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, m_viewRadius, targetMask);

            foreach (Collider target in targetsInViewRadius) {
                Vector3 dirToTarget = (target.transform.position - eyePoint.position).normalized;

                if (Vector3.Angle(eyePoint.forward, dirToTarget) < m_viewAngle / 2) {
                    float distToTarget = Vector3.Distance(eyePoint.position, target.transform.position);

                    if (!Physics.Raycast(eyePoint.position, dirToTarget, distToTarget, obstacleMask)) {
                        CurrentTarget = target.transform;
                        LastSeenPosition = target.transform.position;
                        return;
                    }
                }
            }
        }

        public void AlertToTarget(Transform attacker) {
            if (isTargetAlerted)
                return;
            isTargetAlerted = true;

            CurrentTarget = attacker;
            LastSeenPosition = attacker.position;

            if (alertRoutine != null)
                StopCoroutine(alertRoutine);
            alertRoutine = StartCoroutine(ResetAlertRoutine());
        }

        private IEnumerator ResetAlertRoutine() {
            yield return new WaitForSeconds(timeToForgetTarget);
            isTargetAlerted = false;
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, m_viewRadius);

            Vector3 left = Quaternion.Euler(0, -m_viewAngle / 2, 0) * eyePoint.forward;
            Vector3 right = Quaternion.Euler(0, m_viewAngle / 2, 0) * eyePoint.forward;

            Gizmos.color = CurrentTarget == null ? Color.green : Color.red;
            Gizmos.DrawRay(eyePoint.position, left * m_viewRadius);
            Gizmos.DrawRay(eyePoint.position, right * m_viewRadius);
        }

        public void Tick() => FindTargets();
    }
}
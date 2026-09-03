using UnityEngine;

namespace DelightStudio.AI {
    [RequireComponent(typeof(Animator))]
    public class Enemy_FootIK : MonoBehaviour {
        [Header("IK Settings")]
        [SerializeField] private bool enableIK = true;
        [SerializeField, Range(0f, 1f)] private float ikWeight = 1.0f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Offsets")]
        [SerializeField] private float raycastDistance = 1.0f;
        [SerializeField] private float footOffset = 0.1f;

        private Animator animator;

        private void Awake(){
            animator = GetComponent<Animator>();
        }

        private void OnAnimatorIK(int layerIndex) {
            if (animator == null || !enableIK) return;

            AdjustFoot(AvatarIKGoal.LeftFoot);
            AdjustFoot(AvatarIKGoal.RightFoot);
        }

        private void AdjustFoot(AvatarIKGoal foot) {
            animator.SetIKPositionWeight(foot, ikWeight);
            animator.SetIKRotationWeight(foot, ikWeight);

            Vector3 footPosition = animator.GetIKPosition(foot);

            Ray ray = new Ray(footPosition + Vector3.up * 0.5f, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance + 0.5f, groundLayer)) {
                Vector3 targetFootPosition = hit.point;
                targetFootPosition.y += footOffset;

                animator.SetIKPosition(foot, targetFootPosition);

                Quaternion footRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * transform.rotation;
                animator.SetIKRotation(foot, footRotation);
            }
        }
    }
}
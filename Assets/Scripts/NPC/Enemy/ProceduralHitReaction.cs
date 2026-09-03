using UnityEngine;

namespace DelightStudio.AI {
    public class ProceduralHitReaction : MonoBehaviour {
        [Header("Impact settings")]
        [SerializeField] private float reactionSpeed = 15f;
        [SerializeField] private float recoverySpeed = 10f;
        [SerializeField] private float intensityMultiplier = .2f;
        [SerializeField] private float maxBendAngle = 45f;
        [SerializeField] [Range(0.1f, 1f)] private float propagationDecay = 0.5f;

        [Header("Bones")]
        [SerializeField] private Transform rigRootBone;

        private Transform hitBone;
        private Vector3 hitDirection;
        private float currentIntensity;
        private float targetIntensity;

        public void PlayHitReaction(Transform bone, Vector3 dir, float staggerAmount) {
            hitBone = bone;
            hitDirection = dir.normalized;

            targetIntensity = staggerAmount * intensityMultiplier; 
        }

        private void LateUpdate() {
            if (hitBone == null) 
                return;

            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * reactionSpeed);
            targetIntensity = Mathf.Lerp(targetIntensity, 0f, Time.deltaTime * recoverySpeed);

            if (currentIntensity < 0.01f && targetIntensity < 0.01f) {
                hitBone = null;
                return;
            }

            Transform currentBone = hitBone;
            float boneIntensity = currentIntensity;

            while (currentBone != null && currentBone != transform && boneIntensity > 0.05f) {
                if (currentBone == rigRootBone)                
                    break;                

                Vector3 directionToHit;

                if (currentBone == hitBone) {
                    if (currentBone.parent != null)
                        directionToHit = (currentBone.position - currentBone.parent.position).normalized;
                    else
                        directionToHit = currentBone.up;
                }
                else                
                    directionToHit = (hitBone.position - currentBone.position).normalized;                

                Vector3 bendAxis = Vector3.Cross(directionToHit, hitDirection).normalized;
                if (bendAxis != Vector3.zero) {
                    float angle = maxBendAngle * boneIntensity;
                    Quaternion bendRotation = Quaternion.AngleAxis(angle, bendAxis);
                    currentBone.rotation = bendRotation * currentBone.rotation;
                }

                currentBone = currentBone.parent;
                boneIntensity *= propagationDecay;
            }
        }
    }
}
using System.Collections.Generic;
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

        [Header("Bone Modifiers")]
        [SerializeField] private BoneModifier[] boneModifiers;

        private Transform hitBone;
        private Vector3 hitDirection;
        private float currentIntensity;
        private float targetIntensity;

        private Enemy_Manager enemyManager;
        private Dictionary<Transform, float> boneModifierDict;

        [System.Serializable]
        public struct BoneModifier {
            public Transform bone;
            [Range(0f, 1f)] [Tooltip("1 = Default reaction. 0 = No reaction")]
            public float intensityMultiplier;
        }

        private void Awake() {
            enemyManager = GetComponent<Enemy_Manager>();
        }

        private void Start() {
            boneModifierDict = new Dictionary<Transform, float>();

            foreach (var mod in boneModifiers) {
                if (mod.bone != null && !boneModifierDict.ContainsKey(mod.bone))                
                    boneModifierDict.Add(mod.bone, mod.intensityMultiplier);                
            }
        }

        public void PlayHitReaction(Transform bone, Vector3 dir, float staggerAmount) {
            hitBone = bone;
            hitDirection = dir.normalized;

            targetIntensity = staggerAmount * intensityMultiplier; 
        }

        private void HandleHitReaction() {
            if (hitBone == null || enemyManager.IsDead)
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

                float localModifier = 1f;
                if (boneModifierDict.TryGetValue(currentBone, out float specificModifier))                
                    localModifier = specificModifier;                

                if (localModifier > 0f) {
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
                        float angle = maxBendAngle * boneIntensity * localModifier;
                        Quaternion bendRotation = Quaternion.AngleAxis(angle, bendAxis);
                        currentBone.rotation = bendRotation * currentBone.rotation;
                    }
                }

                currentBone = currentBone.parent;
                boneIntensity *= propagationDecay;
            }
        }

        private void LateUpdate() {
            HandleHitReaction();
        }
    }
}
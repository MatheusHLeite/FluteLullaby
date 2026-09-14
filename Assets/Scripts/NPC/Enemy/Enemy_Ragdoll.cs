using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace DelightStudio.AI {
    public class Enemy_Ragdoll : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Rigidbody m_hips;
        [SerializeField] private Rigidbody m_spine;

        [Header("Get up setup")]
        [SerializeField] private Transform m_leftShoulder;
        [SerializeField] private Transform m_rightShoulder;
        [SerializeField] private LayerMask m_groundLayer;

        private Enemy_Animator enemyAnimator;

        private Transform[] bones;
        private Rigidbody[] ragdollRigidbodies;
        private Collider[] ragdollColliders;

        private NavMeshAgent agent;
        private Animator animator;
        private Transform skin;

        private BoneTransformData[] ragdollBoneData;

        private bool isDead = false;
        private bool isRecovering;
        private bool isBlending;
        private float blendDuration = 0.5f;
        private float blendTimer;
        private float gettingUpMinCooldown;

        public event UnityAction<Vector3, Transform, Transform, bool> OnGetUp;

        private struct BoneTransformData {
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        public bool IsRagdoll { get; private set; }

        private void Awake() {
            enemyAnimator = GetComponent<Enemy_Animator>();

            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
            ragdollColliders = GetComponentsInChildren<Collider>().Where(c => !c.isTrigger).ToArray();

            bones = m_hips.GetComponentsInChildren<Transform>();
            ragdollBoneData = new BoneTransformData[bones.Length];

            skin = transform.GetChild(0);

            for (int i = 0; i < ragdollColliders.Length; i++) 
                for (int j = i + 1; j < ragdollColliders.Length; j++) 
                    Physics.IgnoreCollision(ragdollColliders[i], ragdollColliders[j], true);

            foreach (var rb in ragdollRigidbodies) {
                rb.maxDepenetrationVelocity = 2f;
                rb.solverIterations = 12;
            }

            ToggleRagdoll(false);
        }

        public void ToggleRagdoll(bool state) {
            if (state && isRecovering && !isDead)
                return;

            Vector3 cachedVelocity = agent.velocity;
            animator.enabled = !state;

            if (state) { 
                if (agent.enabled)
                    agent.isStopped = true;
                agent.enabled = false;
            }
            else {               
                agent.enabled = true;
                agent.isStopped = false;
            }

            foreach (var rb in ragdollRigidbodies)            
                rb.isKinematic = !state;

            if (state) {
                enemyAnimator.ResetAnimator();

                foreach (var rb in ragdollRigidbodies) {
                    rb.linearVelocity = cachedVelocity;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            IsRagdoll = state;
        }

        public void OnAIDied(Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part) {
            isDead = true;

            if (IsRagdoll && !isRecovering)
                return;

            ApplyForceOnClosestBone(hitPoint, hitDirection, impact, part);
        }

        internal void OnLegHit(Vector3 hitPoint, Vector3 hitDirection, float impact) {
            ApplyForceOnClosestBone(hitPoint, hitDirection, impact, BodyPart.Leg);

            gettingUpMinCooldown = Time.time + 2f;
            isRecovering = false;
        }

        private void ApplyForceOnClosestBone(Vector3 hitPoint, Vector3 hitDirection, float impact, BodyPart part) {
            ToggleRagdoll(true);
            
            Transform closestBoneTransform = GetClosestBonePrecisely(hitPoint);
            Rigidbody closestBone = closestBoneTransform != null ? 
                closestBoneTransform.GetComponent<Rigidbody>() : GetClosestBone(hitPoint);

            if (closestBone == null || part == BodyPart.Head) return;

            Vector3 finalForce = hitDirection.normalized * impact;

            if (closestBone == m_hips || closestBone.transform.parent == m_hips.transform ||
                closestBone == m_spine || closestBone.transform.parent == m_spine.transform)            
                closestBone.AddForce(finalForce, ForceMode.Impulse);            
            else            
                closestBone.AddForceAtPosition(finalForce, hitPoint, ForceMode.Impulse);            
        }

        #region Helpers
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

        private bool IsBellyUp() {
            float dot = Vector3.Dot(m_hips.transform.up, Vector3.up);
            return dot > 0;
        }
        #endregion

        private void RecoverFromRagdollState() {
            if (!IsRagdoll || isRecovering || m_hips.linearVelocity.magnitude >= 0.02f)
                return;

            if (gettingUpMinCooldown > Time.time) 
                return;
            
            isRecovering = true;

            bool isBellyUp = IsBellyUp();
            Vector3[] ragdollWorldPos = new Vector3[bones.Length];
            Quaternion[] ragdollWorldRot = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++) {
                ragdollWorldPos[i] = bones[i].position;
                ragdollWorldRot[i] = bones[i].rotation;
            }

            skin.SetParent(null);
            RagdollOnGetUp(isBellyUp);
            skin.SetParent(transform);
            skin.localPosition = Vector3.zero;
            skin.localRotation = Quaternion.identity;

            for (int i = 0; i < bones.Length; i++) {
                Transform b = bones[i];
                Vector3 localPos;
                Quaternion localRot;

                if (b.parent != null) {
                    localPos = b.parent.InverseTransformPoint(ragdollWorldPos[i]);
                    localRot = Quaternion.Inverse(b.parent.rotation) * ragdollWorldRot[i];
                }
                else {
                    localPos = ragdollWorldPos[i];
                    localRot = ragdollWorldRot[i];
                }

                ragdollBoneData[i] = new BoneTransformData {
                    localPosition = localPos,
                    localRotation = localRot
                };

                b.localPosition = localPos;
                b.localRotation = localRot;
            }

            enemyAnimator.SampleGetUpPose(isBellyUp);

            isBlending = true;
            blendTimer = 0f;
        }

        private void RagdollOnGetUp(bool isBellyUp) {
            Vector3 pos = m_hips.position;
            Vector3 right = m_rightShoulder.position - m_leftShoulder.position;
            Vector3 up = isBellyUp ? Vector3.up : Vector3.down;

            Vector3 forward = isBellyUp ?
                -Vector3.Cross(up, right).normalized :
                Vector3.Cross(up, right).normalized;

            if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 12f, m_groundLayer))
                pos = hit.point;

            transform.SetPositionAndRotation(
                pos,
                Quaternion.LookRotation(forward)
            );
        }

        public void GetUp() {
            isRecovering = false;
            
            ToggleRagdoll(false);
        }

        private void Update() {
            if (isDead) return;

            RecoverFromRagdollState();
        }

        private void LateUpdate() {
            if (isDead) return;
            if (!isBlending) return;

            blendTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blendTimer / blendDuration);

            float curveT = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < bones.Length; i++) {
                bones[i].localPosition = Vector3.Lerp(ragdollBoneData[i].localPosition, bones[i].localPosition, curveT);
                bones[i].localRotation = Quaternion.Slerp(ragdollBoneData[i].localRotation, bones[i].localRotation, curveT);
            }

            if (t >= 1f)
                isBlending = false;
        }
    }
}
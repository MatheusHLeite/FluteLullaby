using Unity.Netcode;
using UnityEngine;

namespace DelightStudio.Weapons {
    public class Arrow : NetworkBehaviour {
        [Header("Arrow")]
        [SerializeField] private float m_maxLifeTime = 10f;
        [SerializeField] private Vector2 m_gravityRange = new Vector2(4f, 0.7f);
        [SerializeField] private GameObject m_vfx;

        private float m_damage;
        private float m_impact;
        private bool m_hasHit;
        private float m_currentGravity;
        private float m_gravityMultiplier;

        private Collider _thisCollider;
        private Rigidbody _rigidbody;

        private Transform _stuckTarget;
        private Vector3 _stuckLocalPosition;
        private Quaternion _stuckLocalRotation;

        #region Initialization
        public override void OnNetworkSpawn() {
            if (!IsServer)
                return;

            Invoke(nameof(DespawnArrow), m_maxLifeTime);
        }        

        public void Initialize(float damage, float impact, float gravityMultiplier01, Collider colliderToIgnore) {
            _rigidbody = GetComponent<Rigidbody>();
            _thisCollider = GetComponent<Collider>();

            m_currentGravity = 0;
            m_damage = damage;
            m_impact = impact;
            m_gravityMultiplier = Mathf.Lerp(m_gravityRange.x, m_gravityRange.y, gravityMultiplier01);

            m_vfx.SetActive(true);
            Physics.IgnoreCollision(colliderToIgnore, _thisCollider, true);
        }
        #endregion

        private void OnCollisionEnter(Collision collision) {
            if (!IsServer || m_hasHit)
                return;

            m_hasHit = true;
            ContactPoint contact = collision.GetContact(0);
            Vector3 direction = _rigidbody.linearVelocity.normalized;

            if (collision.collider.TryGetComponent(out Damagable_BodyPart damagable)) 
                damagable.TakeDamage(m_damage, contact.point, direction, m_impact);

            StickArrow(contact, collision.transform);
        }

        private void StickArrow(ContactPoint contact, Transform target) {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;

            Destroy(_thisCollider);

            _stuckTarget = target;

            _stuckLocalPosition = target.InverseTransformPoint(contact.point);
            _stuckLocalRotation = Quaternion.Inverse(target.rotation) * transform.rotation;

            transform.position = contact.point;

            m_vfx.SetActive(false);
        }

        private void DespawnArrow() {
            if (NetworkObject.IsSpawned)
                NetworkObject.Despawn();
        }

        private void FixedUpdate() {
            if (!IsServer || m_hasHit)
                return;
            if (_rigidbody.linearVelocity.sqrMagnitude <= 0.01f)
                return;

            m_currentGravity += Time.deltaTime * m_gravityMultiplier;
            m_currentGravity = Mathf.Clamp(m_currentGravity, 0, 10f);

            _rigidbody.AddForce(Physics.gravity * m_currentGravity, ForceMode.Acceleration);
            transform.rotation = Quaternion.LookRotation(_rigidbody.linearVelocity);
        }

        private void LateUpdate() {
            if (!IsServer || !m_hasHit)
                return;

            transform.position = _stuckTarget.TransformPoint(_stuckLocalPosition);
            transform.rotation = _stuckTarget.rotation * _stuckLocalRotation;
        }
    }
}
using Unity.Netcode;
using UnityEngine;

namespace DelightStudio.Weapons {
    public class Weapon_Bow : Weapon_Firearm {
        [Header("Bow Settings")]
        [SerializeField] private Arrow m_arrowPrefab;
        [SerializeField] private Transform m_arrowSpawnPoint;        

        [SerializeField] private float m_minForce = 10f;
        [SerializeField] private float m_maxForce = 40f;
        [SerializeField] private float m_maxChargeTime = 1.5f;

        [SerializeField] private LayerMask m_aimLayerMask;

        [Header("Bow string")]
        [SerializeField] private LineRenderer m_stringRenderer;
        [SerializeField] private Vector2 m_minMaxStringPos = new Vector2(0.29f, 1.012f);

        [Header("Arrow")]
        [SerializeField] private Transform m_arrowVisual;
        [SerializeField] private Vector2 m_minMaxArrowPos = new Vector3(-0.1818f, 0.32f);

        private bool m_isCharging;
        private float m_chargeTime;

        private Collider ownerCollider;

        private void Awake() {
            m_arrowVisual.gameObject.SetActive(false);
        }

        protected override void FireDown(Player_CombatSystem combat) {
            if (m_isCharging)
                return;

            combat.SetBowDraw();
            m_arrowVisual.gameObject.SetActive(true);

            m_isCharging = true;
            m_chargeTime = 0f;
        }

        protected override void FireHold(Player_CombatSystem combat) {
            if (!m_isCharging)
                return;

            m_chargeTime = Mathf.Min(m_chargeTime + Time.deltaTime, m_maxChargeTime);
            UpdateString();
        }

        protected override void FireUp(Player_CombatSystem combat) {
            m_arrowVisual.gameObject.SetActive(false);

            if (!m_isCharging)
                return;

            Vector3 aimDirection = GetProjectileAimDirection(m_arrowSpawnPoint.position, m_aimLayerMask);
            int id = combat.GetInstanceID();
            float charge01 = GetCharge01();
            float damage = GetDamage(charge01);

            m_isCharging = false;
            m_chargeTime = 0f;

            OnStringShot();

            if (!OnProjectileShotPerformed())
                return;
            
            combat.RequestBowShotServerRpc(aimDirection, damage, charge01, id);
        }

        public void FireArrowOnServer(Vector3 direction, float charge01, float damage, int id) {
            if (!NetworkManager.Singleton.IsServer)
                return;

            charge01 = Mathf.Clamp01(charge01);
            float force = Mathf.Lerp(m_minForce, m_maxForce, charge01);
            Vector3 spawnPosition = m_arrowSpawnPoint.position;
            Quaternion rotation = Quaternion.LookRotation(direction);

            Arrow arrowObject = Instantiate(m_arrowPrefab, spawnPosition, rotation);

            if (!arrowObject.TryGetComponent(out NetworkObject networkObject)) {
                Destroy(arrowObject);
                return;
            }

            if (ownerCollider == null) {
                foreach (var obj in FindObjectsByType<Player_CombatSystem>(FindObjectsSortMode.None)) {
                    if (obj.GetInstanceID() == id) {
                        ownerCollider = obj.GetComponent<Collider>();
                        break;
                    }
                }
            }

            Vector3 finalForce = direction * force;

            arrowObject.Initialize(damage, _impact, charge01, ownerCollider);
            networkObject.Spawn();

            if (!arrowObject.TryGetComponent(out Rigidbody rb))
                return;

            rb.useGravity = false;
            rb.linearVelocity = finalForce;
        }

        private void UpdateString() {
            float charge01 = GetCharge01();
            float stringXPos = Mathf.Lerp(m_minMaxStringPos.x, m_minMaxStringPos.y, charge01);
            Vector3 stringPos = new Vector3(stringXPos, -0.045f, -0.021f);

            float arrowZPos = Mathf.Lerp(m_minMaxArrowPos.x, m_minMaxArrowPos.y, charge01);
            Vector3 arrowPos = new Vector3(0.015f, -0.032f, arrowZPos);

            m_stringRenderer.SetPosition(1, stringPos);
            m_arrowVisual.transform.localPosition = arrowPos;
        }

        private void OnStringShot() {
            float maxTime = 0.9f;
            float time = maxTime;

            while (time > 0) {
                time -= Time.deltaTime;
                float time01 = time / maxTime;
                float position = Mathf.Lerp(m_minMaxStringPos.x, m_minMaxStringPos.y, time01);

                Vector3 stringPos = new Vector3(position, -0.045f, -0.021f);
                m_stringRenderer.SetPosition(1, stringPos);
            }

            Vector3 endPos = new Vector3(m_minMaxStringPos.y, -0.045f, -0.021f);
            m_stringRenderer.SetPosition(1, endPos);
        }

        private float GetCharge01() {
            return Mathf.Clamp01(m_chargeTime / m_maxChargeTime);
        }

        private float GetDamage(float charge01) {
            return Mathf.Lerp(m_damage / 1.5f, m_damage * 1.8f, charge01);
        }
    }
}
using Unity.Netcode;
using UnityEngine;

public class DamageSource : NetworkBehaviour {
    private float damage;
    private float impact;

    private Collider collider;

    private void Awake() {
        collider = GetComponent<Collider>();
    }

    public void Setup(float damage, float impact) {
        this.damage = damage;
        this.impact = impact;
    }

    public void SetHitBoxState(bool enabled) {
        collider.enabled = enabled;
    }

    private void OnTriggerEnter(Collider other) {
        if (!IsServer) return;
        if (other.isTrigger || !collider.enabled)
            return;

        if (other.TryGetComponent(out Player_HealthSystem player)) {
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;
            player.TakeDamage(damage, transform.position, hitDirection, impact);
        }
    }
}

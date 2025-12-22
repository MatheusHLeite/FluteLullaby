using UnityEngine;

public class Damagable_BodyPart : MonoBehaviour {
    [SerializeField] private BodyPart bodyPart;

    private IDamageable damageBase;
    private float damageMultiplier;

    private void Awake() {
        damageMultiplier = Singleton.Instance.GameManager.GetDamageMultiplier(bodyPart);
        damageBase = transform.root.GetComponent<IDamageable>();
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, float impact) {
        damageBase.TakeDamage(damage * damageMultiplier, hitPoint, hitDirection, impact);        
    }
}

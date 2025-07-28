
using UnityEngine;

public class Weapon_DamageTrigger : MonoBehaviour {
    [Header("Setup")]    
    [SerializeField] private MeleeWeapon_SO m_thisWeapon;

    private float m_damage;

    private BoxCollider m_collider;

    private void Awake() { //[TODO] Remove it, just for testing purposes
        Setup(m_thisWeapon);
    }

    public void Setup(MeleeWeapon_SO weapon) {
        m_collider = GetComponent<BoxCollider>();

        m_damage = weapon.m_damage;

        m_collider.center = new Vector3(0, weapon.m_hitboxSize.y / 2, 0);
        m_collider.size = weapon.m_hitboxSize;
    }

    public void SetDamage(float damage) => m_damage = damage;
    public void IncreaseDamage(float damage) => m_damage += damage;

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out Enemy enemy)) {
            enemy.TakeDamage(m_damage);
        }
    }
}

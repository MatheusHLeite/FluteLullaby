using UnityEngine;

public class Weapon_Revolver : Weapon_Firearm {
    protected override void Fire() {
        OnShot();

        AudioSystem.CallPlayShotSFX(WeaponClass.Revolver);

        Physics.Raycast(ray, out hit, m_range, ~layerToIgnore);

        Vector3 dir = ray.direction;

        if (hit.collider != null && hit.collider.TryGetComponent(out Damagable_BodyPart damagable))
            damagable.TakeDamage(m_damage, hit.point, dir, m_impact);

        Singleton.Instance.GameEvents.OnShot?.Invoke(weaponMuzzle.position, hit, dir);
    }
}

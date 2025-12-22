using UnityEngine;

public class Weapon_Revolver : Weapon_Firearm {
    protected override void Fire() {
        OnShot();

        AudioSystem.CallPlayShotSFX(Weapons.Revolver);

        Physics.Raycast(ray, out hit, m_range, ~layerToIgnore);

        Vector3 dir = ray.direction;
        //dir = (hit.point - weaponMuzzle.position).normalized;

        if (hit.collider != null && hit.collider.TryGetComponent(out Damagable_BodyPart damagable))
            damagable.TakeDamage(m_damage, hit.point, dir, m_impact);

        /*else
            dir = ray.direction;*/

        Singleton.Instance.GameEvents.OnShot?.Invoke(weaponMuzzle.position, hit, dir);
    }
}

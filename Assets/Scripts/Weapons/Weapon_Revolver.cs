using UnityEngine;

public class Weapon_Revolver : Weapon_Firearm {
    protected override void Fire() {
        OnShot();

        AudioSystem.CallPlayShotSFX(Weapons.Revolver);

        Physics.Raycast(ray, out hit, m_range, ~layerToIgnore);

        Vector3 dir;

        if (hit.collider != null)
        {
            dir = (hit.point - weaponMuzzle.position).normalized;

            if (hit.collider.TryGetComponent(out Enemy enemy))
                enemy.TakeDamage(m_damage);

            if (hit.collider.TryGetComponent(out Player_BodyPart player))
                player.TakeDamage(m_damage, hit.point, dir, m_impact);

            if (hit.collider.TryGetComponent(out NPC_HealthHandler npc))
                npc.TakeDamage(m_damage, hit.point, dir, m_impact);
        }
        else
            dir = ray.direction;


        Singleton.Instance.GameEvents.OnShot?.Invoke(weaponMuzzle.position, hit, dir);
    }
}

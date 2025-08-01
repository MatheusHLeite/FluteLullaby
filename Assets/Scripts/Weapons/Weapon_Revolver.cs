using UnityEngine;

public class Weapon_Revolver : Weapon_Firearm {
    protected override void Fire() {
        OnShot();

        AudioSystem.Play3DAudio(Weapons.Revolver);

        Physics.Raycast(ray, out hit, m_range); 

        if (hit.collider != null) {
            if (hit.collider.TryGetComponent(out Enemy enemy)) 
                enemy.TakeDamage(m_damage);            

            if (hit.collider.TryGetComponent(out Player_BodyPart player)) 
                player.TakeDamage(m_damage, hit.point, ray.direction, m_impact);

            Singleton.Instance.GameEvents.OnShotHit?.Invoke(hit);
        }  
    }
}

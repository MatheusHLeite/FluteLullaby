using UnityEngine;

public class Weapon_Revolver : Weapon_Firearm {
    protected override void Fire() {
        PerformShot(m_damage);
        OnShot();
    }
}

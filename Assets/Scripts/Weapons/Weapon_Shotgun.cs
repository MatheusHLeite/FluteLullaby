using UnityEngine;

public class Weapon_Shotgun : Weapon_Firearm {
    [Header("Shotgun settings")]
    [SerializeField] private int pelletCount = 8;

    protected override void Fire() {
        float damage = m_damage / pelletCount;

        for (int i = 0; i < pelletCount; i++) 
            PerformShot(damage);        

        OnShot();
    }
}
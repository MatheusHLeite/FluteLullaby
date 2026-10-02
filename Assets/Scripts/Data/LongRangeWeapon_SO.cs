using DelightStudio.Data;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New_longRangeWeapon", menuName = "Data/Weapons/New long range weapon")]
public class LongRangeWeapon_SO : Weapon {
    [BoxGroup("Firearm setup")] [Min(0)] public int m_maxAmmo = 6;
    [BoxGroup("Firearm setup")] [Min(0)] public float m_range = 100f;    
    [BoxGroup("Firearm setup")] [Min(0)] public float m_recoilForce = 45f;
    [BoxGroup("Firearm setup")] [Min(0)] public float m_fireRate = 0.1f;
    [BoxGroup("Firearm setup")] [Range(0, 8)] public float m_minSpread = 0f;
    [BoxGroup("Firearm setup")] [Range(8, 30)] public float m_maxSpread = 8f;
    [BoxGroup("Firearm setup")] [Range(0, 30)] public float m_spreadPerShot = 3f;
    [BoxGroup("Firearm setup")] [Range(0.1f, 1f)] public float m_spreadRecovery = 0.4f;
    [BoxGroup("Firearm setup")] [Range(1f, 10f)] public float m_defaultSpreadRecoveryTime = 2f;
    [BoxGroup("Firearm data")] public Item_SO m_ammo;
}

using DelightStudio.Data;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New_longRangeWeapon", menuName = "Data/Weapons/New long range weapon")]
public class LongRangeWeapon_SO : Weapon {
    [BoxGroup("Firearm setup")] [Min(0)] public int m_maxAmmo = 6;
    [BoxGroup("Firearm setup")] [Min(0)] public float m_range = 100f;    
    [BoxGroup("Firearm setup")] [Min(0)] public float m_recoilForce = 45f;
    [BoxGroup("Firearm setup")] [Range(120f, 350f)] public float m_impactForce = 120;    
    [BoxGroup("Firearm data")] public Item_SO m_ammo;
}

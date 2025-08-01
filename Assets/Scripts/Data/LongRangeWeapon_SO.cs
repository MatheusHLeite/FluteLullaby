using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New_longRangeWeapon", menuName = "Data/Weapons/New long range weapon")]
public class LongRangeWeapon_SO : Item_SO {
    [BoxGroup("Weapon setup")] public Weapons m_weapon;
    [BoxGroup("Weapon setup")] [Min(0)] public float m_damage;
    [BoxGroup("Weapon setup")] [Min(0)] public int m_maxAmmo = 6;
    [BoxGroup("Weapon setup")] [Min(0)] public float m_range = 100f;    
    [BoxGroup("Weapon setup")] [Min(0)] public float m_recoilForce = 45f;
    [BoxGroup("Weapon setup")] [Range(120f, 350f)] public float m_impactForce = 120;    
}

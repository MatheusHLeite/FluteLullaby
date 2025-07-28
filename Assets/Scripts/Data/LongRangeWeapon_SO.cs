using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New_longRangeWeapon", menuName = "Data/Weapons/New long range weapon")]
public class LongRangeWeapon_SO : Item_SO {
    [BoxGroup("Weapon setup", centerLabel: true)] [ReadOnly] public ItemType m_weaponType = ItemType.Firearm;
    [Space(15)]
    [BoxGroup("Weapon setup")] [Min(0)] public float m_damage;
    [BoxGroup("Weapon setup")] [Min(0)] public int m_maxAmmo = 6;
    [BoxGroup("Weapon setup")] [Min(0)] public float m_range = 100f;
    [BoxGroup("Weapon setup")] [Range(1f, 2.5f)] public float m_fireRateMultiplier = 1f;
    [BoxGroup("Weapon setup")] [Range(1f, 2.5f)] public float m_reloadSpeedMultiplier = 1f;
    [BoxGroup("Weapon setup")] [Range(120f, 350f)] public float m_impactForce = 120; //[TODO] remove it - 150 for revolver t
    [BoxGroup("Weapon setup")] [Min(0)] public float m_weaponRecoilForce;    
}

using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New_meleeWeapon", menuName = "Data/Weapons/New melee weapon")]
public class MeleeWeapon_SO : Item_SO {
    [BoxGroup("Weapon setup")] [Min(0)] public float m_damage;
    [BoxGroup("Weapon setup")] public Vector3 m_hitboxSize;
}
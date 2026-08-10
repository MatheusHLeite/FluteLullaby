using DelightStudio.Data;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "New_meleeWeapon", menuName = "Data/Weapons/New melee weapon")]
public class MeleeWeapon_SO : Weapon {    
    [BoxGroup("Melee setup")] public Vector3 m_hitboxSize;
}
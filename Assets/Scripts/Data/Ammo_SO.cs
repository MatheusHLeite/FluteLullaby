using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Ammo_WEAPON", menuName = "Data/Weapons/New ammo")]
public class Ammo_SO : Item_SO {
    [BoxGroup("Weapon setup")] public WeaponClass m_weapon;
}

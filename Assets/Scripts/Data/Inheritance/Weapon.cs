using Sirenix.OdinInspector;
using UnityEngine;

namespace DelightStudio.Data {
    public class Weapon : Item_SO {
        [BoxGroup("Weapon setup")][Min(0)] public float m_damage;
        [BoxGroup("Weapon setup")] public WeaponClass m_weaponType;
        [BoxGroup("Weapon setup")] public AnimatorOverrideController m_overrideController;
    }
}
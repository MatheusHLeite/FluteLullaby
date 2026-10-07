using Sirenix.OdinInspector;
using UnityEngine;

namespace DelightStudio.Data {
    public class Weapon : Item_SO {
        [BoxGroup("Weapon setup")] [Range(5f, 150f)] public float m_damage = 5;
        [BoxGroup("Weapon setup")] [Range(5f, 180f)] public float m_impactForce = 90;
        [BoxGroup("Weapon setup")] public WeaponClass m_weaponType;
        [BoxGroup("Weapon setup")] public HandSide m_handSide;
        [BoxGroup("Weapon setup")] public AnimatorOverrideController m_overrideController;
    }
}
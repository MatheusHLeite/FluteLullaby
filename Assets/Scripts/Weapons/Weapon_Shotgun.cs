using UnityEngine;

namespace DelightStudio.Weapons {
    public class Weapon_Shotgun : Weapon_Firearm {
        [Header("Shotgun settings")]
        [SerializeField] private int pelletCount = 8;

        protected override void FireDown(Player_CombatSystem combat) {
            if (!OnShotPerformed(combat))
                return;

            float damage = m_damage / pelletCount;

            for (int i = 0; i < pelletCount; i++)
                PerformShot(damage);

            OnShot();
        }

        protected override void FireHold(Player_CombatSystem combat) { }

        protected override void FireUp(Player_CombatSystem combat) { }
    }
}
namespace DelightStudio.Weapons {
    public class Weapon_Revolver : Weapon_Firearm {
        protected override void FireDown(Player_CombatSystem combat) {
            if (!OnShotPerformed(combat))
                return;

            PerformShot(m_damage);
            OnShot();
        }

        protected override void FireHold(Player_CombatSystem combat) { }

        protected override void FireUp(Player_CombatSystem combat) { }
    }
}
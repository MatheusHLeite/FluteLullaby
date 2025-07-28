using UnityEngine;

public class Animator_WeaponReload : StateMachineBehaviour {
    private Weapon_Firearm _weapon;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (_weapon == null) _weapon = animator.GetComponent<Weapon_Firearm>();
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        _weapon.OnReloadEnd();
    }
}

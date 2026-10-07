using DelightStudio.Player;
using UnityEngine;

public class Animator_WeaponShot : StateMachineBehaviour {
    private Player_HandAnimatorEventCaller _handAnimator;

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        _handAnimator ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        _handAnimator.SetBowFire();
    }
}

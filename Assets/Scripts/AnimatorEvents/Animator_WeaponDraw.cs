using DelightStudio.Player;
using UnityEngine;

public class Animator_WeaponDraw : StateMachineBehaviour {
    private Player_HandAnimatorEventCaller eventCaller;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        eventCaller ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        eventCaller.OnDrawAnimationStarted();
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        eventCaller ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        eventCaller.OnDrawAnimationEnded();
    }
}

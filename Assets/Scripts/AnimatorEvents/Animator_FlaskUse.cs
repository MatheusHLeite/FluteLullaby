using DelightStudio.Player;
using UnityEngine;

public class Animator_FlaskUse : StateMachineBehaviour {
    private Player_HandAnimatorEventCaller eventCaller;

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        eventCaller ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        eventCaller.OnHealDrankAnimationEnded();
    }
}

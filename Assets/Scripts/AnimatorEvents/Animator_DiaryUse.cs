using DelightStudio.Player;
using UnityEngine;

public class Animator_DiaryUse : StateMachineBehaviour {
    private Player_HandAnimatorEventCaller eventCaller;

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        eventCaller ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        eventCaller.SetDiaryVisibility();
    }
}

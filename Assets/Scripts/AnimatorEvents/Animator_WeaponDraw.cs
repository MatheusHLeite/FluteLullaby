using DelightStudio.Player;
using UnityEngine;

public class Animator_WeaponDraw : StateMachineBehaviour {
    [SerializeField] private bool m_enableEnterState;
    [SerializeField] private bool m_enableExitState;

    private Player_HandAnimatorEventCaller eventCaller;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (!m_enableEnterState)
            return;

        eventCaller ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        eventCaller.OnDrawAnimationStarted();
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (!m_enableExitState)
            return;

        eventCaller ??= animator.GetComponent<Player_HandAnimatorEventCaller>();
        eventCaller.OnDrawAnimationEnded();
    }
}

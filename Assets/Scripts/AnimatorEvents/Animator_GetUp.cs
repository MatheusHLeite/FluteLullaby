using DelightStudio.AI;
using UnityEngine;

public class Animator_GetUp : StateMachineBehaviour {
    Enemy_Ragdoll ragdoll;
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
        if (stateInfo.normalizedTime < 0.86f)
            return;

        ragdoll ??= animator.GetComponent<Enemy_Ragdoll>();

        if (!animator.enabled)
            return;

        ragdoll.GetUp();
    }
}

using System;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace DelightStudio.AI {
    public class Enemy_Animator : MonoBehaviour {
        private Animator animator;
        private NetworkAnimator networkAnimator;

        private const string SPEED_FLOAT_PARAMETER = "Speed";
        private const string ATTACK_TRIGGER_PARAMETER = "Attack";
        private const string STAGGER_TRIGGER_PARAMETER = "Stagger";
        private const string STAGGER_BOOL_PARAMETER = "IsStaggered";

        private UnityAction animationEndedAction;
        private bool isAnimationRolling;

        private void Awake() {
            animator = GetComponent<Animator>();
            networkAnimator = GetComponent<NetworkAnimator>();
        }

        public void PlayAttackAnimation(UnityAction onAnimationEnded) {
            networkAnimator.SetTrigger(ATTACK_TRIGGER_PARAMETER);
            //animator.SetTrigger(ATTACK_TRIGGER_PARAMETER);
            animationEndedAction = onAnimationEnded;

            isAnimationRolling = true;
        }

        public void UpdateAnimatorSpeed(float targetSpeed) {
            animator.SetFloat(SPEED_FLOAT_PARAMETER, targetSpeed, 0.1f, Time.deltaTime);
        }

        internal void PlayStaggerAnimation() {
            animator.SetBool(STAGGER_BOOL_PARAMETER, true);
            networkAnimator.SetTrigger(STAGGER_TRIGGER_PARAMETER);
        }

        internal void ResetStagger() {
            animator.SetBool(STAGGER_BOOL_PARAMETER, false);
        }

        public void CallAnimationEndedEvent() {
            animationEndedAction?.Invoke();
            animationEndedAction = null;

            isAnimationRolling = false;
        }
    }
}
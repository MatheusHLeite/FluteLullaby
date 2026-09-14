using System;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace DelightStudio.AI {
    public class Enemy_Animator : MonoBehaviour {
        private Enemy_MotionSync motionSync;
        private Animator animator;
        private NetworkAnimator networkAnimator;

        private const string SPEED_FLOAT_PARAMETER = "Speed";
        private const string ATTACK_TRIGGER_PARAMETER = "Attack";
        private const string STAGGER_TRIGGER_PARAMETER = "Stagger";
        private const string STAGGER_BOOL_PARAMETER = "IsStaggered";

        private const string BELLY_UP_BOOLEAN = "bellyUp";

        private static readonly int GET_UP_BACK_HASH = Animator.StringToHash("anim_getUp_backUp");
        private static readonly int GET_UP_BELLY_HASH = Animator.StringToHash("anim_getUp_bellyUp");

        private UnityAction animationEndedAction;
        private bool isAnimationRolling;

        private void Awake() {
            motionSync = GetComponent<Enemy_MotionSync>();
            animator = GetComponent<Animator>();
            networkAnimator = GetComponent<NetworkAnimator>();
        }

        public void PlayAttackAnimation(Transform target, UnityAction onAnimationEnded) {
            motionSync.StartAttack(target);

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

        public void SampleGetUpPose(bool bellyUp) {
            animator.enabled = true;
            animator.SetBool(BELLY_UP_BOOLEAN, bellyUp);

            int state = bellyUp
                ? GET_UP_BELLY_HASH
                : GET_UP_BACK_HASH;

            animator.Play(state, 0, 0f);
            animator.Update(0f);
        }

        public void ResetAnimator() {
            animator.Play("None", 0, 0f);
            animator.Update(0f);
        }
    }
}
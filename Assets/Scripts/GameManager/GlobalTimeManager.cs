using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

namespace DelightStudio.Manager {
    public class GlobalTimeManager : NetworkBehaviour {
        [Header("Slow-Mo Settings")]        
        [SerializeField] private AnimationCurve criticalDeathSlowMoCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);
        [SerializeField] private AnimationCurve parrySlowMoCurve = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 1f);

        [Header("Audio")]
        [SerializeField] private AudioMixer mainMixer;

        private float defaultTimeScale = 1f;
        private float defaultFixedDeltaTime = 0.02f;

        private const string MIXER_PITCH = "MasterPitch";

        private Coroutine slowMoRoutine;

        private enum SlowMotionCurve { CriticalDeath, Parry }

        private void Start() {
            defaultTimeScale = Time.timeScale;
            defaultFixedDeltaTime = Time.fixedDeltaTime;

            ResetTimeScales();
        }

        public void TriggerCriticalDeathSlowMotion(float duration) {
            if (!IsServer) return;
            ExecuteSlowMoClientRpc(duration, SlowMotionCurve.CriticalDeath);
        }

        public void TriggerParrySlowMotion(float duration) {
            if (!IsServer) return;
            ExecuteSlowMoClientRpc(duration, SlowMotionCurve.Parry);
        }

        [ClientRpc]
        private void ExecuteSlowMoClientRpc(float duration, SlowMotionCurve curve) { 
            if (slowMoRoutine != null)
                StopCoroutine(slowMoRoutine);

            ResetTimeScales();
            slowMoRoutine = StartCoroutine(SlowMotionRoutine(duration, curve));
        }

        private IEnumerator SlowMotionRoutine(float slowMoDuration, SlowMotionCurve slowMoCurve) {
            float elapsed = 0f;
            AnimationCurve curve = GetAnimationCurve(slowMoCurve);

            yield return new WaitForSecondsRealtime(0.045f);

            while (elapsed < slowMoDuration) {
                elapsed += Time.unscaledDeltaTime;

                float t = elapsed / slowMoDuration;
                float currentTimeScale = curve.Evaluate(t);

                Time.timeScale = currentTimeScale;
                Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
                mainMixer.SetFloat(MIXER_PITCH, currentTimeScale);
                yield return null;
            }

            ResetTimeScales();
        }

        private AnimationCurve GetAnimationCurve(SlowMotionCurve curve) {
            return curve switch {
                SlowMotionCurve.Parry => parrySlowMoCurve, 
                SlowMotionCurve.CriticalDeath => criticalDeathSlowMoCurve,
                _ => null 
            };            
        }

        private void ResetTimeScales() {
            Time.timeScale = defaultTimeScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime;
            mainMixer.SetFloat(MIXER_PITCH, 1f);
        }
    }
}
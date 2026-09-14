using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DelightStudio.AI {
    public class Enemy_VisualHandler : MonoBehaviour {
        [Header("Damage feedback")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.05f;
        [SerializeField] private float flashIntensity = 2f;
        [SerializeField] private float hitStopDuration = 1.2f;

        [Header("Critical FX")]
        [SerializeField] private Transform neckBone;
        [SerializeField] private ParticleSystem criticalDamageFX;
        [SerializeField] private ParticleSystem bloodOffHeadlessFX;

        [Header("Blood decal")]
        [SerializeField] private GameObject[] bloodDecalPrefabs;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float maxDistanceBehind = 2.0f;

        private VolumeProfile profileVolume;

        private ChromaticAberration chromaticAberration;
        private Vignette vignette;
        private LensDistortion lensDistortion;

        private Coroutine impactCoroutine;
        private Coroutine flashCoroutine;

        private Renderer[] renderers;
        private MaterialPropertyBlock propBlock;

        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");        

        #region Initialization
        private void Awake() {
            propBlock = new MaterialPropertyBlock();
            renderers = GetComponentsInChildren<Renderer>();

            profileVolume = Singleton.Instance.SettingsManager.VolumeProfile;

            profileVolume.TryGet(out chromaticAberration);
            profileVolume.TryGet(out vignette);
            profileVolume.TryGet(out lensDistortion);
        }

        private void OnDisable() {
            ResetEffects();
        }
        #endregion

        public void OnHit(Vector3 hitPosition, Vector3 hitDirection) {
            PlayFlash();
            SpawnGroundBloodDecal(hitPosition, hitDirection);
        }

        #region Hit effect
        #region Post processing
        private IEnumerator ImpactRoutine() {
            float maxIntensity = .7f;
            float duration = 1.2f;

            float elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                float progress = elapsed / duration;

                float currentVal = Mathf.Sin(progress * Mathf.PI) * maxIntensity;

                chromaticAberration.intensity.value = currentVal;
                vignette.intensity.value = currentVal * 0.45f;
                lensDistortion.intensity.value = -currentVal * 0.4f;
                yield return null;
            }

            ResetEffects();
        }

        private void ResetEffects() {
            chromaticAberration.intensity.value = 0f;
            vignette.intensity.value = 0f;
            lensDistortion.intensity.value = 0f;
        }
        #endregion

        #region Flash on hit
        private void PlayFlash() {
            if (flashCoroutine != null)           
                StopCoroutine(flashCoroutine);            
            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine() {
            float elapsedTime = 0f;
            Color startColor = flashColor * flashIntensity;
            Color targetColor = Color.black;

            while (elapsedTime < flashDuration) {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / flashDuration;
                Color currentColor = Color.Lerp(startColor, targetColor, t);

                SetEmissionColor(currentColor);
                yield return null;
            }

            SetEmissionColor(Color.black);
        }

        private void SetEmissionColor(Color color) {
            foreach (Renderer r in renderers) {
                for (int i = 0; i < r.sharedMaterials.Length; i++) {
                    r.GetPropertyBlock(propBlock, i);
                    propBlock.SetColor(EmissionColorID, color);
                    r.SetPropertyBlock(propBlock, i);
                }
            }
        }
        #endregion

        #region Blood decal
        public void SpawnGroundBloodDecal(Vector3 hitPoint, Vector3 shotDirection) {
            if (bloodDecalPrefabs == null || bloodDecalPrefabs.Length == 0) return;

            Vector3 projectedExitPoint = hitPoint + (shotDirection.normalized * maxDistanceBehind);
            Vector3 rayOrigin = projectedExitPoint + Vector3.up;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, 5.0f, groundLayer)) {
                GameObject selectedDecal = bloodDecalPrefabs[Random.Range(0, bloodDecalPrefabs.Length)];

                Vector3 decalPosition = groundHit.point + (groundHit.normal * 0.005f);
                Quaternion groundRotation = Quaternion.FromToRotation(Vector3.up, groundHit.normal);

                GameObject decalInstance = Instantiate(selectedDecal, decalPosition, groundRotation);
                decalInstance.transform.forward = Vector3.ProjectOnPlane(shotDirection, groundHit.normal);
                decalInstance.transform.localScale = Vector3.one * Random.Range(2f, 6f);

                Destroy(decalInstance, 10f);
            }
        }
        #endregion
        #endregion

        #region Death effect
        public void OnCriticalDeath(bool isDeadAlready = false) {
            if (!isDeadAlready) {
                if (impactCoroutine != null)
                    StopCoroutine(impactCoroutine);
                impactCoroutine = StartCoroutine(ImpactRoutine());

                Singleton.Instance.GlobalTimeManager.TriggerCriticalDeathSlowMotion(hitStopDuration);
            }

            criticalDamageFX.transform.SetParent(null);

            criticalDamageFX.Play();
            bloodOffHeadlessFX.Play();
            neckBone.localScale = Vector3.zero;
        }

        public void OnCriticalBodyShot() {
            criticalDamageFX.transform.SetParent(null);

            criticalDamageFX.Play();
            neckBone.localScale = Vector3.zero;
        }
        #endregion
    }
}
using System.Collections;
using UnityEngine;

namespace DelightStudio.AI {
    public class Enemy_VisualHandler : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.05f;
        [SerializeField] private float flashIntensity = 2f;

        private Renderer[] renderers;
        private MaterialPropertyBlock propBlock;

        private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

        private Coroutine flashCoroutine;

        #region Initialization
        private void Awake() {
            propBlock = new MaterialPropertyBlock();
            renderers = GetComponentsInChildren<Renderer>();
        }
        #endregion

        public void PlayFlash() {
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

                foreach (Renderer r in renderers) {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor(EmissionColorID, currentColor);
                    r.SetPropertyBlock(propBlock);
                }

                yield return null;
            }

            foreach (Renderer r in renderers) {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(EmissionColorID, Color.black);
                r.SetPropertyBlock(propBlock);
            }
        }
    }
}
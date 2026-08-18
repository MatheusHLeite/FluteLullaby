using DelightStudio.Data;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace DelightStudio.UI {
    public class UI_TutorialVideoPlayer : MonoBehaviour {
        [Header("UI")]
        [SerializeField] private TMP_Text m_title;
        [SerializeField] private TMP_Text m_description;

        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private RenderTexture renderTexture;

        private Coroutine loadCoroutine;

        private VideoClip currentClip;
        private Tutorial_SO currentTutorial;

        private int loadVersion;

        public Tutorial_SO CurrentTutorial => currentTutorial;
        public bool IsLoading { get; private set; }

        private void Awake() {
            SetupVideoPlayer();
        }

        public void ResetTutorial() {
            m_title.text = string.Empty; 
            m_description.text = string.Empty;

            videoImage.enabled = false;

            Stop();
        }

        private void SetupVideoPlayer() {
            videoPlayer.playOnAwake = false;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.isLooping = true;

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;

            videoImage.texture = renderTexture;
        }

        public void PlayTutorial(Tutorial_SO tutorial) {
            if (tutorial == null)
                return;

            videoImage.enabled = true;

            m_title.text = tutorial.Title; 
            m_description.text = tutorial.Description;

            loadVersion++;

            if (loadCoroutine != null)
                StopCoroutine(loadCoroutine);

            loadCoroutine = StartCoroutine(LoadTutorialCoroutine(tutorial, loadVersion));
        }

        private IEnumerator LoadTutorialCoroutine(Tutorial_SO tutorial, int version) {
            IsLoading = true;

            StopCurrentVideo();

            currentTutorial = tutorial;

            if (tutorial.Video == null) {
                Debug.LogError($"Tutorial '{tutorial.name}' doesn't have a VideoClip.");
                yield break;
            }

            currentClip = tutorial.Video;

            videoPlayer.clip = currentClip;

            bool prepared = false;
            bool failed = false;

            VideoPlayer.EventHandler preparedHandler = _ => prepared = true;
            VideoPlayer.ErrorEventHandler errorHandler = (_, message) => {
                Debug.LogError($"Video error '{tutorial.name}': {message}");
                failed = true;
            };

            videoPlayer.prepareCompleted += preparedHandler;
            videoPlayer.errorReceived += errorHandler;

            videoPlayer.Prepare();

            while (!prepared && !failed) {
                if (version != loadVersion) {
                    videoPlayer.prepareCompleted -= preparedHandler;
                    videoPlayer.errorReceived -= errorHandler;
                    yield break;
                }
                yield return null;
            }

            videoPlayer.prepareCompleted -= preparedHandler;
            videoPlayer.errorReceived -= errorHandler;

            if (version != loadVersion)
                yield break;

            if (failed) {
                Debug.LogError($"Video error '{tutorial.name}");
                IsLoading = false;
                yield break;
            }

            videoPlayer.Play();

            IsLoading = false;
            loadCoroutine = null;
        }

        public void Stop() {
            loadVersion++;

            if (loadCoroutine != null) {
                StopCoroutine(loadCoroutine);
                loadCoroutine = null;
            }

            IsLoading = false;

            StopCurrentVideo();

            currentTutorial = null;
            currentClip = null;
        }

        private void StopCurrentVideo() {
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        private void OnDestroy() {
            loadVersion++;

            if (loadCoroutine != null)
                StopCoroutine(loadCoroutine);

            StopCurrentVideo();
        }
    }
}
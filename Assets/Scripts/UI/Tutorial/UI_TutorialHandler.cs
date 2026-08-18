using DelightStudio.Data;
using System.Collections.Generic;
using UnityEngine;

namespace DelightStudio.UI {
    public class UI_TutorialHandler : MonoBehaviour {
        [Header("References")]
        [SerializeField] private UI_TutorialVideoPlayer videoPlayer;

        [Header("UI")]
        [SerializeField] private UI_TutorialButton tutorialButtonPrefab;
        [SerializeField] private Transform tutorialButtonContainer;

        private readonly List<UI_TutorialButton> spawnedButtons = new();

        private void Awake() {
            Singleton.Instance.GameEvents.OnScreenSwitch.AddListener(StopTutorial);
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnScreenSwitch.RemoveListener(StopTutorial);
        }

        private void Start() {
            CreateTutorialButtons();

            Singleton.Instance.GameEvents.OnScreenSwitch?.Invoke();
        }

        private void CreateTutorialButtons() {
            ClearButtons();

            List<Tutorial_SO> tutorials = Singleton.Instance.TutorialManager.GetAllTutorials();

            foreach (Tutorial_SO tutorial in tutorials) {
                if (tutorial == null)
                    continue;

                UI_TutorialButton button = Instantiate(tutorialButtonPrefab, tutorialButtonContainer);

                button.Initialize(tutorial, SelectTutorial);
                spawnedButtons.Add(button);
            }
        }

        public void SelectTutorial(Tutorial_SO tutorial) {
            if (tutorial == null)
                return;

            StopTutorial();

            videoPlayer.PlayTutorial(tutorial);
            UpdateSelectedButton(tutorial);
        }

        private void UpdateSelectedButton(Tutorial_SO tutorial) {
            foreach (UI_TutorialButton button in spawnedButtons)            
                button.SetSelected(button.Tutorial == tutorial);            
        }

        public void StopTutorial() {
            videoPlayer.ResetTutorial();
            videoPlayer.Stop();
        }

        private void ClearButtons() {
            foreach (UI_TutorialButton button in spawnedButtons) {
                if (button != null)
                    Destroy(button.gameObject);
            }

            spawnedButtons.Clear();
        }
    }
}
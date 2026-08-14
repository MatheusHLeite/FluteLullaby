using UnityEngine;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class UI_TutorialHandler : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private TutorialSetup[] m_allTutorials;

        private void Start() {
            SetupButtons();
        }

        private void OnEnable() {
            foreach (var t in m_allTutorials)
                t.screen.SetActive(false);
        }

        private void SetupButtons() {
            foreach (var t in m_allTutorials) {
                Button btn = t.button;

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    SetTutorial(t.screen);
                });
            }
        }

        private void SetTutorial(GameObject screen) {
            foreach (var t in m_allTutorials)
                t.screen.SetActive(false);

            screen.SetActive(true);
        }
    }
}

[System.Serializable]
public struct TutorialSetup {
    public Button button;
    public GameObject screen;
}
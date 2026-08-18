using DelightStudio.Data;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class UI_TutorialButton : MonoBehaviour {
        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private GameObject selectedObject;

        private Button button;

        private Action<Tutorial_SO> onSelected;

        public Tutorial_SO Tutorial { get; private set; }

        public void Initialize(Tutorial_SO tutorial, Action<Tutorial_SO> onSelected) {
            button = GetComponent<Button>();

            Tutorial = tutorial;
            this.onSelected = onSelected;

            titleText.text = tutorial.Title;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);

            SetSelected(false);
        }

        private void OnClick() {
            onSelected?.Invoke(Tutorial);
        }

        public void SetSelected(bool selected) {
            if (selectedObject != null)
                selectedObject.SetActive(selected);
        }
    }
}

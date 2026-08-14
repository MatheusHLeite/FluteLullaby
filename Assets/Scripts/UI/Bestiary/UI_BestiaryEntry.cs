using DelightStudio.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DelightStudio.UI {
    public class UI_BestiaryEntry : MonoBehaviour, IPointerClickHandler {
        [Header("Setup")]
        [SerializeField] private TMP_Text m_enemyName;
        [SerializeField] private TMP_Text m_enemyIndex;
        [SerializeField] private GameObject m_newEntryNotification;

        private CanvasGroup thisCG;
   
        private UI_BestiaryHandler bestiaryHandler;
        private Enemy_SO bestiaryEnemy;
        private bool enemyDiscovered;
        private bool notificationRead;

        public Enemy_SO GetEnemy() => bestiaryEnemy;

        public void Setup(BestiaryData data, UI_BestiaryHandler bestiary) {
            thisCG = GetComponent<CanvasGroup>();

            enemyDiscovered = data.enemyDiscovered;
            notificationRead = data.notificationRead;

            bestiaryHandler = bestiary;
            bestiaryEnemy = data.enemy;

            m_enemyName.text = enemyDiscovered ? data.enemy.m_name : "???????";
            m_enemyIndex.text = $"#{data.enemy.m_entryIndex.ToString("000")}";

            m_newEntryNotification.SetActive(enemyDiscovered && !notificationRead);
            
            thisCG.blocksRaycasts = true;
            thisCG.interactable = enemyDiscovered ? true : false;
            thisCG.alpha = enemyDiscovered ? 1f : 0.75f;
        }

        public void UpdateBestiary(BestiaryData enemy) {
            if (enemy.enemy != bestiaryEnemy) return;

            enemyDiscovered = enemy.enemyDiscovered;
            notificationRead = enemy.notificationRead;

            m_enemyName.text = enemy.enemy.m_name;

            m_newEntryNotification.SetActive(!notificationRead);

            thisCG.interactable = true;
            thisCG.alpha = 1f;
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (!bestiaryHandler.IsShowingPopUp || bestiaryHandler.CurrentBeast != bestiaryEnemy) {
                bestiaryHandler.SetBeastVisualizationOn(bestiaryEnemy, enemyDiscovered);

                if (!notificationRead && enemyDiscovered) {
                    Singleton.Instance.GameEvents.OnUpdateBestiaryRead?.Invoke(bestiaryEnemy);

                    notificationRead = true;
                    m_newEntryNotification.SetActive(false);
                }
            }
            else
                bestiaryHandler.SetBeastVisualizationOff(false);
        }
    }
}
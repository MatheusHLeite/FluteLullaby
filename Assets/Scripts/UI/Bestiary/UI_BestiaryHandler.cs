using DelightStudio.Data;
using DelightStudio.Manager;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class UI_BestiaryHandler : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private UI_BestiaryEntry m_bestiaryEntryPrefab;
        [SerializeField] private Transform m_bestiaryEntriesHolder;

        [Header("UI")]
        [SerializeField] private TMP_Text m_enemyName;
        [SerializeField] private TMP_Text m_description;
        [SerializeField] private Image m_fullEnemyDraw;
        [SerializeField] private GameObject m_unknownEnemyOverlay;
        [SerializeField] private CanvasGroup m_fadeCanvasGroup;
        [SerializeField] private GameObject[] m_threatLevel;

        private List<UI_BestiaryEntry> allBestiaryEntries = new();

        private const string ENEMY_NOT_DISCOVERED = "Enemy not found yet, explore the world to find it";

        public bool IsShowingPopUp { get; private set; }
        public Enemy_SO CurrentBeast {  get; private set; }

        private void Awake() {
            Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
            Singleton.Instance.GameEvents.OnNewEnemyFound.AddListener(OnBestiaryUpdate);
            Singleton.Instance.GameEvents.OnBestiaryNotificationRead.AddListener(OnBestiaryUpdate);
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
            Singleton.Instance.GameEvents.OnNewEnemyFound.RemoveListener(OnBestiaryUpdate);
            Singleton.Instance.GameEvents.OnBestiaryNotificationRead.RemoveListener(OnBestiaryUpdate);
        }

        private void Start() {
            Setup();
        }

        private void Setup() {
            m_fadeCanvasGroup.alpha = 0.0f;
            m_fadeCanvasGroup.interactable = false;
            m_fadeCanvasGroup.blocksRaycasts = true;

            List<BestiaryData> allEnemies = Singleton.Instance.SaveManager.GetBestiaryDatas();

            foreach (var enemy in allEnemies) {
                UI_BestiaryEntry newEntry = Instantiate(m_bestiaryEntryPrefab, m_bestiaryEntriesHolder);                
                newEntry.Setup(enemy, this);

                allBestiaryEntries.Add(newEntry);
            }
        }

        private void OnGameResumed() => SetBeastVisualizationOff(false);

        private void OnBestiaryUpdate(BestiaryData data) {
            for (int i = 0; i < allBestiaryEntries.Count; i++) {
                UI_BestiaryEntry newEntry = allBestiaryEntries[i];

                if (newEntry.GetEnemy().id == data.enemyID) {
                    newEntry.UpdateBestiary(data);
                    break;
                }
            }
        }

        public void SetBeastVisualizationOn(Enemy_SO enemy, bool enemyDiscovered) {
            m_fadeCanvasGroup.DOKill();
            m_fadeCanvasGroup.DOFade(1, 0.45f);

            CurrentBeast = enemy;
            IsShowingPopUp = true;

            m_fullEnemyDraw.gameObject.SetActive(enemyDiscovered);
            m_unknownEnemyOverlay.SetActive(!enemyDiscovered);

            if (!enemyDiscovered) {
                for (int i = 0; i < m_threatLevel.Length; i++)
                    m_threatLevel[i].SetActive(false);

                m_enemyName.text = "?????";
                m_description.text = ENEMY_NOT_DISCOVERED;
                return;
            }

            int threatLevel = Mathf.Clamp(enemy.m_threatLevel, 1, 5);

            for (int i = 0; i < m_threatLevel.Length; i++)
                m_threatLevel[i].SetActive(i < threatLevel);

            m_enemyName.text = enemy.m_name;
            m_description.text = enemy.m_description;
            m_fullEnemyDraw.sprite = enemy.m_fullBodyIcon;
        }

        public void SetBeastVisualizationOff(bool immediate) {
            if (immediate) {
                m_fadeCanvasGroup.alpha = 0f;
                IsShowingPopUp = false;
                CurrentBeast = null;
                return;
            }

            m_fadeCanvasGroup.DOKill();
            m_fadeCanvasGroup.DOFade(0, 0.45f).OnComplete(() => {
                IsShowingPopUp = false;
                CurrentBeast = null;
            });
        }
    }
}
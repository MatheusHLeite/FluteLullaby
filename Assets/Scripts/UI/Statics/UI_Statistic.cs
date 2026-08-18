using TMPro;
using UnityEngine;

namespace DelightStudio.UI {
    public class UI_Statistic : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private TMP_Text m_statisticLabel;
        [SerializeField] private TMP_Text m_statisticValue;
        [SerializeField] private GameObject m_hiddenStatisticCover;

        [Header("ID")]
        [field: SerializeField]
        public StatisticId Id { get; private set; }

        private string monsterId;
        private bool alreadySet;

        public void SetID(StatisticId newId, string monsterId) { 
            Id = newId;
            this.monsterId = monsterId;

            UpdateEnemyCover();
        }

        public void Setup(Statistic statistic, string labelText) {
            if (alreadySet) return;
            alreadySet = true;

            m_statisticLabel.text = labelText;
            UpdateValue(statistic);

            Singleton.Instance.GameEvents.OnNewEnemyFound.AddListener(CheckEnemyEntry);
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnNewEnemyFound.RemoveListener(CheckEnemyEntry);
        }

        public void UpdateValue(Statistic statistic) { 
            m_statisticValue.text = statistic.GetDisplayValue();
            UpdateEnemyCover();
        }

        private void CheckEnemyEntry(BestiaryData data) {
            if (data.enemyID != monsterId) return;

            UpdateEnemyCover();
        }

        private void UpdateEnemyCover() {
            if (string.IsNullOrEmpty(monsterId)) return;

            bool isEnemyDiscovered = Singleton.Instance.SaveManager.IsMonsterDiscovered(monsterId);

            m_hiddenStatisticCover.SetActive(!isEnemyDiscovered);
            if (!isEnemyDiscovered)
                m_hiddenStatisticCover.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(-1f, 1f));
        }
    }
}
using DelightStudio.Data;
using System.Collections.Generic;
using UnityEngine;

namespace DelightStudio.UI {
    public class UI_StatisticsHandler : MonoBehaviour {
        [Header("Setup")]
        [SerializeField] private UI_Statistic[] m_statistics;

        [Header("Monster kills")]
        [SerializeField] private UI_Statistic m_statisticsPrefab;
        [SerializeField] private Transform m_statisticsHolder;

        private Dictionary<(StatisticId statId, string monsterId), UI_Statistic> _statisticsDictionary;

        private void Awake() {
            Singleton.Instance.GameEvents.OnStatisticUpdated.AddListener(UpdateStatistic);

            _statisticsDictionary = new Dictionary<(StatisticId statId, string monsterId), UI_Statistic>();
            foreach (var statistic in m_statistics)
                _statisticsDictionary[(statistic.Id, "")] = statistic;
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnStatisticUpdated.RemoveListener(UpdateStatistic);
        }

        private void Start() {
            List<Statistic> allGameStatistics = Singleton.Instance.SaveManager.PlayerData.allGameStatistics;

            foreach (var stat in allGameStatistics)
                SetupStatistic(stat.id, stat);
        }

        private void SetupStatistic(StatisticId id, Statistic stat) {
            if (stat.monsterId != string.Empty) {
                SetupMonstersStatistics(stat);
                return;
            }

            if (_statisticsDictionary.TryGetValue((id, ""), out var uiStatistic))            
                uiStatistic.Setup(stat, stat.GetLabelValue());
        }

        private void SetupMonstersStatistics(Statistic stat) {
            Enemy_SO enemy = Singleton.Instance.EnemiesManager.GetEnemyByID(stat.monsterId);
            var ui = Instantiate(m_statisticsPrefab, m_statisticsHolder);

            ui.SetID(StatisticId.EnemiesKilled, enemy.id);
            ui.Setup(stat, $"{enemy.m_name} killed");

            _statisticsDictionary[(ui.Id, enemy.id)] = ui;
        }

        private void UpdateStatistic(Statistic stat) {
            if (_statisticsDictionary.TryGetValue((stat.id, stat.monsterId), out var uiStatistic))            
                uiStatistic.UpdateValue(stat);            
        }
    }
}
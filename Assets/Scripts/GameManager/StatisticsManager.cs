using DelightStudio.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DelightStudio.Manager {
    public class StatisticsManager : MonoBehaviour {
        [SerializeField] private Statistic[] m_allGameStatistics;

        public List<Statistic> GetAllGameStatistics() {
            Enemy_SO[] allGameEnemies = Singleton.Instance.EnemiesManager.GetAllEnemies();
            List<Statistic> currentStatistics = m_allGameStatistics.ToList();

            foreach (var en in allGameEnemies)
                currentStatistics.Add(en.m_statistic);

            return currentStatistics; 
        }

        private void Awake() {
            Singleton.Instance.GameEvents.OnEnemyKilled.AddListener(UpdateEnemyKilled);
        }

        private void OnDestroy() {
            Singleton.Instance.GameEvents.OnEnemyKilled.RemoveListener(UpdateEnemyKilled);
        }

        #region Get
        private int GetGameTime() {
            return 0;
        }

        private Statistic GetCurrentStatistic(StatisticId id, string monsterId) {
            foreach (var statistic in Singleton.Instance.SaveManager.PlayerData.allGameStatistics) {
                if (statistic.id == id && statistic.monsterId == monsterId)
                    return statistic;
            }
            return new Statistic();
        }
        #endregion

        private void UpdateGameTime() {
            Statistic stat = GetCurrentStatistic(StatisticId.TotalPlaytime, string.Empty);
            stat.value = GetGameTime();

            Singleton.Instance.GameEvents.OnStatisticUpdated?.Invoke(stat);
        }

        private void UpdateEnemyKilled(Enemy_SO enemy) {
            Statistic stat = GetCurrentStatistic(StatisticId.EnemiesKilled, enemy.id);
            stat.value++;

            Singleton.Instance.GameEvents.OnStatisticUpdated?.Invoke(stat);
        }
    }
}
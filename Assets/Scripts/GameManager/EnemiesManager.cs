using DelightStudio.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DelightStudio.Manager {
    public class EnemiesManager : MonoBehaviour {
        [SerializeField] private Enemy_SO[] m_allEnemies;

        public List<BestiaryData> DefaultData() {
            List<BestiaryData> data = new List<BestiaryData>();
            foreach (var e in m_allEnemies) {
                BestiaryData newData = new BestiaryData {                     
                    enemy = e,
                    enemyDiscovered = false,
                    notificationRead = false,
                };
                data.Add(newData);
            }

            return data;
        }

        public Enemy_SO[] GetAllEnemies() => m_allEnemies;

        internal Enemy_SO GetEnemyByID(string monsterId) {
            return m_allEnemies.FirstOrDefault(enemy => enemy.id == monsterId);
        }

        [SerializeField] private Enemy_SO debugEnemy;
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
                Singleton.Instance.GameEvents.OnUpdateEnemyFound?.Invoke(debugEnemy);
        }
    }
}
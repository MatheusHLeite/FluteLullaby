using Sirenix.OdinInspector;
using UnityEngine;

namespace DelightStudio.Data {
    [CreateAssetMenu(fileName = "Enemy #000", menuName = "Data/New enemy entry")]
    public class Enemy_SO : ScriptableObject {
        [BoxGroup("Enemy setup")] public string m_name;
        [BoxGroup("Enemy setup")] public int m_entryIndex;
        [BoxGroup("Enemy setup")][TextArea(6, 10)] public string m_description;
        [BoxGroup("Enemy setup")][Range(1, 5)] public int m_threatLevel = 1;
        [BoxGroup("Enemy setup")] public Sprite m_fullBodyIcon;

        [BoxGroup("Gameplay setup")] [Min(10)] public int m_maxHealth;
        [BoxGroup("Gameplay setup")] [Min(0)] public int m_attackDamage;
        [BoxGroup("Gameplay setup")] [Min(0)] public int m_movementSpeed;
        [BoxGroup("Gameplay setup")] [Min(0)] public float m_maxStaggerAmount;
        [BoxGroup("Gameplay setup")] [Min(0)] public float m_maxStaggerTime;

        [BoxGroup("Enemy data setup")] public Statistic m_statistic;
        [BoxGroup("Enemy data setup")][GUIColor("#FFFF00")][ReadOnly] public string id;

        [BoxGroup("Enemy data setup")] [Button("Generate ID")]
        public void GenerateNewID() { 
            string newId = System.Guid.NewGuid().ToString();

            id = newId;
            m_statistic.monsterId = newId;
        }
    }
}
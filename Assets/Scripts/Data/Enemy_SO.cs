using Sirenix.OdinInspector;
using UnityEngine;

namespace DelightStudio.Data {
    [CreateAssetMenu(fileName = "Enemy #000", menuName = "Data/New enemy entry")]
    public class Enemy_SO : ScriptableObject {
        [BoxGroup("Enemy setup")] public string m_name;
        [BoxGroup("Enemy setup")] public int m_entryIndex;
        [BoxGroup("Enemy setup")] public Sprite m_fullBodyIcon;
        [BoxGroup("Enemy setup")] [TextArea(4,6)] public string m_description;
        [BoxGroup("Enemy setup")] [Range(1, 5)] public int m_threatLevel = 1;
        [Space(10)]
        [BoxGroup("Enemy setup")] public Statistic m_statistic;
        [BoxGroup("Enemy setup")][GUIColor("#FFFF00")][ReadOnly] public string id;

        [BoxGroup("Enemy setup")]
        [Button("Generate ID")]
        public void GenerateNewID() { 
            string newId = System.Guid.NewGuid().ToString();

            id = newId;
            m_statistic.monsterId = newId;
        }
    }
}
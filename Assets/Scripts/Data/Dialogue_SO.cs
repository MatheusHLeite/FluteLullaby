using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue [Character] [Index]", menuName = "Data/New dialogue")]
public class Dialogue_SO : ScriptableObject {
    [BoxGroup("Dialogue setup")] public string m_speaker;
    [BoxGroup("Dialogue setup")] public Dialogue[] m_dialogues;
    [Space(10)]
    [BoxGroup("Dialogue setup")] [GUIColor("#FFFF00")][ReadOnly] public string id;

    [BoxGroup("Dialogue setup")] [Button("Generate ID")]
    public void GenerateNewID() => id = System.Guid.NewGuid().ToString();
}

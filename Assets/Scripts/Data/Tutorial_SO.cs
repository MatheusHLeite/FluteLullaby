using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Video;

namespace DelightStudio.Data {
    [CreateAssetMenu(fileName = "Tutorial_", menuName = "Data/New tutorial")]
    public class Tutorial_SO : ScriptableObject {
        [Header("Setup")]
        [SerializeField] private string m_title;
        [TextArea(6,8)]
        [SerializeField] private string m_description;
        [SerializeField] private VideoClip m_video;

        [Space(10)]
        [ReadOnly] public string id;

        [Button("Generate ID")]
        public void GenerateNewID() => id = System.Guid.NewGuid().ToString();

        public string Title => m_title;
        public string Description => m_description;
        public VideoClip Video => m_video;
    }
}
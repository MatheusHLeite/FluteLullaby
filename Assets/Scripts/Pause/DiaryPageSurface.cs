using UnityEngine;

namespace DelightStudio.UI {
    public class DiaryPageSurface : MonoBehaviour {
        [Header("Interaction Compensation")]
        [Tooltip("Defina a área interna ativa da textura (0-1). Comece com (0,0,1,1).")]
        [SerializeField] private Rect _interactionSubRect = new Rect(0, 0, 1, 1);

        public bool TryGetNormalizedPosition(RaycastHit hit, out Vector2 normalizedPosition) {
            Vector2 rawUv = hit.textureCoord;
            
            float localX = Mathf.InverseLerp(_interactionSubRect.xMin, _interactionSubRect.xMax, rawUv.x);
            float localY = Mathf.InverseLerp(_interactionSubRect.yMin, _interactionSubRect.yMax, rawUv.y);

            normalizedPosition = new Vector2(localX, localY);
            return true;
        }
    }
}
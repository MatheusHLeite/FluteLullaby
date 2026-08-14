using UnityEngine;

namespace DelightStudio.UI {
    [RequireComponent(typeof(BoxCollider))]
    public class DiaryPageSurface : MonoBehaviour {
        private BoxCollider interactionCollider;

        private void Awake() {
            interactionCollider = GetComponent<BoxCollider>();
        }

        public bool TryGetNormalizedPosition(
            Vector3 worldPoint,
            out Vector2 normalizedPosition) {
            normalizedPosition = Vector2.zero;

            if (interactionCollider == null)
                return false;

            Vector3 localPoint =
                transform.InverseTransformPoint(worldPoint);

            Vector3 center = interactionCollider.center;
            Vector3 size = interactionCollider.size;

            float minX = center.x - size.x * 0.5f;
            float maxX = center.x + size.x * 0.5f;

            float minY = center.y - size.y * 0.5f;
            float maxY = center.y + size.y * 0.5f;

            float x = Mathf.InverseLerp(minX, maxX, localPoint.x);
            float y = Mathf.InverseLerp(minY, maxY, localPoint.y);

            normalizedPosition = new Vector2(
                Mathf.Clamp01(x),
                Mathf.Clamp01(y)
            );

            return true;
        }

        public bool Contains(Vector2 normalizedPosition) {
            return normalizedPosition.x >= 0f &&
                   normalizedPosition.x <= 1f &&
                   normalizedPosition.y >= 0f &&
                   normalizedPosition.y <= 1f;
        }
    }
}
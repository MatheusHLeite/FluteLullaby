using Sirenix.OdinInspector;
using UnityEngine;

public class VerticalLayout3D : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private float spacing = 0.2f;
    [SerializeField] private bool fromTopToBottom = true;

    [Button]
    public void RebuildLayout() {
        int childCount = transform.childCount;
        if (childCount == 0) return;

        float totalHeight = spacing * (childCount - 1);
        float direction = fromTopToBottom ? -1f : 1f;

        for (int i = 0; i < childCount; i++) {
            Transform child = transform.GetChild(i);

            float yOffset = (i * spacing) - (totalHeight / 2f);
            child.localPosition = new Vector3(0, yOffset * direction, 0);
        }
    }
}

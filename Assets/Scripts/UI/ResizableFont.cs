using TMPro;
using UnityEngine;

public class ResizableFont : MonoBehaviour {
    private TMP_Text thisText;

    [Header("Setup")]
    [SerializeField] private float smallFontSize = 22;
    [SerializeField] private float normalFontSize = 26;
    [SerializeField] private float bigFontSize = 30;

    private Size currentSize;

    private void Awake() {
        thisText = GetComponent<TMP_Text>();

        Singleton.Instance.GameEvents.OnFontSizeChanged.AddListener(OnFontSizeChanged);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnFontSizeChanged.RemoveListener(OnFontSizeChanged);
    }

    private void OnFontSizeChanged(int i) {
        currentSize = (Size)i;
        float correctSize = smallFontSize;

        switch (currentSize) {
            case Size.Small:
                correctSize = smallFontSize;
                break;
            case Size.Normal:
                correctSize = normalFontSize;
                break;
            case Size.Big:
                correctSize = bigFontSize;
                break;
        }

        thisText.fontSize = correctSize;
    }
}

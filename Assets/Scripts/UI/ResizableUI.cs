using UnityEngine;

public class ResizableUI : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private float smallSizeMultiplier = .8f;
    [SerializeField] private float normalSizeMultiplier = 1f;
    [SerializeField] private float bigSizeMultiplier = 1.2f;

    private Vector3 actualVectorSize;
    private Size actualSize;

    private void Awake() {
        Singleton.Instance.GameEvents.OnUISizeChanged.AddListener(OnUISizeChanged);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnUISizeChanged.RemoveListener(OnUISizeChanged);
    }

    private  void OnUISizeChanged(int i){
        actualSize = (Size)i;

        switch (actualSize) {
            case Size.Small:
                actualVectorSize = new Vector3(smallSizeMultiplier, smallSizeMultiplier, smallSizeMultiplier);
                break;
            case Size.Normal:
                actualVectorSize = new Vector3(normalSizeMultiplier, normalSizeMultiplier, normalSizeMultiplier);
                break;
            case Size.Big:
                actualVectorSize = new Vector3(bigSizeMultiplier, bigSizeMultiplier, bigSizeMultiplier);
                break;
        }

        ResizeUI();
    }

    private void ResizeUI() => transform.localScale = actualVectorSize;
}

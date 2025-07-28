using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColorPicker : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private RawImage colorImage;
    [SerializeField] private RectTransform pickerIndicator;

    public System.Action<Color32> OnColorChanged;

    private Texture2D colorTexture;

    #region Initialization
    void Start() {
        colorTexture = colorImage.texture as Texture2D;
    }

    private void OnDestroy() {
        Destroy(colorTexture);
    }
    #endregion

    public void OnPointerDown(PointerEventData eventData) => PickColor(eventData);

    public void OnDrag(PointerEventData eventData) => PickColor(eventData);

    void PickColor(PointerEventData eventData) {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            colorImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        Rect rect = colorImage.rectTransform.rect;
        float px = Mathf.Clamp01((localPoint.x - rect.x) / rect.width);
        float py = Mathf.Clamp01((localPoint.y - rect.y) / rect.height);

        int texX = Mathf.FloorToInt(px * colorTexture.width);
        int texY = Mathf.FloorToInt(py * colorTexture.height);

        Color color = colorTexture.GetPixel(texX, texY);
        OnColorChanged?.Invoke(color);

        if (pickerIndicator != null)
            pickerIndicator.anchoredPosition = localPoint;
    }
}

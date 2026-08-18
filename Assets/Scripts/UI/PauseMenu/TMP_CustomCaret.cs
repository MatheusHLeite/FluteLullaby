using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TMP_CustomCaret : MonoBehaviour {
    [Header("Caret")]
    [SerializeField] private float caretWidth = 2f;
    [SerializeField] private float caretHeight = 18f;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private float horizontalOffset = 0f;

    [Header("Blink")]
    [SerializeField] private float blinkSpeed = 0.5f;

    private TMP_InputField inputField;

    private RectTransform caret;
    private Image caretImage;

    private float blinkTimer;
    private bool caretVisible;

    private void Awake() {
        inputField = GetComponent<TMP_InputField>();

        CreateCaret();
    }

    private void OnEnable() {
        inputField.onSelect.AddListener(OnSelect);
        inputField.onDeselect.AddListener(OnDeselect);
    }

    private void OnDisable() {
        inputField.onSelect.RemoveListener(OnSelect);
        inputField.onDeselect.RemoveListener(OnDeselect);
    }

    private void CreateCaret() {
        GameObject obj = new GameObject("CustomCaret");

        obj.transform.SetParent(inputField.textViewport, false);

        caret = obj.AddComponent<RectTransform>();
        caretImage = obj.AddComponent<Image>();

        caretImage.raycastTarget = false;
        caretImage.color = Color.black;

        caret.sizeDelta = new Vector2(caretWidth, caretHeight);

        caretVisible = true;
    }

    private void UpdateCaretPosition() {
        TMP_Text text = inputField.textComponent;

        if (text == null)
            return;

        text.ForceMeshUpdate();

        int characterIndex = inputField.stringPosition;
        Vector3 caretPosition;

        if (characterIndex < text.textInfo.characterCount) {
            TMP_CharacterInfo characterInfo =
                text.textInfo.characterInfo[characterIndex];

            caretPosition = characterInfo.bottomLeft;
        }
        else if (text.textInfo.characterCount > 0) {
            TMP_CharacterInfo characterInfo =
                text.textInfo.characterInfo[text.textInfo.characterCount - 1];

            caretPosition = characterInfo.topRight;
        }
        else        
            caretPosition = Vector3.zero;        

        caret.localPosition = caretPosition + (Vector3.up * verticalOffset) + (Vector3.right * horizontalOffset);
    }

    private void UpdateBlink() {
        blinkTimer += Time.unscaledDeltaTime;

        if (blinkTimer >= blinkSpeed) {
            blinkTimer = 0f;
            caretVisible = !caretVisible;

            caret.gameObject.SetActive(caretVisible);
            caret.transform.localRotation = Quaternion.Euler(0,0,Random.Range(-5.5f, 5.5f));
        }
    }

    private void OnSelect(string text) {
        blinkTimer = 0f;
        caretVisible = true;
    }

    private void OnDeselect(string text) {
        caret.gameObject.SetActive(false);
    }

    private void Update() {
        if (!inputField.isFocused) {
            caret.gameObject.SetActive(false);
            return;
        }

        UpdateCaretPosition();
        UpdateBlink();
    }
}
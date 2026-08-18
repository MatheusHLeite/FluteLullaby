using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DelightStudio.UI {
    public class UI_Drawing : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {
        [Header("Setup")]
        [SerializeField] private Color penColor = Color.black;
        [SerializeField] private int brushSize = 10;

        [Header("Notes")]
        [SerializeField] private TMP_InputField if_notes;

        private Color backgroundColor;

        private RawImage rawImage;
        private Texture2D texture;

        private int textureWidth;
        private int textureHeight;

        private Vector2Int previousPixel;
        private bool isDrawing;

        private bool eraserEnabled = false;

        private bool textureDirty;
        private bool screenEnabled;
        private bool loaded;

        private bool isOnMainScreen;
        private bool isEdited;
        NotesSaveData lastData;

        #region Initialization
        private void Awake() {
            Singleton.Instance.GameEvents.OnGamePaused.AddListener(OnGamePaused);
            Singleton.Instance.GameEvents.OnGameResumed.AddListener(OnGameResumed);
            Singleton.Instance.GameEvents.OnScreenSwitch.AddListener(SaveData);

            if_notes.onValueChanged.AddListener(OnNotesEdited);
            if_notes.onSubmit.AddListener(OnNotesEdited);
            if_notes.onEndEdit.AddListener(OnNotesEdited);
            if_notes.onDeselect.AddListener(OnNotesEdited);

            rawImage = GetComponent<RawImage>();

            Color bgColor = Color.white;
            bgColor.a = 0f;
            backgroundColor = bgColor;
        }

        private void OnDestroy() {
            if_notes.onValueChanged.RemoveListener(OnNotesEdited);
            if_notes.onSubmit.RemoveListener(OnNotesEdited);
            if_notes.onEndEdit.RemoveListener(OnNotesEdited);
            if_notes.onDeselect.RemoveListener(OnNotesEdited);

            Singleton.Instance.GameEvents.OnGamePaused.RemoveListener(OnGamePaused);
            Singleton.Instance.GameEvents.OnGameResumed.RemoveListener(OnGameResumed);
            Singleton.Instance.GameEvents.OnScreenSwitch.RemoveListener(SaveData);
        }

        private void Start() {
            CheckDataLoad();
        }

        private void CheckDataLoad() {
            NotesSaveData data = Singleton.Instance.SaveManager.PlayerData.notesData;

            if (LoadData(data)) 
                return;

            ResetCanvas();
        }
        #endregion

        #region Texture
        private void ResetCanvas() {
            textureWidth = rawImage.texture.width;
            textureHeight = rawImage.texture.height;

            texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
  
            rawImage.texture = texture;

            Color[] pixels = new Color[textureWidth * textureHeight];

            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = backgroundColor;

            texture.SetPixels(pixels);
            texture.Apply(false);

            if_notes.text = string.Empty;
            lastData = new NotesSaveData {
                notesData = string.Empty,
                textureData = GetSaveData()
            };

            loaded = true;
        }
        #endregion

        #region Data management
        public bool LoadData(NotesSaveData data) {
            if (data == null) 
                return false;

            if_notes.text = data.notesData;

            if (string.IsNullOrEmpty(data.textureData))
                return false;

            byte[] pngData;

            try {
                pngData = Convert.FromBase64String(data.textureData);
            }
            catch (FormatException) {
                Debug.LogError("Dados da anotação inválidos.");
                return false;
            }

            Texture2D loadedTexture = new Texture2D(2, 2, TextureFormat.RGBA32,false);

            if (!loadedTexture.LoadImage(pngData)) {
                Debug.LogError("Não foi possível carregar a anotação.");
                Destroy(loadedTexture);
                return false;
            }

            if (texture != null)
                Destroy(texture);

            texture = loadedTexture;

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            rawImage.texture = texture;

            textureWidth = texture.width;
            textureHeight = texture.height;
            
            lastData = data;

            textureDirty = false;
            loaded = true;
            return true;
        }

        private void SaveData() {
            if (!loaded) 
                return;

            if (lastData.textureData == GetSaveData() ||
                lastData.notesData == if_notes.text || 
                !isEdited) return;
            
            NotesSaveData notesSaveData = new() {
                textureData = GetSaveData(),
                notesData = if_notes.text
            };
            lastData = notesSaveData;

            Singleton.Instance.GameEvents.OnNoteDataSaved?.Invoke(notesSaveData);
        }

        public string GetSaveData() {
            if (texture == null)
                return null;
     
            if (textureDirty) {
                texture.Apply(false);
                textureDirty = false;
            }

            byte[] pngData = texture.EncodeToPNG();

            return Convert.ToBase64String(pngData);
        }
        #endregion

        #region Input
        private void CheckEraser() {
            /*if (eventData.button == PointerEventData.InputButton.Left && eraserEnabled)
                eraserEnabled = false;
            if (eventData.button == PointerEventData.InputButton.Right && !eraserEnabled)
                eraserEnabled = true;*/

            if (Input.GetMouseButton(0) && eraserEnabled)
                eraserEnabled = false;
            if (Input.GetMouseButton(1) && !eraserEnabled)
                eraserEnabled = true;
        }

        public void OnPointerDown(PointerEventData eventData) {
            if (!TryGetPixelPosition(eventData, out Vector2Int pixel))
                return;

            isDrawing = true;
            previousPixel = pixel;
            
            DrawCircle(pixel.x, pixel.y);

            textureDirty = true;
            OnNotesEdited();
        }

        public void OnDrag(PointerEventData eventData) {
            if (!isDrawing)
                return;

            if (!TryGetPixelPosition(eventData, out Vector2Int pixel))
                return;

            if (pixel == previousPixel)
                return;

            DrawLine(previousPixel, pixel);
            previousPixel = pixel;
            textureDirty = true;
        }


        public void OnPointerUp(PointerEventData eventData) {
            isDrawing = false;
        }
        #endregion

        #region Helpers
        private bool TryGetPixelPosition(PointerEventData eventData, out Vector2Int pixel) {
            pixel = default;

            if (rawImage == null || texture == null)
                return false;

            RectTransform rectTransform = rawImage.rectTransform;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint)) {
                return false;
            }

            Rect rect = rectTransform.rect;

            float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
            float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

            if (normalizedX < 0f || normalizedX > 1f ||
                normalizedY < 0f || normalizedY > 1f) {
                return false;
            }

            int x = Mathf.RoundToInt(normalizedX * (textureWidth - 1));
            int y = Mathf.RoundToInt(normalizedY * (textureHeight - 1));

            pixel = new Vector2Int(x, y);
            return true;
        }
        #endregion

        #region Drawing
        private void DrawLine(Vector2Int from, Vector2Int to) {
            int dx = Mathf.Abs(to.x - from.x);
            int dy = Mathf.Abs(to.y - from.y);

            int steps = Mathf.Max(dx, dy);

            if (steps == 0) {
                DrawCircle(from.x, from.y);
                return;
            }

            for (int i = 0; i <= steps; i++) {
                float t = i / (float)steps;

                int x = Mathf.RoundToInt(Mathf.Lerp(from.x, to.x, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(from.y, to.y, t));

                DrawCircle(x, y);
            }
        }

        private void DrawCircle(int centerX, int centerY) {
            CheckEraser();

            int radius = eraserEnabled ? brushSize * 2 : brushSize / 2;
            int radiusSquared = radius * radius;

            for (int y = -radius; y <= radius; y++) {
                for (int x = -radius; x <= radius; x++) {
                    int distanceSquared = x * x + y * y;

                    if (distanceSquared > radiusSquared)
                        continue;

                    int pixelX = centerX + x;
                    int pixelY = centerY + y;

                    if (pixelX < 0 || pixelX >= textureWidth ||
                        pixelY < 0 || pixelY >= textureHeight)
                        continue;

                    if (eraserEnabled) {
                        texture.SetPixel(pixelX, pixelY, backgroundColor);
                        continue;
                    }

                    float noise = Mathf.PerlinNoise(pixelX * 0.08f, pixelY * 0.08f);

                    float strength = Mathf.Lerp(0.25f, 1.0f, noise);
                    Color color = Color.Lerp(backgroundColor, penColor, strength);

                    texture.SetPixel(pixelX, pixelY, color);
                }
            }
        }
        #endregion

        public void OnNotesEnabled(bool enabled) {
            isOnMainScreen = enabled;
        }

        private void OnNotesEdited(string n = default) {
            isEdited = true;
        }

        private void OnGamePaused() {
            eraserEnabled = false;
            screenEnabled = true;
        }

        private void OnGameResumed() { 
            screenEnabled = false;
            SaveData();
        }

        private void Update() {
            if (!screenEnabled) return;

            if (isOnMainScreen && !if_notes.isFocused) {
                EventSystem.current.SetSelectedGameObject(if_notes.gameObject);
                if_notes.ActivateInputField();

                int endPosition = if_notes.text.Length;

                if_notes.stringPosition = endPosition;
                if_notes.selectionStringAnchorPosition = endPosition;
                if_notes.selectionStringFocusPosition = endPosition;
            }

            if (textureDirty) {
                texture.Apply(false);
                textureDirty = false;
            }
        }
    }
}
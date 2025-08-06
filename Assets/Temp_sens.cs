using System.Linq.Expressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Temp_sens : MonoBehaviour
{
    public TMP_Text txtValue;
    public Slider sensSlider;
    public GameObject go;
    public GameObject inventoryScreen;
    public UnityEvent OnWindowClose;

    private bool windowOpened;
    private GameObject actualScreen;

    private void Awake() {
        sensSlider.onValueChanged.AddListener(OnSensitivityChange);
    }

    private void OnDestroy() {
        sensSlider.onValueChanged.RemoveListener(OnSensitivityChange);
    }

    private void Start() {
        sensSlider.minValue = 0.01f;
        sensSlider.maxValue = 18f;

        txtValue.text = 2.ToString();
    }

    private void OnSensitivityChange(float sensitivity) {
        txtValue.text = sensitivity.ToString("0.00");
        Singleton.Instance.GameEvents.OnSensitivityChange?.Invoke(sensitivity);
    }

    private void OpenScreen(GameObject screen) {
        //OnWindowOpen?.Invoke();

        windowOpened = true;
        actualScreen = screen;

        screen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseScreen(GameObject screen) {
        OnWindowClose?.Invoke();

        windowOpened = false;

        screen.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ChangeScreen(GameObject previousScreen, GameObject newScreen) {

    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) { //[TODO] Remove
            if (!windowOpened)            
                OpenScreen(go);            
            else if (windowOpened && actualScreen == go)
                CloseScreen(go);            
        }

        if (Input.GetKeyDown(KeyCode.Tab)) { //[TODO] Remove
            if (!windowOpened)
                OpenScreen(inventoryScreen);
            else if (windowOpened && actualScreen == inventoryScreen)
                CloseScreen(inventoryScreen);
        }
    }
}

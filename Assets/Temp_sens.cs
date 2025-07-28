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
    public UnityEvent OnWindowClose;

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

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) { //[TODO] Remove
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                go.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                OnWindowClose?.Invoke();

                go.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}

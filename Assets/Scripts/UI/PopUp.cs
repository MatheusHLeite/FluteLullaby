using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopUp : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private GameObject popUp;
    [Space(10)]
    [SerializeField] private GameObject titleHolder;
    [SerializeField] private TMP_Text txt_title;
    [SerializeField] private TMP_Text txt_body;
    [SerializeField] private GameObject countdownHolder;
    [SerializeField] private Button btn_accept;
    [SerializeField] private Button btn_cancel;
    
    private TMP_Text txt_accept;
    private TMP_Text txt_cancel;

    private CanvasGroup canvasGroup;

    public bool IsOpened { get; private set; }

    private void Awake() {
        canvasGroup = GetComponent<CanvasGroup>();
        txt_accept = btn_accept.GetComponentInChildren<TMP_Text>();
        txt_cancel = btn_cancel.GetComponentInChildren<TMP_Text>();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        popUp.SetActive(false);
    }

    public void Setup(string titleMessage, string bodyMessage, string acceptButtonText, string cancelButtonText, 
        UnityAction acceptAction, UnityAction cancelAction, bool shouldClosePopUp = true, bool hasCountdown = false) {
        titleHolder.gameObject.SetActive(titleMessage != string.Empty);

        txt_title.SetText(titleMessage);
        txt_body.SetText(bodyMessage);

        btn_accept.gameObject.SetActive(!string.IsNullOrEmpty(acceptButtonText));
        btn_cancel.gameObject.SetActive(!string.IsNullOrEmpty(cancelButtonText));

        txt_accept.SetText(acceptButtonText);
        txt_cancel.SetText(cancelButtonText);

        countdownHolder.SetActive(hasCountdown);

        if (acceptAction != null) {
            btn_accept.onClick.RemoveAllListeners();            
            btn_accept.onClick.AddListener(acceptAction);
        }

        if (cancelAction != null) {
            btn_cancel.onClick.RemoveAllListeners();            
            btn_cancel.onClick.AddListener(cancelAction);
        }

        if (shouldClosePopUp) {
            btn_accept.onClick.AddListener(ClosePopUp);
            btn_cancel.onClick.AddListener(ClosePopUp);
        }
    }

    public void OpenPopUp() {
        canvasGroup.DOKill();

        canvasGroup.alpha = 0;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        canvasGroup.DOFade(1, 0.2f);
        popUp.SetActive(true);

        IsOpened = true;
    }

    public void ClosePopUp() {
        if (!IsOpened) return;

        canvasGroup.DOKill();

        canvasGroup.alpha = 1;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        IsOpened = false;

        canvasGroup.DOFade(0, 0.2f).OnComplete(() => {
            popUp.SetActive(false);            
        });
    }
}

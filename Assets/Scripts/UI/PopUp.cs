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

    public void Setup(string titleMessage, string bodyMessage, string acceptButtonText, string cancelButtonText, UnityAction acceptAction, UnityAction cancelAction) {
        titleHolder.gameObject.SetActive(titleMessage != string.Empty);

        txt_title.SetText(titleMessage);
        txt_body.SetText(bodyMessage);

        btn_accept.gameObject.SetActive(acceptAction != null);
        btn_cancel.gameObject.SetActive(cancelAction != null);
     
        if (acceptAction != null) {
            txt_accept.SetText(acceptButtonText);

            btn_accept.onClick.RemoveAllListeners();            
            btn_accept.onClick.AddListener(acceptAction);
        }

        if (cancelAction != null) {
            txt_cancel.SetText(cancelButtonText);

            btn_cancel.onClick.RemoveAllListeners();            
            btn_cancel.onClick.AddListener(cancelAction);
        }
    }

    public void OpenPopUp() {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        canvasGroup.DOFade(1, 0.2f);
        popUp.SetActive(true);

        IsOpened = true;
    }

    public void ClosePopUp() {
        if (!IsOpened) return;

        canvasGroup.alpha = 1;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        IsOpened = false;

        canvasGroup.DOFade(0, 0.2f).OnComplete(() => {
            popUp.SetActive(false);            
        });
    }
}

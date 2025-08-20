using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_SettingsTabHandler : MonoBehaviour {
    private InputHandler Input;

    [Header("Tabs")]
    [SerializeField] private Button[] m_tabButton;
    [SerializeField] private GameObject[] m_tabs;

    private TMP_Text[] buttonTexts;

    private int selectedTabIndex;
    private int previousSelectedTabIndex;
    private bool screenOpened;

    private Color unselectedButtonColor = new Color(.5f, .5f, .5f);     //808080
    private Color selectedButtonColor = new Color(1, .55f, .2f);        //FF8C33

    private void Awake() {
        Input = Singleton.Instance.InputHandler;

        buttonTexts = new TMP_Text[m_tabButton.Length];

        for (int i = 0; i < m_tabButton.Length; i++) {
            int index = i;

            m_tabButton[i].onClick.RemoveAllListeners();
            m_tabButton[i].onClick.AddListener(() => SelectSpecificTab(index));

            buttonTexts[i] = m_tabButton[i].GetComponentInChildren<TMP_Text>();
        }
    }

    private void Start() {
        for (int i = 0; i < m_tabs.Length; i++)        
            m_tabs[i].SetActive(false);        

        m_tabs[0].SetActive(true);
    }

    public void OnSettingsScreensOpened() {
        screenOpened = true;

        SelectSpecificTab(0);
    }

    public void OnSettingsScreensClosed() {
        screenOpened = false;

        //Save settings; or ask the player if he want to revert the changes
    }

    private void SelectNextTab() {
        previousSelectedTabIndex = selectedTabIndex;
        selectedTabIndex++;
        if (selectedTabIndex > m_tabs.Length - 1) selectedTabIndex = 0;

        SelectTab();
    }

    private void SelectPreviousTab() {
        previousSelectedTabIndex = selectedTabIndex;
        selectedTabIndex--;
        if (selectedTabIndex < 0) selectedTabIndex = m_tabs.Length - 1;

        SelectTab();
    }

    private void SelectTab() {
        m_tabs[previousSelectedTabIndex].SetActive(false);
        m_tabs[selectedTabIndex].SetActive(true);

        buttonTexts[previousSelectedTabIndex].color = unselectedButtonColor;
        buttonTexts[selectedTabIndex].color = selectedButtonColor;
    }

    private void SelectSpecificTab(int index) {
        previousSelectedTabIndex = selectedTabIndex;
        selectedTabIndex = index;

        SelectTab();
    }

    private void Update() {
        if (!screenOpened) return;

        if (Input.NextTab) SelectNextTab();
        if (Input.PreviousTab) SelectPreviousTab();
    }
}

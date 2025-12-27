using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class RadialMenuUI : MonoBehaviour {
    [Header("Setup")]
    [SerializeField] private RadialMenuButton m_optionPrefab;
    [SerializeField] private TMP_Text m_timer;

    [Header("Decision made UI")]
    [SerializeField] private CanvasGroup m_decisionMadeCanvasGroup;
    [SerializeField] private TMP_Text m_decisionMadeText;

    [Header("Values")]
    [SerializeField] [Range(0.1f, 10f)] private float m_radiusPercent = 3.2f;
    [SerializeField] [Range(0.1f, 10f)] private float m_optionSizePercent = 3.5f;

    private float startAngle;

    private void Start()
    {
        Invoke(nameof(TestUI), 3f);
    }

    private void Awake() {
        Singleton.Instance.GameEvents.OnImportantDecisionTaken.AddListener(OnImportantDecisionTaken);
    }

    private void OnDestroy() {
        Singleton.Instance.GameEvents.OnImportantDecisionTaken.RemoveListener(OnImportantDecisionTaken);
    }

    private void OnImportantDecisionTaken(ImportantDecision decision) {
        
    }

    private void TestUI()
    {
        ImportantDecision[] newDecisions = new ImportantDecision[4];

        newDecisions[0].optionText = "Shoot";
        newDecisions[1].optionText = "Run";
        newDecisions[2].optionText = "Run towards";
        newDecisions[3].optionText = "Hide";

        SetupOptions(newDecisions, 60);
    }

    private void SetupOptions(ImportantDecision[] options, int timer) {
        for (int i = 0; i < options.Length; i++) {
            RadialMenuButton newButton = Instantiate(m_optionPrefab, transform);
            newButton.SetupUI(options[i]);
        }

        UpdateLayout();
        StartCoroutine(StartTimer(timer));
    }

    private void UpdateLayout() {
        int optionCount = transform.childCount;
        if (optionCount == 0) return;

        RectTransform rt = transform as RectTransform;
        float minSize = Mathf.Min(rt.rect.width, rt.rect.height);

        float radius = minSize * m_radiusPercent;
        float optionSize = minSize * m_optionSizePercent;

        float angleStep = 360f / optionCount;
        startAngle = GetStartAngle(optionCount);

        for (int i = 0; i < optionCount; i++) {
            RectTransform option = transform.GetChild(i) as RectTransform;
            CanvasGroup cv = option.GetComponent<CanvasGroup>();
            if (option == null) continue;

            float angle = startAngle + angleStep * i;
            float rad = angle * Mathf.Deg2Rad;

            Vector2 position = new Vector2(
                Mathf.Cos(rad),
                Mathf.Sin(rad)
            ) * radius;

            Vector2 dirToCenter = -position;
            float zRot = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg - 180f;
            Quaternion rotation = Quaternion.Euler(0f, 0f, zRot);

            option.anchorMin = option.anchorMax = new Vector2(0.5f, 0.5f);
            option.sizeDelta = new Vector2(optionSize, optionSize);

            option.localRotation = rotation;
            option.anchoredPosition = -dirToCenter * 2.75f;
            cv.alpha = 0;

            cv.DOFade(1, 1.5f).SetDelay(0.25f);
            option.DOAnchorPos(position, 1f).SetEase(Ease.OutCirc);
        }
    }

    private int GetStartAngle(int count) {
        return count switch {
            2 => 180,
            3 => 210,
            5 => 162,
            _ => 180
        };
    }

    private IEnumerator StartTimer(int initialTimer) {
        float time = initialTimer;
        int lastTimeRecorded = -1;

        while (time > 0) {
            time -= Time.deltaTime;
            int timeRound = Mathf.RoundToInt(time);

            if (timeRound != lastTimeRecorded) {
                lastTimeRecorded = timeRound;

                m_timer.text = timeRound.ToString();
                m_timer.color = timeRound <= 10 ? Color.red : Color.white;
                m_timer.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f);
            }
            yield return null;
        }
    }
}

public struct ImportantDecision {
    public string optionText;
}
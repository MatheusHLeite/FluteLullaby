using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class Countdown : MonoBehaviour {
    private TMP_Text _text;
    private Coroutine _timeCoroutine;

    private int lastTime;

    private void Awake() {
        _text = GetComponent<TMP_Text>();
    }

    public void SetCountdown(float startValue, UnityAction onCountdownComplete = default) {
        if (_timeCoroutine != null)
            StopCoroutine(_timeCoroutine);

        _timeCoroutine = StartCoroutine(StartCountdown(startValue, onCountdownComplete));
    }
    
    private IEnumerator StartCountdown(float startValue, UnityAction onCountdownComplete = default) {
        float timer = startValue;
        int timerRound;

        while (timer > 0) {
            timer -= Time.deltaTime;

            timerRound = Mathf.RoundToInt(timer);
            if (lastTime != timerRound) UpdateTimerText(timerRound);
            lastTime = timerRound;

            yield return null;
        } 

        onCountdownComplete?.Invoke();
        _timeCoroutine = null;
    }

    private void UpdateTimerText(int time) => _text.text = time.ToString();
}

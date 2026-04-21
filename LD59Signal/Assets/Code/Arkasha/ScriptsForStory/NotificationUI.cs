using System.Collections;
using UnityEngine;
using TMPro;

// Универсальный UI для всплывающих уведомлений.
// Вызывай NotificationUI.Instance.Show("текст") из любого скрипта.
public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text    _messageText;

    [Header("Настройки")]
    [SerializeField] private float _fadeInDuration  = 0.4f;
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    private Coroutine _currentRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Скрываем сразу при старте
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    // Показывает уведомление с текстом. Если уведомление уже идёт — прерывает и показывает новое.
    public void Show(string message)
    {
        if (_currentRoutine != null)
            StopCoroutine(_currentRoutine);

        _currentRoutine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        _messageText.text = message;

        // Fade in
        yield return StartCoroutine(Fade(0f, 1f, _fadeInDuration));

        // Держим
        yield return new WaitForSeconds(_displayDuration);

        // Fade out
        yield return StartCoroutine(Fade(1f, 0f, _fadeOutDuration));

        _currentRoutine = null;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float timer = 0f;
        _canvasGroup.alpha = from;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }

        _canvasGroup.alpha = to;
    }
}

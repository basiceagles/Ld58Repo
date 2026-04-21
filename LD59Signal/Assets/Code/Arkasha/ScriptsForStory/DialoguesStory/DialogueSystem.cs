using System;
using System.Collections;
using UnityEngine;
using TMPro;

// Система монолога. Синглтон.
// Показывает панель с именем говорящего и текстом с эффектом печатной машинки.
// ЛКМ — следующая реплика. При переключении до конца озвучки — стоп старый клип, старт новый.
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI панель")]
    [SerializeField] private GameObject  _panelRoot;       // корневой объект панели
    [SerializeField] private CanvasGroup _canvasGroup;     // для fade in/out панели
    [SerializeField] private TMP_Text    _speakerText;     // имя говорящего
    [SerializeField] private TMP_Text    _lineText;        // текст реплики

    [Header("Настройки печати")]
    [SerializeField] private float _typeSpeed     = 40f;   // символов в секунду
    [SerializeField] private float _fadeInSpeed   = 0.3f;  // секунд на появление панели

    [Header("Аудио")]
    [SerializeField] private AudioSource _voiceSource;     // источник для озвучки реплик

    // Вызывается когда весь диалог завершён
    public event Action OnDialogueFinished;

    private DialogueData _currentData;
    private int          _currentLineIndex;
    private bool         _isOpen;
    private bool         _isTyping;

    private Coroutine _typeRoutine;
    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen) return;

        // ЛКМ — следующая реплика или ускорение печати
        if (Input.GetMouseButtonDown(0))
            HandleClick();
    }

    // Запускает диалог. Вызывай из любого места: DialogueSystem.Instance.Play(myDialogueData)
    public void Play(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueSystem] DialogueData пустой или не назначен.");
            return;
        }

        _currentData      = data;
        _currentLineIndex = 0;
        _isOpen           = true;

        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        // Плавно показываем панель
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadePanel(0f, 1f, _fadeInSpeed));

        ShowLine(_currentLineIndex);
    }

    // Клик: если текст ещё печатается — показываем его целиком мгновенно.
    // Если текст уже допечатан — переходим к следующей реплике.
    private void HandleClick()
    {
        if (_isTyping)
        {
            SkipTyping();
        }
        else
        {
            NextLine();
        }
    }

    // Показывает реплику по индексу
    private void ShowLine(int index)
    {
        DialogueData.Line line = _currentData.lines[index];

        // Имя говорящего
        if (_speakerText != null)
            _speakerText.text = line.speakerName;

        // Останавливаем предыдущую озвучку и запускаем новую
        if (_voiceSource != null)
        {
            _voiceSource.Stop();

            if (line.voiceClip != null)
                _voiceSource.PlayOneShot(line.voiceClip);
        }

        // Запускаем печать текста
        if (_typeRoutine != null) StopCoroutine(_typeRoutine);
        _typeRoutine = StartCoroutine(TypeLine(line.text));
    }

    // Эффект печатной машинки
    private IEnumerator TypeLine(string fullText)
    {
        _isTyping = true;
        _lineText.text = string.Empty;

        float interval = 1f / _typeSpeed;

        for (int i = 0; i < fullText.Length; i++)
        {
            _lineText.text += fullText[i];
            yield return new WaitForSeconds(interval);
        }

        _isTyping    = false;
        _typeRoutine = null;
    }

    // Мгновенно показывает весь текст текущей реплики
    private void SkipTyping()
    {
        if (_typeRoutine != null)
        {
            StopCoroutine(_typeRoutine);
            _typeRoutine = null;
        }

        _isTyping      = false;
        _lineText.text = _currentData.lines[_currentLineIndex].text;
    }

    // Переход к следующей реплике или закрытие диалога
    private void NextLine()
    {
        _currentLineIndex++;

        if (_currentLineIndex >= _currentData.lines.Length)
        {
            CloseDialogue();
            return;
        }

        ShowLine(_currentLineIndex);
    }

    // Закрывает панель диалога и вызывает событие завершения
    private void CloseDialogue()
    {
        _isOpen = false;

        if (_voiceSource != null)
            _voiceSource.Stop();

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadePanel(1f, 0f, _fadeInSpeed, () =>
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }));

        OnDialogueFinished?.Invoke();
    }

    // Плавное изменение прозрачности панели. onComplete — опциональный колбэк после завершения.
    private IEnumerator FadePanel(float from, float to, float duration, Action onComplete = null)
    {
        if (_canvasGroup == null) yield break;

        float timer = 0f;
        _canvasGroup.alpha = from;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(from, to, timer / duration);
            yield return null;
        }

        _canvasGroup.alpha = to;
        onComplete?.Invoke();
        _fadeRoutine = null;
    }
}

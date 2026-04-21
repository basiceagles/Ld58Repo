using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Экран появляется автоматически после расшифровки всех 5 фрагментов.
// Показывает текст сигнала и две кнопки выбора.
public class SignalRevealUI : MonoBehaviour
{
    public static SignalRevealUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject  _panelRoot;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_Text    _signalText;
    [SerializeField] private Button      _forgetButton;      // Забыть об этом
    [SerializeField] private Button      _changeSignalButton; // Изменить сигнал

    [Header("Текст сигнала")]
    [TextArea(3, 8)]
    [SerializeField] private string _signalContent; // заполни в инспекторе

    [Header("Настройки")]
    [SerializeField] private float _fadeInDuration  = 0.5f;
    [SerializeField] private float _fadeOutDuration = 0.3f;

    [Header("Блокировка игрока")]
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private CameraController _cameraController;

    private bool      _isOpen;
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

        // Вешаем обработчики на кнопки
        if (_forgetButton != null)
            _forgetButton.onClick.AddListener(OnForgetClicked);

        if (_changeSignalButton != null)
            _changeSignalButton.onClick.AddListener(OnChangeSignalClicked);
    }

    private void OnDestroy()
    {
        if (_forgetButton != null)
            _forgetButton.onClick.RemoveListener(OnForgetClicked);

        if (_changeSignalButton != null)
            _changeSignalButton.onClick.RemoveListener(OnChangeSignalClicked);
    }

    private void Start()
{
    if (PlayerProgressManager.Instance != null)
        PlayerProgressManager.Instance.OnAllFragmentsDeciphered += OnAllDeciphered;
    else
        Debug.LogError("[SignalRevealUI] PlayerProgressManager не найден!", this);
}

// Ждём пока DecipherScreen закроется и только потом открываемся
private void OnAllDeciphered()
{
    StartCoroutine(OpenAfterDecipherScreen());
}

private System.Collections.IEnumerator OpenAfterDecipherScreen()
{
    // Ждём пока DecipherScreen закроет экран и вернёт управление
    yield return new WaitForSeconds(3.5f);
    Open();
}

    private void OnDisable()
{
    if (PlayerProgressManager.Instance != null)
        PlayerProgressManager.Instance.OnAllFragmentsDeciphered -= OnAllDeciphered;
}

    // Открывается автоматически когда расшифрованы все фрагменты
    private void Open()
{
    Debug.Log("[SignalRevealUI] Open вызван");
    if (_isOpen) return;

        _isOpen = true;

        if (_signalText != null)
            _signalText.text = _signalContent;

        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        SetPlayerControl(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Fade(0f, 1f, _fadeInDuration));

        PlayerProgressManager.Instance?.ObtainSignalText();
    }

    // Игрок выбрал "Забыть об этом"
    private void OnForgetClicked()
    {
        Close();
    }

    // Игрок выбрал "Изменить сигнал"
    private void OnChangeSignalClicked()
    {
        PlayerProgressManager.Instance?.RegisterSignalChanged();

        if (NotificationUI.Instance != null)
            NotificationUI.Instance.Show("Signal contents have been altered");

        Close();
    }

    private void Close()
    {
        _isOpen = false;

        SetPlayerControl(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(Fade(1f, 0f, _fadeOutDuration, () =>
        {
            if (_panelRoot != null)
                _panelRoot.SetActive(false);
        }));
    }

    private IEnumerator Fade(float from, float to, float duration, System.Action onComplete = null)
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

    private void SetPlayerControl(bool enabled)
    {
        if (_playerController != null) _playerController.enabled = enabled;
        if (_cameraController != null) _cameraController.enabled = enabled;
    }
}
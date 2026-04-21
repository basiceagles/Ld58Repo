using System;
using UnityEngine;

public class DecipherTerminal : MonoBehaviour
{
    [Header("Взаимодействие")]
    [SerializeField] private float      _interactionRadius = 3f;
    [SerializeField] private Transform  _playerTransform;
    [SerializeField] private GameObject _interactPrompt;

    [Header("Экран")]
    [SerializeField] private DecipherScreen _decipherScreen;

    [Header("Фрагменты")]
    [SerializeField] private DecipherFragmentConfig[] _fragments;

    public event Action OnAllFragmentsDeciphered;

    public int TotalFragments => _fragments != null ? _fragments.Length : 0;

    public int FragmentsDeciphered => PlayerProgressManager.Instance != null
        ? PlayerProgressManager.Instance.GlobalFragmentsDeciphered
        : 0;

    private bool  _isOpen;
private bool  _skipI;
private bool  _wasDeciphered; // эта антенна уже была расшифрована
private float _sqrInteractionRadius;

    private void Awake()
    {
        _sqrInteractionRadius = _interactionRadius * _interactionRadius;

        if (_playerTransform == null)
            _playerTransform = GameObject.FindWithTag("Player")?.transform;

        if (_decipherScreen == null)
            _decipherScreen = FindObjectOfType<DecipherScreen>();

        if (_decipherScreen == null)
            Debug.LogError("[DecipherTerminal] DecipherScreen не найден!", this);

        if (_playerTransform == null)
            Debug.LogError("[DecipherTerminal] Player Transform не найден!", this);

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
    }

    private void Update()
    {
        if (_isOpen || _playerTransform == null) return;

        bool inRange = IsPlayerInRange();
        bool canOpen = CanOpenDecipher();

        if (_interactPrompt != null)
            _interactPrompt.SetActive(inRange && canOpen);

        if (_skipI)
        {
            if (Input.GetKeyUp(KeyCode.I))
                _skipI = false;
            return;
        }

        if (inRange && canOpen && Input.GetKeyDown(KeyCode.I))
    TryEnter();
    }

    // Проверяет условия: бумажка + сигнал подключён + не все расшифрованы
   private bool CanOpenDecipher()
{
    var progress = PlayerProgressManager.Instance;
    if (progress == null) return false;

    // Единственное условие — бумажка с кодами
    if (!progress.HasDecipherKey) return false;

    // Эта антенна уже была расшифрована
    if (_wasDeciphered) return false;

    // Все фрагменты уже расшифрованы глобально
    if (progress.AllFragmentsDeciphered) return false;

    return true;
}

    public void TryEnter()
    {
        if (_isOpen) return;

        if (_decipherScreen == null)
        {
            Debug.LogError("[DecipherTerminal] DecipherScreen не назначен!");
            return;
        }

        var progress = PlayerProgressManager.Instance;
        if (progress == null) return;

        if (progress.AllFragmentsDeciphered) return;

        int fragmentIndex = progress.GlobalFragmentsDeciphered;
        if (fragmentIndex >= TotalFragments) return;

        _isOpen = true;
        _skipI  = true;

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);

        _decipherScreen.Open(this, _fragments[fragmentIndex]);
    }

    // Вызывается экраном когда игрок вышел
    public void NotifyExit()
    {
        _isOpen = false;
    }

    // Вызывается экраном после успешной расшифровки фрагмента
   public bool TryGetNextFragment(out DecipherFragmentConfig nextConfig)
{
    // Помечаем эту антенну как расшифрованную
    _wasDeciphered = true;

    PlayerProgressManager.Instance?.RegisterFragmentDeciphered();

    nextConfig = default;
    OnAllFragmentsDeciphered?.Invoke();
    return false;
}

    private bool IsPlayerInRange()
    {
        return (_playerTransform.position - transform.position).sqrMagnitude
               <= _sqrInteractionRadius;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _interactionRadius);
    }
    #endif
}
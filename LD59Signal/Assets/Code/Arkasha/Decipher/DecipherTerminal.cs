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

    // Срабатывает когда все фрагменты расшифрованы
    public event Action OnAllFragmentsDeciphered;

    public int FragmentsDeciphered { get; private set; } = 0;
    public int TotalFragments => _fragments != null ? _fragments.Length : 0;

    private bool  _isOpen;
    private float _sqrInteractionRadius;

    private void Awake()
    {
        _sqrInteractionRadius = _interactionRadius * _interactionRadius;

        if (_decipherScreen == null)
            Debug.LogError($"[DecipherTerminal] DecipherScreen не назначен!", this);

        if (_playerTransform == null)
            Debug.LogError($"[DecipherTerminal] Player Transform не назначен!", this);

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
    }

   private void Update()
{
    if (_isOpen || _playerTransform == null) return;

    bool inRange = IsPlayerInRange();
    bool allDone = FragmentsDeciphered >= TotalFragments;

    if (_interactPrompt != null)
        _interactPrompt.SetActive(inRange && !allDone);

    if (inRange && !allDone)
    {
        // Показываем дистанцию до терминала каждые ~секунду через лог
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log($"[DecipherTerminal] E нажата, inRange={inRange}, allDone={allDone}, фрагментов={FragmentsDeciphered}/{TotalFragments}");
            TryEnter();
        }
    }
}

   public void TryEnter()
{
    if (_isOpen)
    {
        Debug.Log("[DecipherTerminal] TryEnter — экран уже открыт");
        return;
    }
    if (_decipherScreen == null)
    {
        Debug.LogError("[DecipherTerminal] TryEnter — DecipherScreen не назначен!");
        return;
    }
    if (FragmentsDeciphered >= TotalFragments)
    {
        Debug.Log("[DecipherTerminal] TryEnter — все фрагменты уже расшифрованы");
        return;
    }

    Debug.Log($"[DecipherTerminal] Открываем экран, фрагмент {FragmentsDeciphered + 1}");

    _isOpen = true;

    if (_interactPrompt != null)
        _interactPrompt.SetActive(false);

    _decipherScreen.Open(this, _fragments[FragmentsDeciphered]);
}

    // Вызывается экраном когда игрок нажал выход
    public void NotifyExit()
    {
        _isOpen = false;
    }

    // Вызывается экраном после успешной расшифровки фрагмента
    // Возвращает true если есть ещё фрагменты
    public bool TryGetNextFragment(out DecipherFragmentConfig nextConfig)
    {
        FragmentsDeciphered++;

        if (FragmentsDeciphered < TotalFragments)
        {
            nextConfig = _fragments[FragmentsDeciphered];
            return true;
        }

        nextConfig = default;
        OnAllFragmentsDeciphered?.Invoke();
        return false;
    }

    // sqrMagnitude вместо magnitude — не считаем лишний квадратный корень
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
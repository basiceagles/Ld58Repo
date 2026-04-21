using UnityEngine;

// Бумажка с кодами. Лежит у факса после второго звонка.
// Игрок подходит, жмёт E — предмет исчезает, выдаётся статус HasDecipherKey.
public class DecipherKeyItem : MonoBehaviour
{
    [Header("Взаимодействие")]
    [SerializeField] private float     _interactionRadius = 2f;
    [SerializeField] private Transform _playerTransform;

    private float      _sqrInteractionRadius;
    private bool       _inRange = false;
    private bool       _skipE   = false;
    private GameObject _interactPrompt;

    private void Awake()
    {
        _sqrInteractionRadius = _interactionRadius * _interactionRadius;

        if (_playerTransform == null)
            _playerTransform = GameObject.FindWithTag("Player")?.transform;

        // Ищем промт на сцене по тегу InteractPrompt
        _interactPrompt = GameObject.FindWithTag("InteractPrompt");

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);
        else
            Debug.LogWarning("[DecipherKeyItem] Объект с тегом InteractPrompt не найден!", this);
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        bool inRange = IsPlayerInRange();

        if (_interactPrompt != null)
            _interactPrompt.SetActive(inRange);

        _inRange = inRange;

        if (_skipE)
        {
            if (Input.GetKeyUp(KeyCode.E))
                _skipE = false;
            return;
        }

        if (inRange && Input.GetKeyDown(KeyCode.E))
            Pickup();
    }

    private void OnDestroy()
    {
        // Скрываем промт если объект уничтожается пока игрок рядом
        if (_inRange && _interactPrompt != null)
            _interactPrompt.SetActive(false);
    }

    // Игрок подобрал бумажку
    private void Pickup()
    {
        if (PlayerProgressManager.Instance == null)
        {
            Debug.LogWarning("[DecipherKeyItem] PlayerProgressManager не найден!", this);
            return;
        }

        _skipE = true;

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);

        PlayerProgressManager.Instance.ObtainDecipherKey();

        if (NotificationUI.Instance != null)
            NotificationUI.Instance.Show("Signal decryption available — press I near an antenna");

        Destroy(gameObject);
    }

    private bool IsPlayerInRange()
    {
        return (_playerTransform.position - transform.position).sqrMagnitude
               <= _sqrInteractionRadius;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _interactionRadius);
    }
    #endif
}   
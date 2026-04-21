using System.Collections;
using UnityEngine;

// Радиостанция. Обрабатывает все три звонка:
// - Интро (командование) — доступен сразу при старте
// - Звонок 1 (Неизвестный) — в первую бурю
// - Звонок 2 (Неизвестный) — в следующую бурю после звонка 1
// Звонит (звук + мигающий свет) пока игрок не ответит.
public class RadioStation : MonoBehaviour
{
    [Header("Взаимодействие")]
    [SerializeField] private float     _interactionRadius = 2.5f;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private GameObject _interactPrompt;   // UI подсказка [E]

    [Header("Звонок")]
    [SerializeField] private AudioSource _ringingSource;   // звук звонка (loop = true)
    [SerializeField] private Light       _blinkLight;      // point light для мигания
    [SerializeField] private float       _blinkInterval = 0.4f; // интервал мигания в секундах

    [Header("Диалоги")]
    [SerializeField] private DialogueData _introDialogue;  // командование — сразу при старте
    [SerializeField] private DialogueData _call1Dialogue;  // неизвестный — буря 1
    [SerializeField] private DialogueData _call2Dialogue;  // неизвестный — буря 2

    [Header("Дверь")]
    [SerializeField] private Transform _doorTransform;
    [SerializeField] private Vector3   _doorOpenRotation;   // локальный Euler угол открытой двери
    [SerializeField] private float     _doorOpenDuration = 1.5f;

    [Header("Спавн бумажки с кодами")]
    [SerializeField] private GameObject _decipherKeyPrefab; // префаб бумажки
    [SerializeField] private Transform  _decipherKeySpawnPoint; // точка спавна у факса

    [Header("Ссылки")]
    [SerializeField] private GamePhaseManager _gamePhaseManager;

    // Внутреннее состояние звонков
    private enum CallState { Intro, Call1, Call2, AllDone }
    private CallState _currentCall = CallState.Intro;

    // Флаги
    private bool _isRinging      = false; // сейчас звонит
    private bool _introAnswered  = false; // интро уже было отвечено
    private bool _call1Answered  = false; // звонок 1 уже был отвечён
    private bool _call2Answered  = false; // звонок 2 уже был отвечён
    private bool _doorOpened     = false; // дверь уже открыта

    // Номер бури в которой был отвечен звонок 1 (чтобы звонок 2 шёл в следующую)
    private int _stormWhenCall1Answered = -1;

    private float _sqrInteractionRadius;
    private Coroutine _blinkRoutine;
    private bool _skipE = false; // защита от двойного срабатывания E

    private void Awake()
    {
        _sqrInteractionRadius = _interactionRadius * _interactionRadius;

        if (_playerTransform == null)
            _playerTransform = GameObject.FindWithTag("Player")?.transform;

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);

        if (_blinkLight != null)
            _blinkLight.enabled = false;
    }

    private void Start()
    {
        // Интро-звонок начинается сразу при старте
        StartRinging();
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        // Проверяем условия появления следующих звонков
        CheckStormCalls();

        bool inRange = IsPlayerInRange();

        // Показываем подсказку только когда звонит и игрок рядом
        if (_interactPrompt != null)
            _interactPrompt.SetActive(inRange && _isRinging);

        // Защита от двойного срабатывания E
        if (_skipE)
        {
            if (Input.GetKeyUp(KeyCode.E))
                _skipE = false;
            return;
        }

        if (inRange && _isRinging && Input.GetKeyDown(KeyCode.E))
            AnswerCall();
    }

    // Проверяет условия для запуска звонков 1 и 2
    private void CheckStormCalls()
    {
        if (_gamePhaseManager == null) return;

        int stormCount   = _gamePhaseManager.stormCount;
        bool isStorm     = _gamePhaseManager.isStormActive;

        // Звонок 1 — первая буря, интро уже отвечено, звонок 1 ещё не был
        if (_currentCall == CallState.Call1 && isStorm && stormCount >= 1 && !_isRinging)
        {
            StartRinging();
            return;
        }

        // Звонок 2 — следующая буря после ответа на звонок 1, звонок 2 ещё не был
        if (_currentCall == CallState.Call2 && isStorm
            && stormCount > _stormWhenCall1Answered
            && _stormWhenCall1Answered != -1
            && !_isRinging)
        {
            StartRinging();
        }
    }

    // Начинает звонить: звук + мигание света
    private void StartRinging()
    {
        _isRinging = true;

        if (_ringingSource != null && !_ringingSource.isPlaying)
        {
            _ringingSource.loop = true;
            _ringingSource.Play();
        }

        if (_blinkRoutine != null) StopCoroutine(_blinkRoutine);
        _blinkRoutine = StartCoroutine(BlinkRoutine());
    }

    // Останавливает звонок
    private void StopRinging()
    {
        _isRinging = false;

        if (_ringingSource != null)
            _ringingSource.Stop();

        if (_blinkRoutine != null)
        {
            StopCoroutine(_blinkRoutine);
            _blinkRoutine = null;
        }

        if (_blinkLight != null)
            _blinkLight.enabled = false;
    }

    // Мигание Point Light пока звонит
    private IEnumerator BlinkRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(_blinkInterval);

        while (_isRinging)
        {
            if (_blinkLight != null)
                _blinkLight.enabled = !_blinkLight.enabled;

            yield return wait;
        }
    }

    // Игрок нажал E — отвечаем на текущий звонок
    private void AnswerCall()
    {
        StopRinging();

        if (_interactPrompt != null)
            _interactPrompt.SetActive(false);

        _skipE = true;

        switch (_currentCall)
        {
            case CallState.Intro:
                AnswerIntro();
                break;

            case CallState.Call1:
                AnswerCall1();
                break;

            case CallState.Call2:
                AnswerCall2();
                break;
        }
    }

    // Ответ на интро — диалог командования, после него открывается дверь
    private void AnswerIntro()
    {
        _introAnswered = true;
        _currentCall   = CallState.Call1;

        if (DialogueSystem.Instance != null && _introDialogue != null)
        {
            DialogueSystem.Instance.OnDialogueFinished += OnIntroFinished;
            DialogueSystem.Instance.Play(_introDialogue);
        }
        else
        {
            OnIntroFinished();
        }
    }

    // Вызывается после завершения интро-диалога
    private void OnIntroFinished()
    {
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueFinished -= OnIntroFinished;

        if (!_doorOpened)
            StartCoroutine(OpenDoorRoutine());
    }

    // Ответ на звонок 1 — диалог Неизвестного №1
    private void AnswerCall1()
    {
        _call1Answered = true;

        if (_gamePhaseManager != null)
            _stormWhenCall1Answered = _gamePhaseManager.stormCount;

        _currentCall = CallState.Call2;

        if (DialogueSystem.Instance != null && _call1Dialogue != null)
            DialogueSystem.Instance.Play(_call1Dialogue);
    }

    // Ответ на звонок 2 — диалог Неизвестного №2, после него спавним бумажку с кодами
    private void AnswerCall2()
    {
        _call2Answered = true;
        _currentCall   = CallState.AllDone;

        if (DialogueSystem.Instance != null && _call2Dialogue != null)
        {
            DialogueSystem.Instance.OnDialogueFinished += OnCall2Finished;
            DialogueSystem.Instance.Play(_call2Dialogue);
        }
        else
        {
            OnCall2Finished();
        }
    }

    // Вызывается после завершения диалога звонка 2 — спавним бумажку
    private void OnCall2Finished()
    {
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.OnDialogueFinished -= OnCall2Finished;

        SpawnDecipherKey();
    }

    // Спавнит бумажку с кодами у факса
    private void SpawnDecipherKey()
    {
        if (_decipherKeyPrefab == null || _decipherKeySpawnPoint == null)
        {
            Debug.LogWarning("[RadioStation] Префаб бумажки или точка спавна не назначены!", this);
            return;
        }

        Instantiate(_decipherKeyPrefab, _decipherKeySpawnPoint.position, _decipherKeySpawnPoint.rotation);

        #if UNITY_EDITOR
        Debug.Log("[RadioStation] Бумажка с кодами заспавнена.");
        #endif
    }

    // Плавно открывает дверь через поворот Transform
    private IEnumerator OpenDoorRoutine()
    {
        _doorOpened = true;

        if (_doorTransform == null) yield break;

        Quaternion startRot  = _doorTransform.localRotation;
        Quaternion targetRot = Quaternion.Euler(_doorOpenRotation);
        float timer = 0f;

        while (timer < _doorOpenDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / _doorOpenDuration);
            _doorTransform.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        _doorTransform.localRotation = targetRot;
    }

    // Расстояние через sqrMagnitude — без лишнего sqrt
    private bool IsPlayerInRange()
    {
        return (_playerTransform.position - transform.position).sqrMagnitude
               <= _sqrInteractionRadius;
    }

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _interactionRadius);
    }
    #endif
}

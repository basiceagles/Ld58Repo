using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SmallHedge.SoundManager;

public class DecipherScreen : MonoBehaviour
{
    private const float MATCH_THRESHOLD  = 0.92f; // порог совпадения для успеха
    private const float MATCH_HOLD_TIME  = 3f;    // сколько секунд держать порог
    private const int   NOISE_POINT_COUNT = 20;   // сколько точек получают шум за раз
    private const float PLAYER_START_MULT = 0.5f; // стартовые параметры игрока = 50% от цели

    [Header("Контроллеры — отключаются пока экран открыт")]
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private CameraController _cameraController;

    [Header("Волны")]
    [SerializeField] private WaveRenderer _targetWaveRenderer;
    [SerializeField] private WaveRenderer _playerWaveRenderer;

    [Header("UI")]
    [SerializeField] private GameObject _screenRoot;
    [SerializeField] private Slider     _progressSlider;
    [SerializeField] private TMP_Text   _progressText;
    [SerializeField] private Slider     _matchScoreSlider;
    [SerializeField] private TMP_Text   _matchScoreText;
    [SerializeField] private TMP_Text   _fragmentRevealText;
    [SerializeField] private float      _revealFadeDuration = 1f;
    [SerializeField] private GameObject _phaseControlHintRoot;
    [SerializeField] private Button     _exitButton;

    private DecipherTerminal       _terminal;
    private DecipherFragmentConfig _currentConfig;

    private float _playerAmplitude;
    private float _playerFrequency;
    private float _playerPhase;

    private float   _matchTimer;
    private float   _noiseTimer;
    private float   _inversionTimer;

    private readonly float[] _noiseOffsets = new float[OscilloscopeSimulator.SAMPLE_COUNT];

    private bool      _isOpen;
    private Coroutine _successRoutine;
    private Coroutine _revealRoutine;

    private readonly WaitForSeconds _waitSuccessDelay = new WaitForSeconds(2.5f);

    private void Awake()
    {
        if (_screenRoot != null)
            _screenRoot.SetActive(false);

        if (_exitButton != null)
            _exitButton.onClick.AddListener(RequestExit);
    }

    private void OnDestroy()
    {
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(RequestExit);
    }

    private void Update()
    {
        if (!_isOpen) return;

        // Если фаза активна — E занята под неё, выходим только через Escape
        bool exitByE = Input.GetKeyDown(KeyCode.E) && !_currentConfig.hasPhaseControl;
        if (Input.GetKeyDown(KeyCode.Escape) || exitByE)
        {
            RequestExit();
            return;
        }

        HandleWaveInput();
        HandleDrift();
        HandleNoise();
        HandleInversion();
        UpdateWaveRenderers();
        EvaluateMatchScore();
    }

    public void Open(DecipherTerminal terminal, DecipherFragmentConfig config)
    {
        if (_isOpen) return;

        _terminal = terminal;

        if (_playerController != null) _playerController.enabled = false;
        if (_cameraController != null) _cameraController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (_screenRoot != null) _screenRoot.SetActive(true);

        _isOpen = true;

        LoadFragment(config);
        UpdateProgressUI();
    }

    public void LoadFragment(DecipherFragmentConfig config)
    {
        // Работаем с копией структуры — оригинал в терминале не меняется
        _currentConfig = config;

        _playerAmplitude = _currentConfig.targetAmplitude * PLAYER_START_MULT;
        _playerFrequency = _currentConfig.targetFrequency * PLAYER_START_MULT;
        _playerPhase     = 0f;

        _matchTimer     = 0f;
        _noiseTimer     = 0f;
        _inversionTimer = 0f;

        Array.Clear(_noiseOffsets, 0, _noiseOffsets.Length);

        if (_phaseControlHintRoot != null)
            _phaseControlHintRoot.SetActive(_currentConfig.hasPhaseControl);

        if (_fragmentRevealText != null)
        {
            _fragmentRevealText.text = string.Empty;
            _fragmentRevealText.gameObject.SetActive(false);
        }

        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }
    }

    private void HandleWaveInput()
    {
        float dt = Time.deltaTime;

        if (Input.GetKey(KeyCode.W)) _playerAmplitude += _currentConfig.ampAdjustSpeed * dt;
        if (Input.GetKey(KeyCode.S)) _playerAmplitude -= _currentConfig.ampAdjustSpeed * dt;
        if (Input.GetKey(KeyCode.D)) _playerFrequency += _currentConfig.freqAdjustSpeed * dt;
        if (Input.GetKey(KeyCode.A)) _playerFrequency -= _currentConfig.freqAdjustSpeed * dt;

        if (_currentConfig.hasPhaseControl)
        {
            if (Input.GetKey(KeyCode.E)) _playerPhase += _currentConfig.phaseAdjustSpeed * dt;
            if (Input.GetKey(KeyCode.Q)) _playerPhase -= _currentConfig.phaseAdjustSpeed * dt;
        }

        _playerAmplitude = Mathf.Clamp(_playerAmplitude, 0.05f, 3f);
        _playerFrequency = Mathf.Clamp(_playerFrequency, 0.5f, 25f);
        _playerPhase     = Mathf.Clamp(_playerPhase, -Mathf.PI * 4f, Mathf.PI * 4f);
    }

    private void HandleDrift()
    {
        if (!_currentConfig.hasDrift) return;
        // Цель медленно уплывает — игрок должен подстраиваться
        _currentConfig.targetPhase += _currentConfig.driftSpeed * Time.deltaTime;
    }

    private void HandleNoise()
    {
        if (_currentConfig.noiseStrength <= 0f) return;

        _noiseTimer += Time.deltaTime;
        if (_noiseTimer < _currentConfig.noiseInterval) return;

        _noiseTimer = 0f;
        Array.Clear(_noiseOffsets, 0, _noiseOffsets.Length);

        for (int i = 0; i < NOISE_POINT_COUNT; i++)
        {
            int idx = UnityEngine.Random.Range(0, OscilloscopeSimulator.SAMPLE_COUNT);
            _noiseOffsets[idx] = UnityEngine.Random.Range(
                -_currentConfig.noiseStrength, _currentConfig.noiseStrength);
        }
    }

    private void HandleInversion()
    {
        if (!_currentConfig.hasInversion) return;

        _inversionTimer += Time.deltaTime;
        if (_inversionTimer < _currentConfig.inversionInterval) return;

        _inversionTimer = 0f;
        // Переворачиваем волну — игрок должен среагировать
        _currentConfig.targetAmplitude *= -1f;
    }

    private void UpdateWaveRenderers()
    {
        _targetWaveRenderer?.UpdateWave(
            _currentConfig.targetAmplitude,
            _currentConfig.targetFrequency,
            _currentConfig.targetPhase,
            _noiseOffsets);

        _playerWaveRenderer?.UpdateWave(
            _playerAmplitude,
            _playerFrequency,
            _playerPhase);
    }

    private void EvaluateMatchScore()
    {
        float score = OscilloscopeSimulator.ComputeMatchScore(
            _currentConfig.targetAmplitude,
            _currentConfig.targetFrequency,
            _currentConfig.targetPhase,
            _noiseOffsets,
            _playerAmplitude,
            _playerFrequency,
            _playerPhase);

        if (_matchScoreSlider != null) _matchScoreSlider.value = score;
        if (_matchScoreText   != null) _matchScoreText.text   = $"{Mathf.RoundToInt(score * 100f)}%";

        if (score >= MATCH_THRESHOLD)
        {
            _matchTimer += Time.deltaTime;

            if (_matchTimer >= MATCH_HOLD_TIME && _successRoutine == null)
                _successRoutine = StartCoroutine(SuccessRoutine());
        }
        else
        {
            // Не удержал порог — таймер сбрасывается
            _matchTimer = 0f;
        }
    }

    private IEnumerator SuccessRoutine()
    {
        SoundManager.PlaySound(SoundType.UICONFIRM);

        if (_fragmentRevealText != null)
        {
            _fragmentRevealText.text = _currentConfig.revealedText;
            _revealRoutine = StartCoroutine(FadeInText(_fragmentRevealText, _revealFadeDuration));
        }

        bool hasMore = _terminal.TryGetNextFragment(out DecipherFragmentConfig nextConfig);
        UpdateProgressUI();

        yield return _waitSuccessDelay;

        if (!_isOpen)
        {
            _successRoutine = null;
            yield break;
        }

        if (hasMore)
            LoadFragment(nextConfig);
        else
            CloseScreen();

        _successRoutine = null;
    }

    private IEnumerator FadeInText(TMP_Text text, float duration)
    {
        Color c = text.color;
        c.a = 0f;
        text.color = c;
        text.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a    = Mathf.Clamp01(timer / duration);
            text.color = c;
            yield return null;
        }

        _revealRoutine = null;
    }

    private void RequestExit()
    {
        if (!_isOpen) return;

        if (_successRoutine != null)
        {
            StopCoroutine(_successRoutine);
            _successRoutine = null;
        }

        if (_revealRoutine != null)
        {
            StopCoroutine(_revealRoutine);
            _revealRoutine = null;
        }

        CloseScreen();
    }

    private void CloseScreen()
    {
        _isOpen = false;

        if (_screenRoot != null) _screenRoot.SetActive(false);

        if (_playerController != null) _playerController.enabled = true;
        if (_cameraController != null) _cameraController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _terminal?.NotifyExit();
        _terminal = null;
    }

    private void UpdateProgressUI()
    {
        if (_terminal == null) return;

        int deciphered = _terminal.FragmentsDeciphered;
        int total      = _terminal.TotalFragments;

        if (_progressSlider != null)
            _progressSlider.value = total > 0 ? (float)deciphered / total : 0f;

        if (_progressText != null)
            _progressText.text = $"{deciphered} / {total}";
    }
}
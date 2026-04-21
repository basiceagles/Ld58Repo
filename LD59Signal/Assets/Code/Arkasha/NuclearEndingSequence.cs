using System.Collections;
using UnityEngine;

// Концовка — ядерный взрыв.
// 1. Сфера взрыва растёт на месте
// 2. Через _delayFallSound — первый звук
// 3. Сразу после взрыва — волна начинает расти
// 4. Через _delayWaveSound — второй звук
// 5. Волна касается игрока → вспышка → чёрный экран
public class NuclearEndingSequence : MonoBehaviour
{
    [Header("Таймеры")]
    [SerializeField] private float _delayBeforeStart = 5f;  // пауза перед стартом
    [SerializeField] private float _delayFallSound   = 2f;  // через сколько после старта взрыва — первый звук
    [SerializeField] private float _delayWaveSound   = 3f;  // через сколько после старта волны — второй звук

    [Header("Точка взрыва")]
    [SerializeField] private Transform _explosionPoint;

    [Header("Сфера взрыва")]
    [SerializeField] private float    _explosionGrowDuration = 4f;
    [SerializeField] private float    _explosionStartScale   = 2f;
    [SerializeField] private float    _explosionEndScale     = 80f;
    [SerializeField] private Material _explosionMaterial;

    [Header("Волна")]
    [SerializeField] private float    _waveGrowDuration  = 10f;
    [SerializeField] private float    _waveStartScale    = 5f;
    [SerializeField] private float    _waveEndScale      = 2000f;
    [SerializeField] private float    _waveLightIntensity = 8f;
    [SerializeField] private float    _waveLightRange     = 1000f;
    [SerializeField] private Material _waveMaterial;

    [Header("Звуки")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip   _fallClip;  // первый звук — взрыв
    [SerializeField] private AudioClip   _waveClip;  // второй звук — волна

    [Header("Fade")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private float       _flashDuration = 1f;

    [Header("Ссылки")]
    [SerializeField] private Transform _playerTransform;

    private GameObject _explosionSphere;
    private GameObject _waveSphere;
    private Light      _waveLight;

    private void Start()
    {
        if (_playerTransform == null)
            _playerTransform = GameObject.FindWithTag("Player")?.transform;

        if (_fadeCanvasGroup != null)
            _fadeCanvasGroup.alpha = 0f;

        if (_explosionPoint == null)
        {
            Debug.LogError("[NuclearEnding] Точка взрыва не назначена!", this);
            return;
        }

        StartCoroutine(EndingRoutine());
    }

    private IEnumerator EndingRoutine()
    {
        yield return new WaitForSeconds(_delayBeforeStart);

        // Запускаем взрыв и волну параллельно
        StartCoroutine(ExplosionRoutine());
        StartCoroutine(WaveRoutine());

        // Ждём пока волна коснётся игрока
        yield return StartCoroutine(WaitForWaveContact());

        // Вспышка и чёрный экран
        yield return StartCoroutine(FlashAndFadeRoutine());
    }

    // Сфера взрыва растёт на месте + первый звук
    private IEnumerator ExplosionRoutine()
    {
        // Спавним сферу взрыва
        _explosionSphere = CreateSphere(
            _explosionMaterial,
            new Color(1f, 0.4f, 0f),
            _explosionStartScale,
            false);

        // Первый звук через n секунд после начала роста
        StartCoroutine(PlaySoundDelayed(_fallClip, _delayFallSound));

        // Растим взрыв
        yield return StartCoroutine(GrowSphere(
            _explosionSphere,
            _explosionStartScale,
            _explosionEndScale,
            _explosionGrowDuration));
    }

    // Волна растёт из точки взрыва и движется к игроку
    private IEnumerator WaveRoutine()
    {
        // Небольшая задержка чтобы взрыв уже начался
        yield return new WaitForSeconds(0.5f);

        // Спавним волну
        _waveSphere = CreateSphere(
            _waveMaterial,
            new Color(1f, 0.6f, 0.1f),
            _waveStartScale,
            true);

        // Второй звук через n секунд после старта волны
        StartCoroutine(PlaySoundDelayed(_waveClip, _delayWaveSound));

        // Растим волну
        yield return StartCoroutine(GrowSphere(
            _waveSphere,
            _waveStartScale,
            _waveEndScale,
            _waveGrowDuration));
    }

    // Ждём момента когда волна достигает игрока
   private IEnumerator WaitForWaveContact()
{
    // Ждём пока волна заспавнится
    yield return new WaitUntil(() => _waveSphere != null);

    if (_playerTransform == null) yield break;

    while (true)
    {
            if (_waveSphere == null) break;

            // Радиус волны = половина масштаба
            float waveRadius    = _waveSphere.transform.localScale.x * 0.5f;
            float distToPlayer  = Vector3.Distance(
                _waveSphere.transform.position,
                _playerTransform.position);

            // Волна накрыла игрока
            if (waveRadius >= distToPlayer)
                break;

            yield return null;
        }
    }

    // Создаёт сферу в точке взрыва
    private GameObject CreateSphere(Material mat, Color fallbackColor, float startScale, bool withLight)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.position   = _explosionPoint.position;
        sphere.transform.localScale = Vector3.one * startScale;

        Destroy(sphere.GetComponent<Collider>());

        sphere.GetComponent<MeshRenderer>().material =
            GetOrCreateMaterial(mat, fallbackColor);

        if (withLight)
        {
            _waveLight           = sphere.AddComponent<Light>();
            _waveLight.type      = LightType.Point;
            _waveLight.color     = new Color(1f, 0.5f, 0f);
            _waveLight.intensity = _waveLightIntensity;
            _waveLight.range     = _waveLightRange;
        }

        return sphere;
    }

    // Плавно увеличивает сферу
    private IEnumerator GrowSphere(GameObject sphere, float startScale, float endScale, float duration)
    {
        if (sphere == null) yield break;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t     = Mathf.SmoothStep(0f, 1f, timer / duration);
            float scale = Mathf.Lerp(startScale, endScale, t);
            sphere.transform.localScale = Vector3.one * scale;
            yield return null;
        }
    }

    // Проигрывает звук с задержкой
    private IEnumerator PlaySoundDelayed(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip);
    }

    // Белая вспышка → чёрный экран навсегда
   // Белая вспышка → чёрный экран навсегда
    private IEnumerator FlashAndFadeRoutine()
    {
        if (_fadeCanvasGroup == null) yield break;

        var image = _fadeCanvasGroup.GetComponent<UnityEngine.UI.Image>();

        // Глушим все звуки сцены
        AudioListener.volume = 0f;

        if (image != null) image.color = Color.white;
        _fadeCanvasGroup.alpha = 1f;

        // На белом экране ждём Escape или таймер
        float timer = 0f;
        while (timer < _flashDuration)
        {
            timer += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LoadMainMenu();
                yield break;
            }

            yield return null;
        }

        if (image != null) image.color = Color.black;

        // На чёрном экране тоже ждём Escape
        while (true)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LoadMainMenu();
                yield break;
            }

            yield return null;
        }
    }

    private void LoadMainMenu()
    {
        AudioListener.volume = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    private void PlaySound(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
            _audioSource.PlayOneShot(clip);
    }

    private Material GetOrCreateMaterial(Material existing, Color fallbackColor)
    {
        if (existing != null) return existing;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = fallbackColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", fallbackColor * 3f);
        return mat;
    }
}
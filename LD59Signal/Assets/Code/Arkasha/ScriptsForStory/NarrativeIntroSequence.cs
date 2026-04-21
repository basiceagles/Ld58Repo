using System.Collections;
using UnityEngine;

// Запускается один раз при старте сцены.
// Только фейд — имитация открывания глаз.
// Диалог и дверь — через RadioStation.
public class NarrativeIntroSequence : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;  // чёрный оверлей на весь экран
    [SerializeField] private float _fadeInDelay    = 1f;    // пауза перед началом фейда
    [SerializeField] private float _fadeInDuration = 2f;    // длительность появления картинки

    [Header("Блокировка игрока на время фейда")]
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private CameraController _cameraController;

    private void Start()
    {
        // Экран чёрный с самого начала
        if (_fadeCanvasGroup != null)
            _fadeCanvasGroup.alpha = 1f;

        SetPlayerControl(false);
        StartCoroutine(IntroRoutine());
    }

    private IEnumerator IntroRoutine()
    {
        yield return new WaitForSeconds(_fadeInDelay);

        yield return StartCoroutine(FadeOut(_fadeInDuration));

        // Фейд завершён — возвращаем управление
        SetPlayerControl(true);
    }

    // Плавно убирает чёрный оверлей
    private IEnumerator FadeOut(float duration)
    {
        if (_fadeCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            _fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / duration);
            yield return null;
        }

        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.gameObject.SetActive(false);
    }

    private void SetPlayerControl(bool enabled)
    {
        if (_playerController != null) _playerController.enabled = enabled;
        if (_cameraController != null) _cameraController.enabled = enabled;
    }
}

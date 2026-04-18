using System;
using System.Collections;
using UnityEngine;

public sealed class WeatherFader : MonoBehaviour
{
    public enum WeatherType
    {
        Basic,
        Foggy,
    }

    [Serializable]
    public struct WeatherPreset
    {
        public Color environmentFogColor;
        [Min(0f)]
        public float environmentFogDensity;
        public Color mainCameraBackgroundColor;
    }

    [Header("References")]
    [SerializeField] private Camera _mainCamera;

    [Header("Weather Presets")]
    [SerializeField] private WeatherPreset _basicWeather;
    [SerializeField] private WeatherPreset _foggyWeather;

    [Header("Fade")]
    [Min(0f)]
    [SerializeField] private float _defaultFadeTimeSeconds = 1f;

    private Coroutine _fadeRoutine;

    private void Reset()
    {
        _mainCamera = Camera.main;
    }

    private void Awake()
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }
    }

    [ContextMenu("Debug/Fade: Basic -> Foggy")]
    private void DebugFadeBasicToFoggy()
    {
        FadeToFoggy();
    }

    [ContextMenu("Debug/Fade: Foggy -> Basic")]
    private void DebugFadeFoggyToBasic()
    {
        FadeToBasic();
    }

    public void FadeToBasic() => FadeTo(WeatherType.Basic, _defaultFadeTimeSeconds);

    public void FadeToFoggy() => FadeTo(WeatherType.Foggy, _defaultFadeTimeSeconds);

    public void FadeToBasic(float fadeTimeSeconds) => FadeTo(WeatherType.Basic, fadeTimeSeconds);

    public void FadeToFoggy(float fadeTimeSeconds) => FadeTo(WeatherType.Foggy, fadeTimeSeconds);

    public void FadeTo(WeatherType weatherType) => FadeTo(weatherType, _defaultFadeTimeSeconds);

    public void FadeTo(WeatherType weatherType, float fadeTimeSeconds)
    {
        var target = GetPreset(weatherType);
        FadeTo(target, fadeTimeSeconds);
    }

    public void FadeTo(WeatherPreset targetPreset, float fadeTimeSeconds)
    {
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
        }

        if (_mainCamera == null)
        {
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
            _fadeRoutine = null;
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(targetPreset, Mathf.Max(0f, fadeTimeSeconds)));
    }

    private WeatherPreset GetPreset(WeatherType weatherType)
    {
        return weatherType switch
        {
            WeatherType.Basic => _basicWeather,
            WeatherType.Foggy => _foggyWeather,
            _ => _basicWeather,
        };
    }

    private IEnumerator FadeRoutine(WeatherPreset target, float fadeTimeSeconds)
    {
        RenderSettings.fog = true;

        WeatherPreset start = CaptureCurrent();

        if (fadeTimeSeconds <= 0f)
        {
            Apply(target);
            _fadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeTimeSeconds;
            float easedT = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

            WeatherPreset lerped = new WeatherPreset
            {
                environmentFogColor = Color.Lerp(start.environmentFogColor, target.environmentFogColor, easedT),
                environmentFogDensity = Mathf.Lerp(start.environmentFogDensity, target.environmentFogDensity, easedT),
                mainCameraBackgroundColor = Color.Lerp(start.mainCameraBackgroundColor, target.mainCameraBackgroundColor, easedT),
            };

            Apply(lerped);
            yield return null;
        }

        Apply(target);
        _fadeRoutine = null;
    }

    private WeatherPreset CaptureCurrent()
    {
        return new WeatherPreset
        {
            environmentFogColor = RenderSettings.fogColor,
            environmentFogDensity = RenderSettings.fogDensity,
            mainCameraBackgroundColor = _mainCamera.backgroundColor,
        };
    }

    private void Apply(WeatherPreset preset)
    {
        RenderSettings.fogColor = preset.environmentFogColor;
        RenderSettings.fogDensity = preset.environmentFogDensity;
        _mainCamera.backgroundColor = preset.mainCameraBackgroundColor;
    }
}

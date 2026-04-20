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
        public Color environmentAmbientColor;
        public float directionalLightIntensity;
        public float grassWindSpeed;
        public float grassWindSize;
        public float grassWindBending;
    }

    [Header("References")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Light _directionalLight;
    [SerializeField] private Terrain _terrain;

    [Header("Ambient Audio")]
    [SerializeField] private AudioSource _ambientAudioSource;
    [SerializeField] private AudioSource _ambientAudioSourceSecondary;
    [SerializeField] private AudioClip _basicAmbientClip;
    [SerializeField] private AudioClip _foggyAmbientClip;
    [Min(0f)]
    [SerializeField] private float _audioFadeTimeSeconds = 1f;

    [Header("Weather Presets")]
    [SerializeField] private WeatherPreset _basicWeather;
    [SerializeField] private WeatherPreset _foggyWeather;

    [Header("Transition Settings")]
    [Min(0f)]
    [SerializeField] private float _defaultFadeTimeSeconds = 1f;

    private Coroutine _fadeRoutine;
    private float _ambientInitialVolume = 1f;
    private AudioSource _activeAmbientSource;
    private AudioSource _inactiveAmbientSource;

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

        if (_directionalLight == null)
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    _directionalLight = lights[i];
                    break;
                }
            }
        }

        if (_terrain == null)
        {
            _terrain = Terrain.activeTerrain;
        }

        if (_ambientAudioSource != null)
        {
            _ambientInitialVolume = _ambientAudioSource.volume;
        }

        EnsureAmbientSources();
    }

    private void Start()
    {
        Apply(_basicWeather);
        StartAmbientImmediate(_basicAmbientClip);
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
                environmentAmbientColor = Color.Lerp(start.environmentAmbientColor, target.environmentAmbientColor, easedT),
                directionalLightIntensity = Mathf.Lerp(start.directionalLightIntensity, target.directionalLightIntensity, easedT),
                grassWindSpeed = Mathf.Lerp(start.grassWindSpeed, target.grassWindSpeed, easedT),
                grassWindSize = Mathf.Lerp(start.grassWindSize, target.grassWindSize, easedT),
                grassWindBending = Mathf.Lerp(start.grassWindBending, target.grassWindBending, easedT),
            };

            Apply(lerped);
            yield return null;
        }

        Apply(target);
        _fadeRoutine = null;
    }

    private WeatherPreset CaptureCurrent()
    {
        TerrainData terrainData = _terrain != null ? _terrain.terrainData : null;

        return new WeatherPreset
        {
            environmentFogColor = RenderSettings.fogColor,
            environmentFogDensity = RenderSettings.fogDensity,
            mainCameraBackgroundColor = _mainCamera.backgroundColor,
            environmentAmbientColor = RenderSettings.ambientLight,
            directionalLightIntensity = _directionalLight != null ? _directionalLight.intensity : 0f,
            grassWindSpeed = terrainData != null ? terrainData.wavingGrassSpeed : 0f,
            grassWindSize = terrainData != null ? terrainData.wavingGrassAmount : 0f,
            grassWindBending = terrainData != null ? terrainData.wavingGrassStrength : 0f,
        };
    }

    private void Apply(WeatherPreset preset)
    {
        RenderSettings.fogColor = preset.environmentFogColor;
        RenderSettings.fogDensity = preset.environmentFogDensity;
        _mainCamera.backgroundColor = preset.mainCameraBackgroundColor;
        RenderSettings.ambientLight = preset.environmentAmbientColor;
        if (_directionalLight != null)
        {
            _directionalLight.intensity = preset.directionalLightIntensity;
        }

        TerrainData terrainData = _terrain != null ? _terrain.terrainData : null;
        if (terrainData != null)
        {
            terrainData.wavingGrassSpeed = preset.grassWindSpeed;
            terrainData.wavingGrassAmount = preset.grassWindSize;
            terrainData.wavingGrassStrength = preset.grassWindBending;
        }
    }

    private void StartAmbientImmediate(AudioClip clip)
    {
        EnsureAmbientSources();
        if (_activeAmbientSource == null)
        {
            return;
        }

        if (clip == null)
        {
            _activeAmbientSource.Stop();
            return;
        }

        _activeAmbientSource.clip = clip;
        _activeAmbientSource.volume = _ambientInitialVolume;
        _activeAmbientSource.loop = true;
        _activeAmbientSource.Play();

        if (_inactiveAmbientSource != null)
        {
            _inactiveAmbientSource.Stop();
            _inactiveAmbientSource.clip = null;
            _inactiveAmbientSource.volume = 0f;
        }
    }

    private IEnumerator CrossfadeAmbient(AudioClip nextClip, float fadeTimeSeconds)
    {
        EnsureAmbientSources();
        if (_activeAmbientSource == null)
        {
            yield break;
        }

        if (_inactiveAmbientSource == null)
        {
            StartAmbientImmediate(nextClip);
            yield break;
        }

        if (nextClip == null)
        {
            float tStop = 0f;
            float startStopVolume = _activeAmbientSource.volume;
            if (fadeTimeSeconds <= 0f)
            {
                _activeAmbientSource.Stop();
                _activeAmbientSource.clip = null;
                _activeAmbientSource.volume = _ambientInitialVolume;
                yield break;
            }

            while (tStop < 1f)
            {
                tStop += Time.deltaTime / fadeTimeSeconds;
                _activeAmbientSource.volume = Mathf.Lerp(startStopVolume, 0f, Mathf.Clamp01(tStop));
                yield return null;
            }

            _activeAmbientSource.volume = 0f;
            _activeAmbientSource.Stop();
            _activeAmbientSource.clip = null;
            _activeAmbientSource.volume = _ambientInitialVolume;
            yield break;
        }

        _inactiveAmbientSource.Stop();
        _inactiveAmbientSource.clip = nextClip;
        _inactiveAmbientSource.loop = true;
        _inactiveAmbientSource.volume = 0f;
        _inactiveAmbientSource.Play();

        if (fadeTimeSeconds <= 0f)
        {
            _activeAmbientSource.Stop();
            _activeAmbientSource.volume = 0f;
            _inactiveAmbientSource.volume = _ambientInitialVolume;
            SwapAmbientSources();
            yield break;
        }

        float t = 0f;
        float fromStartVolume = _activeAmbientSource.volume;
        while (t < 1f)
        {
            t += Time.deltaTime / fadeTimeSeconds;
            float k = Mathf.Clamp01(t);
            _activeAmbientSource.volume = Mathf.Lerp(fromStartVolume, 0f, k);
            _inactiveAmbientSource.volume = Mathf.Lerp(0f, _ambientInitialVolume, k);
            yield return null;
        }

        _activeAmbientSource.volume = 0f;
        _activeAmbientSource.Stop();
        _inactiveAmbientSource.volume = _ambientInitialVolume;
        SwapAmbientSources();
    }

    private void EnsureAmbientSources()
    {
        if (_ambientAudioSource != null && _ambientAudioSourceSecondary == null)
        {
            _ambientAudioSourceSecondary = gameObject.AddComponent<AudioSource>();
            _ambientAudioSourceSecondary.playOnAwake = false;
            _ambientAudioSourceSecondary.loop = true;
            _ambientAudioSourceSecondary.spatialBlend = _ambientAudioSource.spatialBlend;
            _ambientAudioSourceSecondary.outputAudioMixerGroup = _ambientAudioSource.outputAudioMixerGroup;
            _ambientAudioSourceSecondary.rolloffMode = _ambientAudioSource.rolloffMode;
            _ambientAudioSourceSecondary.minDistance = _ambientAudioSource.minDistance;
            _ambientAudioSourceSecondary.maxDistance = _ambientAudioSource.maxDistance;
            _ambientAudioSourceSecondary.dopplerLevel = _ambientAudioSource.dopplerLevel;
            _ambientAudioSourceSecondary.spread = _ambientAudioSource.spread;
            _ambientAudioSourceSecondary.priority = _ambientAudioSource.priority;
            _ambientAudioSourceSecondary.panStereo = _ambientAudioSource.panStereo;
            _ambientAudioSourceSecondary.reverbZoneMix = _ambientAudioSource.reverbZoneMix;
            _ambientAudioSourceSecondary.pitch = _ambientAudioSource.pitch;
            _ambientAudioSourceSecondary.mute = _ambientAudioSource.mute;
            _ambientAudioSourceSecondary.bypassEffects = _ambientAudioSource.bypassEffects;
            _ambientAudioSourceSecondary.bypassListenerEffects = _ambientAudioSource.bypassListenerEffects;
            _ambientAudioSourceSecondary.bypassReverbZones = _ambientAudioSource.bypassReverbZones;
            _ambientAudioSourceSecondary.ignoreListenerPause = _ambientAudioSource.ignoreListenerPause;
            _ambientAudioSourceSecondary.ignoreListenerVolume = _ambientAudioSource.ignoreListenerVolume;
            _ambientAudioSourceSecondary.volume = 0f;
        }

        if (_activeAmbientSource == null)
        {
            _activeAmbientSource = _ambientAudioSource;
            _inactiveAmbientSource = _ambientAudioSourceSecondary;
        }
    }

    private void SwapAmbientSources()
    {
        AudioSource temp = _activeAmbientSource;
        _activeAmbientSource = _inactiveAmbientSource;
        _inactiveAmbientSource = temp;
    }
}

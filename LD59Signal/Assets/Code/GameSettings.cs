using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Audio;
using System;
using UnityEngine.SceneManagement;

public class GameSettings : MonoBehaviour
{
    [Header("UI")]
    public TMPro.TMP_Dropdown resolutionDropdown;
    public TMPro.TMP_Dropdown refreshRateDropdown;
    public Toggle fullscreenToggle;
    public Slider sensitivitySlider;

    [Header("References")]
    public CameraController cameraController;

    [Header("Audio Mixer Settings")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    private Resolution[] resolutions;

    // FIXED / ADDED FIELDS
    private int targetFPS = -1;
    private int lastDisplayCount = 0;
    private int lastMonitorRefreshRate = 0;
    private List<int> availableRefreshRates = new List<int>();

    List<string> uniqueResolutions = new List<string>();

    void Update()
    {
        CheckForDisplayChanges();
    }

    void Start()
    {
        SetupResolutionDropdown();
        SetupRefreshRateDropdown();

        if (!PlayerPrefs.HasKey("FirstLaunch"))
        {
            Resolution currentRes = Screen.currentResolution;
            int maxRefreshRate = currentRes.refreshRate;

            foreach (Resolution res in resolutions)
            {
                if (res.width == currentRes.width && res.height == currentRes.height)
                {
                    if (res.refreshRate > maxRefreshRate)
                    {
                        maxRefreshRate = res.refreshRate;
                    }
                }
            }

            Screen.SetResolution(currentRes.width, currentRes.height, FullScreenMode.FullScreenWindow, maxRefreshRate);

            int optimalIndex = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == currentRes.width &&
                    resolutions[i].height == currentRes.height &&
                    resolutions[i].refreshRate == maxRefreshRate)
                {
                    optimalIndex = i;
                    break;
                }
            }

            PlayerPrefs.SetInt("FirstLaunch", 1);
            PlayerPrefs.SetInt("Fullscreen", 1);
            PlayerPrefs.SetInt("ResolutionIndex", optimalIndex);
            PlayerPrefs.Save();
        }

        RealTimeLoad();
        LoadSettings();
    }

    private void CheckForDisplayChanges()
    {
        int currentMonitorRefreshRate = Screen.currentResolution.refreshRate;

        if (Display.displays.Length != lastDisplayCount || currentMonitorRefreshRate != lastMonitorRefreshRate)
        {
            lastDisplayCount = Display.displays.Length;
            lastMonitorRefreshRate = currentMonitorRefreshRate;

            SetupResolutionDropdown();
            SetupRefreshRateDropdown();

            if (QualitySettings.vSyncCount == 0)
            {
                ApplyCurrentSettings();
            }
        }
    }

    private void ApplyCurrentSettings()
    {
        string resolutionString = uniqueResolutions[resolutionDropdown.value];
        string[] parts = resolutionString.Split('x');
        int width = int.Parse(parts[0]);
        int height = int.Parse(parts[1]);
        int refreshRate = availableRefreshRates[refreshRateDropdown.value];

        int actualRefreshRate = refreshRate > 0 ? refreshRate : Screen.currentResolution.refreshRate;

        if (!IsResolutionSupported(width, height, actualRefreshRate))
        {
            actualRefreshRate = GetNearestSupportedRefreshRate(width, height, actualRefreshRate);
        }

        Screen.SetResolution(width, height, Screen.fullScreen, actualRefreshRate);

        if (QualitySettings.vSyncCount == 0)
        {
            if (refreshRate == 0)
            {
                targetFPS = -1;
                Application.targetFrameRate = -1;
            }
            else
            {
                targetFPS = refreshRate;
                Application.targetFrameRate = targetFPS;
            }
        }
    }

    private void RealTimeLoad()
    {
        sfxSlider.onValueChanged.AddListener(SfxMixerVolume);
        musicSlider.onValueChanged.AddListener(MusicMixerVolume);
    }

    public void SfxMixerVolume(float sliderValue)
    {
        float volumeDB = ConvertToDecibels(sliderValue);
        sfxMixerGroup.audioMixer.SetFloat("SFX", volumeDB);
        PlayerPrefs.SetFloat("SfxVolume", sliderValue);
    }

    public void MusicMixerVolume(float sliderValue)
    {
        float volumeDB = ConvertToDecibels(sliderValue);
        musicMixerGroup.audioMixer.SetFloat("Music", volumeDB);
        PlayerPrefs.SetFloat("MusicVolume", sliderValue);
    }

    private float ConvertToDecibels(float linearVolume)
    {
        if (linearVolume <= 0.001f)
            return -80f;

        float adjustedVolume = Mathf.Pow(linearVolume, 0.7f);
        return Mathf.Log10(adjustedVolume) * 20f;
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        uniqueResolutions.Clear();

        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string resolutionString = resolutions[i].width + "x" + resolutions[i].height;

            if (!uniqueResolutions.Contains(resolutionString))
            {
                uniqueResolutions.Add(resolutionString);
            }

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = uniqueResolutions.IndexOf(resolutionString);
            }
        }

        resolutionDropdown.AddOptions(uniqueResolutions);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void SetupRefreshRateDropdown()
    {
        UpdateAvailableRefreshRates();

        int currentRefreshRateIndex = 1;
        for (int i = 0; i < availableRefreshRates.Count; i++)
        {
            if (availableRefreshRates[i] == Screen.currentResolution.refreshRate)
            {
                currentRefreshRateIndex = i;
                break;
            }
        }

        refreshRateDropdown.value = currentRefreshRateIndex;
        refreshRateDropdown.RefreshShownValue();
    }

    private void UpdateAvailableRefreshRates()
    {
        availableRefreshRates.Clear();
        refreshRateDropdown.ClearOptions();

        string currentResolutionString = uniqueResolutions[resolutionDropdown.value];
        string[] parts = currentResolutionString.Split('x');
        int width = int.Parse(parts[0]);
        int height = int.Parse(parts[1]);

        List<string> refreshRateOptions = new List<string>();

        availableRefreshRates.Add(0);
        refreshRateOptions.Add("No limits");

        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                if (!availableRefreshRates.Contains(resolutions[i].refreshRate))
                {
                    availableRefreshRates.Add(resolutions[i].refreshRate);
                    refreshRateOptions.Add(resolutions[i].refreshRate + "Hz");
                }
            }
        }

        refreshRateDropdown.AddOptions(refreshRateOptions);
    }

    private void LoadSettings()
    {
        LoadDropdownValue("ResolutionIndex", resolutionDropdown, uniqueResolutions.Count);
        LoadDropdownValue("RefreshRateIndex", refreshRateDropdown, availableRefreshRates.Count);

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);
        SetFullscreen(isFullscreen);

        float sensitivity = PlayerPrefs.GetFloat("Sensitivity", 1f);
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        SetSensitivity(sensitivity);

        float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        sfxSlider.SetValueWithoutNotify(sfxVolume);
        SfxMixerVolume(sfxVolume);

        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        musicSlider.SetValueWithoutNotify(musicVolume);
        MusicMixerVolume(musicVolume);
    }

    private void LoadDropdownValue(string key, TMPro.TMP_Dropdown dropdown, int maxCount)
    {
        int savedValue = PlayerPrefs.GetInt(key, dropdown.value);
        if (savedValue < maxCount)
        {
            dropdown.value = savedValue;
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        UpdateAvailableRefreshRates();
        ApplyCurrentSettings();
    }

    public void SetRefreshRate(int refreshRateIndex)
    {
        ApplyCurrentSettings();
    }

    private bool IsResolutionSupported(int width, int height, int refreshRate)
    {
        foreach (Resolution res in Screen.resolutions)
        {
            if (res.width == width && res.height == height && res.refreshRate == refreshRate)
            {
                return true;
            }
        }
        return false;
    }

    private int GetNearestSupportedRefreshRate(int width, int height, int targetRefreshRate)
    {
        int nearestRate = 60;
        int minDifference = int.MaxValue;

        foreach (Resolution res in Screen.resolutions)
        {
            if (res.width == width && res.height == height)
            {
                int difference = Mathf.Abs(res.refreshRate - targetRefreshRate);
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearestRate = res.refreshRate;
                }
            }
        }

        return nearestRate;
    }

    public void SetFullscreen(bool isFullScreen)
    {
        Screen.fullScreenMode = isFullScreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.fullScreen = isFullScreen;
    }

    public void SetSensitivity(float sensitivity)
    {
        if (cameraController != null)
        {
            cameraController.SetSensitivity(sensitivity);
        }

        PlayerPrefs.SetFloat("Sensitivity", sensitivity);
    }
}
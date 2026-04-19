using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

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
    private int lastDisplayCount = 0;
    private List<int> availableRefreshRates = new List<int>();
    private List<string> uniqueResolutions = new List<string>();

    private void Update()
    {
        CheckForDisplayChanges();
    }

    private void Start()
    {
        SetupResolutionDropdown();
        UpdateAvailableRefreshRates();

        if (!PlayerPrefs.HasKey("FirstLaunch"))
        {
            Resolution currentRes = Screen.currentResolution;
            
            PlayerPrefs.SetInt("FirstLaunch", 1);
            PlayerPrefs.SetInt("Fullscreen", 1);
            
            string resString = currentRes.width + "x" + currentRes.height;
            int resIndex = uniqueResolutions.IndexOf(resString);
            if (resIndex == -1) resIndex = uniqueResolutions.Count - 1;
            PlayerPrefs.SetInt("ResolutionIndex", resIndex);
            
            UpdateAvailableRefreshRates();
            int maxHzIndex = availableRefreshRates.Count - 1;
            PlayerPrefs.SetInt("RefreshRateIndex", maxHzIndex);
            
            PlayerPrefs.SetFloat("Sensitivity", 100f);
            PlayerPrefs.Save();
        }

        RealTimeLoad();
        LoadSettings();
    }

    private void CheckForDisplayChanges()
    {
        if (Display.displays.Length != lastDisplayCount)
        {
            lastDisplayCount = Display.displays.Length;
            SetupResolutionDropdown();
            UpdateAvailableRefreshRates();
            ApplyCurrentSettings();
        }
    }

    private void ApplyCurrentSettings()
    {
        if (uniqueResolutions.Count == 0 || resolutionDropdown.value >= uniqueResolutions.Count) return;
        
        string resolutionString = uniqueResolutions[resolutionDropdown.value];
        string[] parts = resolutionString.Split('x');
        int width = int.Parse(parts[0]);
        int height = int.Parse(parts[1]);
        
        int refreshRate = 0;
        if (availableRefreshRates.Count > 0 && refreshRateDropdown.value < availableRefreshRates.Count)
        {
            refreshRate = availableRefreshRates[refreshRateDropdown.value];
        }

        bool isFullscreen = fullscreenToggle.isOn;
        FullScreenMode mode = isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

        if (refreshRate > 0)
        {
            Screen.SetResolution(width, height, mode, refreshRate);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = refreshRate;
        }
        else
        {
            Screen.SetResolution(width, height, mode);
            QualitySettings.vSyncCount = 0; 
            Application.targetFrameRate = -1;
        }
    }

    private void RealTimeLoad()
    {
        sfxSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        masterSlider?.onValueChanged.RemoveAllListeners();
        fullscreenToggle.onValueChanged.RemoveAllListeners();
        sensitivitySlider.onValueChanged.RemoveAllListeners();
        resolutionDropdown.onValueChanged.RemoveAllListeners();
        refreshRateDropdown.onValueChanged.RemoveAllListeners();

        sfxSlider.onValueChanged.AddListener(SfxMixerVolume);
        musicSlider.onValueChanged.AddListener(MusicMixerVolume);
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(MasterMixerVolume);
        
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        refreshRateDropdown.onValueChanged.AddListener(SetRefreshRate);
    }

    public void MasterMixerVolume(float sliderValue)
    {
        float volumeDB = ConvertToDecibels(sliderValue);
        masterMixerGroup.audioMixer.SetFloat("Master", volumeDB);
        PlayerPrefs.SetFloat("MasterVolume", sliderValue);
        PlayerPrefs.Save();
    }

    public void SfxMixerVolume(float sliderValue)
    {
        float volumeDB = ConvertToDecibels(sliderValue);
        sfxMixerGroup.audioMixer.SetFloat("SFX", volumeDB);
        PlayerPrefs.SetFloat("SfxVolume", sliderValue);
        PlayerPrefs.Save();
    }

    public void MusicMixerVolume(float sliderValue)
    {
        float volumeDB = ConvertToDecibels(sliderValue);
        musicMixerGroup.audioMixer.SetFloat("Music", volumeDB);
        PlayerPrefs.SetFloat("MusicVolume", sliderValue);
        PlayerPrefs.Save();
    }

    private float ConvertToDecibels(float linearVolume)
    {
        if (linearVolume <= 0.001f) return -80f;
        return Mathf.Log10(Mathf.Pow(linearVolume, 0.7f)) * 20f;
    }

    private void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        uniqueResolutions.Clear();

        string currentResString = Screen.width + "x" + Screen.height;

        for (int i = resolutions.Length - 1; i >= 0; i--)
        {
            string resolutionString = resolutions[i].width + "x" + resolutions[i].height;
            if (!uniqueResolutions.Contains(resolutionString)) uniqueResolutions.Add(resolutionString);
        }
        
        resolutionDropdown.AddOptions(uniqueResolutions);
        int currentResolutionIndex = uniqueResolutions.IndexOf(currentResString);
        if (currentResolutionIndex != -1) resolutionDropdown.SetValueWithoutNotify(currentResolutionIndex);
    }

    private void UpdateAvailableRefreshRates()
    {
        int previousHz = -1;
        if (availableRefreshRates.Count > 0 && refreshRateDropdown.value < availableRefreshRates.Count)
        {
            previousHz = availableRefreshRates[refreshRateDropdown.value];
        }

        availableRefreshRates.Clear();
        refreshRateDropdown.ClearOptions();

        if (uniqueResolutions.Count == 0 || resolutionDropdown.value >= uniqueResolutions.Count) return;

        string currentResolutionString = uniqueResolutions[resolutionDropdown.value];
        string[] parts = currentResolutionString.Split('x');
        int width = int.Parse(parts[0]);
        int height = int.Parse(parts[1]);

        List<string> refreshRateOptions = new List<string> { "No limits" };
        availableRefreshRates.Add(0);

        List<int> rates = new List<int>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == width && resolutions[i].height == height)
            {
                if (!rates.Contains(resolutions[i].refreshRate)) rates.Add(resolutions[i].refreshRate);
            }
        }
        
        rates.Sort();
        foreach (int rate in rates)
        {
            availableRefreshRates.Add(rate);
            refreshRateOptions.Add(rate + "Hz");
        }

        refreshRateDropdown.AddOptions(refreshRateOptions);

        if (previousHz != -1)
        {
            int newIndex = availableRefreshRates.IndexOf(previousHz);
            if (newIndex != -1) refreshRateDropdown.SetValueWithoutNotify(newIndex);
        }
        
        refreshRateDropdown.RefreshShownValue();
    }

    private void LoadSettings()
    {
        int resIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutionDropdown.value);
        if (resIndex < uniqueResolutions.Count) resolutionDropdown.SetValueWithoutNotify(resIndex);
        
        UpdateAvailableRefreshRates();
        
        int hzIndex = PlayerPrefs.GetInt("RefreshRateIndex", -1);
        if (hzIndex != -1 && hzIndex < availableRefreshRates.Count) refreshRateDropdown.SetValueWithoutNotify(hzIndex);

        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        fullscreenToggle.SetIsOnWithoutNotify(isFullscreen);

        float sensitivity = PlayerPrefs.GetFloat("Sensitivity", 100f);
        sensitivitySlider.SetValueWithoutNotify(sensitivity);
        
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(masterVolume);
            MasterMixerVolume(masterVolume);
        }

        float sfxVolume = PlayerPrefs.GetFloat("SfxVolume", 0.8f);
        sfxSlider.SetValueWithoutNotify(sfxVolume);
        SfxMixerVolume(sfxVolume);

        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        musicSlider.SetValueWithoutNotify(musicVolume);
        MusicMixerVolume(musicVolume);
        
        ApplyCurrentSettings();
        SetSensitivity(sensitivity);
    }

    public void SetResolution(int resolutionIndex)
    {
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        UpdateAvailableRefreshRates();
        ApplyCurrentSettings();
        PlayerPrefs.Save();
    }

    public void SetRefreshRate(int refreshRateIndex)
    {
        PlayerPrefs.SetInt("RefreshRateIndex", refreshRateIndex);
        ApplyCurrentSettings();
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullScreen)
    {
        PlayerPrefs.SetInt("Fullscreen", isFullScreen ? 1 : 0);
        ApplyCurrentSettings();
        PlayerPrefs.Save();
    }

    public void SetSensitivity(float sensitivity)
    {
        if (cameraController != null)
        {
            cameraController.SetSensitivity(sensitivity);
        }
        else
        {
            var foundController = FindAnyObjectByType<CameraController>();
            if (foundController != null) foundController.SetSensitivity(sensitivity);
        }

        PlayerPrefs.SetFloat("Sensitivity", sensitivity);
        PlayerPrefs.Save();
    }
}
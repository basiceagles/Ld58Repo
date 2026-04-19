using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Audio;

public class GameSettings : MonoBehaviour
{
    [Header("UI")]
    public Toggle fullscreenToggle;

    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown refreshRateDropdown;

    public Slider sensitivitySlider;

    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("References")]
    public CameraController cameraController;

    public AudioMixer audioMixer; 
    // Exposed parameters in mixer:
    // MusicVolume
    // SFXVolume

    private Resolution[] resolutions;

    List<string> uniqueResolutions = new List<string>();
    List<int> refreshRates = new List<int>();


    void Start()
    {
        SetupResolutions();

        // Hook listeners FIRST
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        resolutionDropdown.onValueChanged.AddListener(SetResolution);

        refreshRateDropdown.onValueChanged.AddListener(SetRefreshRate);

        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);

        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // THEN load saved values
        LoadSettings();
    }


    void SetupResolutions()
    {
        resolutions = Screen.resolutions;

        resolutionDropdown.ClearOptions();
        uniqueResolutions.Clear();

        int currentIndex = 0;

        foreach (Resolution res in resolutions)
        {
            string option = res.width + "x" + res.height;

            if (!uniqueResolutions.Contains(option))
                uniqueResolutions.Add(option);

            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
            {
                currentIndex = uniqueResolutions.IndexOf(option);
            }
        }

        resolutionDropdown.AddOptions(uniqueResolutions);

        resolutionDropdown.value = currentIndex;

        UpdateRefreshRates();
    }


    void UpdateRefreshRates()
    {
        refreshRateDropdown.ClearOptions();

        refreshRates.Clear();

        string[] split =
            uniqueResolutions[resolutionDropdown.value].Split('x');

        int width = int.Parse(split[0]);
        int height = int.Parse(split[1]);

        List<string> options = new List<string>();

        foreach (Resolution res in resolutions)
        {
            if (res.width == width &&
                res.height == height &&
                !refreshRates.Contains(res.refreshRate))
            {
                refreshRates.Add(res.refreshRate);

                options.Add(res.refreshRate + " Hz");
            }
        }

        refreshRateDropdown.AddOptions(options);
    }



    void LoadSettings()
    {
        // Fullscreen
        bool fullscreen =
            PlayerPrefs.GetInt("Fullscreen",1) == 1;

        fullscreenToggle.isOn = fullscreen;

        SetFullscreen(fullscreen);


        // Resolution
        int resIndex =
            PlayerPrefs.GetInt("Resolution",resolutionDropdown.value);

        resolutionDropdown.value = resIndex;

        UpdateRefreshRates();


        // Refresh
        int refreshIndex =
            PlayerPrefs.GetInt("RefreshRate",0);

        refreshRateDropdown.value = refreshIndex;

        ApplyResolution();


        float sens = PlayerPrefs.GetFloat("Sensitivity",1f);
        sensitivitySlider.SetValueWithoutNotify(sens);
        SetSensitivity(sens);


        float music = PlayerPrefs.GetFloat("Music",0.8f);
        musicSlider.SetValueWithoutNotify(music);
        SetMusicVolume(music);


        float sfx = PlayerPrefs.GetFloat("SFX",0.8f);
        sfxSlider.SetValueWithoutNotify(sfx);
        SetSFXVolume(sfx);
    }



    public void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;

        PlayerPrefs.SetInt("Fullscreen", value ? 1 : 0);

        PlayerPrefs.Save();
    }


    public void SetResolution(int index)
    {
        UpdateRefreshRates();

        ApplyResolution();

        PlayerPrefs.SetInt("Resolution", index);

        PlayerPrefs.Save();
    }


    public void SetRefreshRate(int index)
    {
        ApplyResolution();

        PlayerPrefs.SetInt("RefreshRate", index);

        PlayerPrefs.Save();
    }


    void ApplyResolution()
    {
        string[] split =
            uniqueResolutions[resolutionDropdown.value].Split('x');

        int width = int.Parse(split[0]);

        int height = int.Parse(split[1]);

        int hz = refreshRates[refreshRateDropdown.value];

        Screen.SetResolution(
            width,
            height,
            Screen.fullScreen,
            hz
        );
    }



    public void SetSensitivity(float value)
    {
        if(cameraController != null)
            cameraController.SetSensitivity(value);

        PlayerPrefs.SetFloat("Sensitivity", value);

        PlayerPrefs.Save();
    }



    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        float db = Mathf.Log10(value) * 20f;

        audioMixer.SetFloat("Music", db);

        PlayerPrefs.SetFloat("Music", value);

        PlayerPrefs.Save();

        Debug.Log("Music slider changed: " + value);
    }


    public void SetSFXVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        float db = Mathf.Log10(value) * 20f;

        audioMixer.SetFloat("SFX", db);

        PlayerPrefs.SetFloat("SFX", value);

        PlayerPrefs.Save();
    }

    public void ApplyAndSaveAllSettings()
    {
        // Fullscreen
        SetFullscreen(fullscreenToggle.isOn);

        // Resolution + refresh
        ApplyResolution();

        PlayerPrefs.SetInt(
            "Resolution",
            resolutionDropdown.value
        );

        PlayerPrefs.SetInt(
            "RefreshRate",
            refreshRateDropdown.value
        );


        // Sensitivity
        SetSensitivity(
            sensitivitySlider.value
        );


        // Music
        SetMusicVolume(
            musicSlider.value
        );


        // SFX
        SetSFXVolume(
            sfxSlider.value
        );


        // Final hard save
        PlayerPrefs.Save();
    }
}
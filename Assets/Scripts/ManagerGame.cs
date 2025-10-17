using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class ManagerGame : MonoBehaviour
{
    public static ManagerGame I { get; private set; }

    public static bool IsPaused { get; private set; }

    public static void SetPause(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f; // optional — stops physics automatically
    }

    // GameManager fields (top of class)
    const string PP_RES_INDEX = "res_index";
    const string PP_FULLSCREEN = "fullscreen";
    const string PP_BGM = "bgm";
    const string PP_SFX = "sfx";

    int SelectedResolution = -1;
    bool IsFullScreen = true;
    [Range(0f, 1f)] float bgmVolume = 0.5f;
    [Range(0f, 1f)] float sfxVolume = 0.5f;

    [Header("BGM Settings")]
    public AudioClip bgmClip;
    

    [Header("SFX Library")]
    public List<NamedClip> sfxList = new List<NamedClip>();
    Dictionary<string, AudioClip> sfxMap;

    [Header("General Settings")]
    public int sfxPoolSize = 10;
    public bool persistAcrossScenes = true;

    AudioSource bgmSource;
    AudioSource[] sfxPool;
    int poolIndex;

    [Header("UI References")]
    public TMP_Dropdown ResDropDown;
    public Toggle FullScreenToggle;
    public Scrollbar Volume;

    Resolution[] AllResolutions;
    List<string> resolutionStringList = new List<string>();
    List<Resolution> SelectedResolutionList = new List<Resolution>();

    [System.Serializable]
    public class NamedClip
    {
        public string name;
        public AudioClip clip;
    }

    void Awake()
    {
        // Singleton guard
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        LoadSettings();

        // BGM setup
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;
        bgmSource.playOnAwake = false;
        bgmSource.volume = bgmVolume;

        if (bgmClip)
        {
            bgmSource.clip = bgmClip;
            bgmSource.Play();
        }

        // SFX pool
        sfxPool = new AudioSource[sfxPoolSize];
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = false;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D sound
            sfxPool[i] = src;
        }

        // Build SFX dictionary
        sfxMap = new Dictionary<string, AudioClip>();
        foreach (var s in sfxList)
        {
            if (s != null && s.clip != null && !sfxMap.ContainsKey(s.name))
                sfxMap.Add(s.name, s.clip);
        }

        // Resolution and FullScreen
        InitializeResolutionsAndFullScreen();

        InitializeVolume();
    }

    private void OnLevelWasLoaded(int level)
    {
        InitializeResolutionsAndFullScreen();
        InitializeVolume();
    }

    void InitializeVolume()
    {
        Volume = FindAnyObjectByType<Scrollbar>();
        Volume.value = I.bgmVolume;
    }

    void InitializeResolutionsAndFullScreen()
    {
        ResDropDown = FindAnyObjectByType<TMP_Dropdown>();
        FullScreenToggle = FindAnyObjectByType<Toggle>();

        // Build resolution list only once
        if (I.SelectedResolutionList.Count == 0)
        {
            AllResolutions = Screen.resolutions;

            foreach (Resolution res in AllResolutions)
            {
                string newRes = $"{res.width} x {res.height}";
                if (!I.resolutionStringList.Contains(newRes))
                {
                    I.resolutionStringList.Add(newRes);
                    I.SelectedResolutionList.Add(res);
                }
            }
        }

        ResDropDown.ClearOptions();
        ResDropDown.AddOptions(I.resolutionStringList);

        // First run ever? Map to monitor’s current resolution
        if (I.SelectedResolution < 0 || I.SelectedResolution >= I.SelectedResolutionList.Count)
        {
            var cur = Screen.currentResolution;
            int idx = I.SelectedResolutionList.FindIndex(r => r.width == cur.width && r.height == cur.height);
            if (idx < 0) idx = 0; // fallback
            I.SelectedResolution = idx;
        }

        // Apply stored values
        ResDropDown.value = I.SelectedResolution;
        FullScreenToggle.isOn = I.IsFullScreen;

        // Actually set them again in case the scene load reset the resolution
        Resolution selected = I.SelectedResolutionList[I.SelectedResolution];
        Screen.SetResolution(selected.width, selected.height, I.IsFullScreen);

        ResDropDown.RefreshShownValue();
    }

    void SaveSettings()
    {
        PlayerPrefs.SetFloat(PP_BGM, bgmVolume);
        PlayerPrefs.SetFloat(PP_SFX, sfxVolume);
        PlayerPrefs.SetInt(PP_FULLSCREEN, IsFullScreen ? 1 : 0);
        PlayerPrefs.SetInt(PP_RES_INDEX, SelectedResolution);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        // volumes (default 0.6 if never saved)
        bgmVolume = PlayerPrefs.GetFloat(PP_BGM, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(PP_SFX, sfxVolume);

        // fullscreen (default to current)
        IsFullScreen = PlayerPrefs.GetInt(PP_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;

        // resolution index (default -1 = not set yet)
        SelectedResolution = PlayerPrefs.GetInt(PP_RES_INDEX, -1);
    }

    // --- Background Music ---
    public void PlayBGM(AudioClip clip = null)
    {
        if (clip != null) bgmSource.clip = clip;
        if (!bgmSource.isPlaying) bgmSource.Play();
    }

    public void StopBGM() => bgmSource.Stop();

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        bgmSource.volume = bgmVolume;
    }

    // --- Sound Effects ---
    public void PlaySFX(string name, float pitchVariance = 0f)
    {
        if (!sfxMap.TryGetValue(name, out var clip) || clip == null)
        {
            Debug.LogWarning($"AudioManager: Missing clip '{name}'");
            return;
        }

        var src = sfxPool[poolIndex];
        poolIndex = (poolIndex + 1) % sfxPoolSize;

        src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        src.volume = sfxVolume;
        src.PlayOneShot(clip);
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        foreach (var src in sfxPool) src.volume = sfxVolume;
    }

    public void LoadScene(int index)
    {
        SceneManager.LoadScene(index);
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game quit!"); // only visible in editor
        SaveSettings();
    }

    public void SetResolution()
    {
        SelectedResolution = ResDropDown.value;
        Resolution res = SelectedResolutionList[SelectedResolution];
        Screen.SetResolution(res.width, res.height, IsFullScreen);

        I.SelectedResolution = SelectedResolution;
    }

    public void SetFullScreen()
    {
        IsFullScreen = FullScreenToggle.isOn;
        Screen.fullScreen = IsFullScreen;

        I.IsFullScreen = IsFullScreen;
    }
}

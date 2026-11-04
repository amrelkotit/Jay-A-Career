using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI SCRIPT — player settings, saved in PlayerPrefs and applied on every launch:
/// master volume, mouse sensitivity, invert Y, subtitle size and graphics quality.
/// The static values are read by the player controller, dialogue box and HUD; the
/// MonoBehaviour part wires the Settings panel's sliders/toggles (main menu and pause menu).
/// </summary>
public class SettingsManager : MonoBehaviour
{
    const string VolumeKey = "jay.volume", SensitivityKey = "jay.sensitivity", InvertKey = "jay.invertY",
        SubtitleKey = "jay.subtitles", QualityKey = "jay.quality";

    public static float MasterVolume { get; private set; } = 1;
    public static float MouseSensitivity { get; private set; } = 1;
    public static bool InvertY { get; private set; }
    public static float SubtitleScale { get; private set; } = 1;

    [Header("Panel controls (optional)")]
    public CanvasGroup panel;
    public Slider volumeSlider, sensitivitySlider, subtitleSlider;
    public Toggle invertToggle;
    public TMP_Dropdown qualityDropdown;
    public Button closeButton;
    public GameObject[] hideWhileOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void LoadOnLaunch()
    {
        MasterVolume = PlayerPrefs.GetFloat(VolumeKey, 1);
        MouseSensitivity = PlayerPrefs.GetFloat(SensitivityKey, 1);
        InvertY = PlayerPrefs.GetInt(InvertKey, 0) == 1;
        SubtitleScale = PlayerPrefs.GetFloat(SubtitleKey, 1);
        AudioListener.volume = MasterVolume;
        if (PlayerPrefs.HasKey(QualityKey)) QualitySettings.SetQualityLevel(PlayerPrefs.GetInt(QualityKey), true);
    }

    void Awake()
    {
        if (volumeSlider) { volumeSlider.minValue = 0; volumeSlider.maxValue = 1; volumeSlider.SetValueWithoutNotify(MasterVolume); volumeSlider.onValueChanged.AddListener(SetVolume); }
        if (sensitivitySlider) { sensitivitySlider.minValue = .3f; sensitivitySlider.maxValue = 2.5f; sensitivitySlider.SetValueWithoutNotify(MouseSensitivity); sensitivitySlider.onValueChanged.AddListener(SetSensitivity); }
        if (subtitleSlider) { subtitleSlider.minValue = .8f; subtitleSlider.maxValue = 1.5f; subtitleSlider.SetValueWithoutNotify(SubtitleScale); subtitleSlider.onValueChanged.AddListener(SetSubtitleScale); }
        if (invertToggle) { invertToggle.SetIsOnWithoutNotify(InvertY); invertToggle.onValueChanged.AddListener(SetInvertY); }
        if (qualityDropdown)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
            qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());
            qualityDropdown.onValueChanged.AddListener(SetQuality);
        }
        if (closeButton) closeButton.onClick.AddListener(Close);
        SetVisible(false);
    }

    public void Open() => SetVisible(true);
    public void Close() => SetVisible(false);

    void SetVisible(bool value)
    {
        if (panel) { panel.alpha = value ? 1 : 0; panel.blocksRaycasts = value; panel.interactable = value; }
        foreach (var g in hideWhileOpen) if (g) g.SetActive(!value);
    }

    public void SetVolume(float v) { MasterVolume = v; AudioListener.volume = v; PlayerPrefs.SetFloat(VolumeKey, v); }
    public void SetSensitivity(float v) { MouseSensitivity = v; PlayerPrefs.SetFloat(SensitivityKey, v); }
    public void SetSubtitleScale(float v) { SubtitleScale = v; PlayerPrefs.SetFloat(SubtitleKey, v); }
    public void SetInvertY(bool v) { InvertY = v; PlayerPrefs.SetInt(InvertKey, v ? 1 : 0); }
    public void SetQuality(int level) { QualitySettings.SetQualityLevel(level, true); PlayerPrefs.SetInt(QualityKey, level); }
}

// 职责：菜单设置与本地偏好存储；控件值只在用户操作时落盘，初次运行沿用项目默认值。
// 入口：Initialize() / ApplyToWorkshop()；音量、画质、全屏和镜头灵敏度可在Inspector配置。
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuSettings : MonoBehaviour
{
    private const string Prefix = "HY.Menu.";
    [SerializeField]
    private Slider volume;
    [SerializeField]
    private Slider sensitivity;
    [SerializeField]
    private Dropdown quality;
    [SerializeField]
    private Toggle fullscreen;
    private bool initialized;
    public void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        quality.ClearOptions();
        quality.AddOptions(QualitySettings.names.ToList());
        volume.SetValueWithoutNotify(Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", AudioListener.volume)));
        sensitivity.SetValueWithoutNotify(Mathf.Clamp(PlayerPrefs.GetFloat(Prefix + "Sensitivity", 2f), sensitivity.minValue, sensitivity.maxValue));
        quality.SetValueWithoutNotify(Mathf.Clamp(
            PlayerPrefs.GetInt(Prefix + "Quality", QualitySettings.GetQualityLevel()),
            0,
            QualitySettings.names.Length - 1));
        fullscreen.SetIsOnWithoutNotify(PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0);
        AudioListener.volume = volume.value;
        if (PlayerPrefs.HasKey(Prefix + "Quality"))
        {
            QualitySettings.SetQualityLevel(quality.value);
        }

        if (PlayerPrefs.HasKey(Prefix + "Fullscreen"))
        {
            Screen.fullScreen = fullscreen.isOn;
        }

        volume.onValueChanged.AddListener(SetVolume);
        sensitivity.onValueChanged.AddListener(SetSensitivity);
        quality.onValueChanged.AddListener(SetQuality);
        fullscreen.onValueChanged.AddListener(SetFullscreen);
    }

    public void ApplyToWorkshop()
    {
        if (CameraController.instance != null)
        {
            if (PlayerPrefs.HasKey(Prefix + "Sensitivity"))
            {
                CameraController.instance.mouseSensitivity = sensitivity.value;
            }
            else
            {
                sensitivity.SetValueWithoutNotify(CameraController.instance.mouseSensitivity);
            }
        }
    }

    private void SetVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(Prefix + "Volume", value);
        PlayerPrefs.Save();
    }

    private void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(Prefix + "Sensitivity", value);
        ApplyToWorkshop();
        PlayerPrefs.Save();
    }

    private void SetQuality(int value)
    {
        QualitySettings.SetQualityLevel(value);
        PlayerPrefs.SetInt(Prefix + "Quality", value);
        PlayerPrefs.Save();
    }

    private void SetFullscreen(bool value)
    {
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(Prefix + "Fullscreen", value ? 1 : 0);
        PlayerPrefs.Save();
    }
}

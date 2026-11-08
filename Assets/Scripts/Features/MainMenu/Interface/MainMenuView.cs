// 职责：主菜单原生uGUI展示与按钮绑定，导航及文件操作由MainMenuController协调。
// 入口：Bind() / Home() / Picker() / Settings() / SetChoices() / SetBusy()。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class MainMenuView : MonoBehaviour
{
    [SerializeField]
    private GameObject home;
    [SerializeField]
    private GameObject picker;
    [SerializeField]
    private GameObject settingsPanel;
    [SerializeField]
    private CanvasGroup controls;
    [SerializeField]
    private Text heading;
    [SerializeField]
    private Text details;
    [SerializeField]
    private Text status;
    [SerializeField]
    private Text recentLabel;
    [SerializeField]
    private Dropdown blueprints;
    [SerializeField]
    private Button exploration;
    [SerializeField]
    private Button siege;
    [SerializeField]
    private Button vehicleWorkshop;
    [SerializeField]
    private Button baseWorkshop;
    [SerializeField]
    private Button resume;
    [SerializeField]
    private Button recent;
    [SerializeField]
    private Button settingsButton;
    [SerializeField]
    private Button quit;
    [SerializeField]
    private Button launch;
    [SerializeField]
    private Button create;
    [SerializeField]
    private Button refresh;
    [SerializeField]
    private Button back;
    [SerializeField]
    private Button settingsBack;
    [SerializeField]
    private Button cancelNavigation;
    public void Bind(MainMenuController owner)
    {
        exploration.onClick.AddListener(() => owner.Choose(MenuDestination.Exploration));
        siege.onClick.AddListener(() => owner.Choose(MenuDestination.Siege));
        vehicleWorkshop.onClick.AddListener(() => owner.Choose(MenuDestination.VehicleWorkshop));
        baseWorkshop.onClick.AddListener(() => owner.Choose(MenuDestination.BaseWorkshop));
        resume.onClick.AddListener(owner.ResumeWorkshop);
        recent.onClick.AddListener(owner.OpenRecent);
        settingsButton.onClick.AddListener(Settings);
        quit.onClick.AddListener(owner.Quit);
        launch.onClick.AddListener(owner.LaunchSelected);
        create.onClick.AddListener(owner.CreateBlueprint);
        refresh.onClick.AddListener(owner.RefreshLibrary);
        back.onClick.AddListener(owner.ShowHome);
        settingsBack.onClick.AddListener(owner.ShowHome);
        cancelNavigation.onClick.AddListener(owner.CancelNavigation);
        blueprints.onValueChanged.AddListener(owner.SelectBlueprint);
    }

    public void Home(bool canResume, string recentName)
    {
        home.SetActive(true);
        picker.SetActive(false);
        settingsPanel.SetActive(false);
        resume.interactable = canResume;
        recent.interactable = !string.IsNullOrEmpty(recentName);
        recentLabel.text = string.IsNullOrEmpty(recentName) ? "No recent workshop" : "Recent: " + recentName;
        Message("Choose a mode to play, or open a workshop to create your own design.");
    }

    public void Picker(string title, bool editing)
    {
        home.SetActive(false);
        picker.SetActive(true);
        settingsPanel.SetActive(false);
        heading.text = title;
        create.gameObject.SetActive(editing);
    }

    public void Settings()
    {
        home.SetActive(false);
        picker.SetActive(false);
        settingsPanel.SetActive(true);
        Message("Settings are saved on this device.");
    }

    public void SetChoices(List<string> labels, int selected)
    {
        blueprints.ClearOptions();
        blueprints.AddOptions(labels);
        blueprints.SetValueWithoutNotify(selected);
        blueprints.RefreshShownValue();
        blueprints.interactable = labels.Count > 0;
    }

    public void Selection(string text, bool canLaunch, string action)
    {
        details.text = text;
        launch.interactable = canLaunch;
        launch.GetComponentInChildren<Text>().text = action;
    }

    public void Message(string text)
    {
        status.text = text;
    }

    public void SetBusy(bool busy)
    {
        controls.interactable = !busy;
        cancelNavigation.gameObject.SetActive(busy);
    }
}

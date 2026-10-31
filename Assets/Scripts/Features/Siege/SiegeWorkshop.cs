// 职责：在整备阶段复用 Main 蓝图编辑器，隔离已部署舰队与大型建筑，退出后恢复驾驶视图。
// 入口：Open() / Close()；只编辑存档预览，不免费替换战场机体或恢复耐久。
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class SiegeWorkshop : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField]
    private MainUIPanels panels;
    [SerializeField]
    private CanvasGroup strategyHud;
    [SerializeField]
    private Button buildButton;
    private SiegeWorkshopStage stage;
    public bool IsOpen
    {
        get; private set;
    }

    private void Awake()
    {
        session.BindWorkshop(this);
    }

    private void Start()
    {
        BuildManager.instance.enabled = false;
        SetPanels(false);
        buildButton.onClick.AddListener(OpenSelected);
        MainUIButtons.instance.playButton.GetComponentInChildren<Text>().text = "RETURN TO SIEGE  [P]";
        MainUIButtons.instance.exitButton.gameObject.SetActive(false);
    }

    private void Update()
    {
        buildButton.interactable = session.Match != null && session.Match.CanRefit && !IsOpen;
    }

    private void OpenSelected()
    {
        Open();
    }

    public bool Open(string saveName = null)
    {
        if (IsOpen || session.Match == null || !session.Match.CanRefit)
        {
            return false;
        }

        strategyHud.GetComponent<SiegeDeploymentView>()?.Cancel();
        IsOpen = true;
        stage = new SiegeWorkshopStage();
        try
        {
            stage.Open(gameObject.scene);
        }
        catch
        {
            stage.Dispose();
            stage = null;
            IsOpen = false;
            throw;
        }

        GameManager.instance.blocksParent = null;
        PlayManager.instance.blocksParent = null;
        ActionManager.instance.Clear();
        BuildManager.instance.enabled = true;
        BuildManager.instance.ExitDeveloperBlueprintBuildMode(false);
        InputManager.instance.keyInput = true;
        InputManager.instance.EnterBuildMode();
        SetPanels(true);
        SaveUIPanel.instance.RefreshList();
        if (SaveManager.instance.saves.Count > 0)
        {
            string requested = string.IsNullOrEmpty(saveName) ? SaveManager.instance.currentSaveName : saveName;
            string selected = !string.IsNullOrEmpty(requested) && SaveManager.instance.saves.Contains(requested) ? requested : SaveManager.instance.saves[0];
            SaveManager.instance.LoadSave(selected);
        }

        return true;
    }

    public bool Close()
    {
        if (!IsOpen)
        {
            return false;
        }

        BuildManager build = BuildManager.instance;
        build.CancelBlueprintLoad();
        build.DeselectBlock();
        build.SetBuildMode(false);
        build.enabled = false;
        ActionManager.instance.Clear();
        Transform preview = GameManager.instance.blocksParent;
        GameManager.instance.blocksParent = null;
        PlayManager.instance.blocksParent = null;
        if (preview != null)
        {
            preview.gameObject.SetActive(false);
            Destroy(preview.gameObject);
        }

        SaveManager.instance.blocks.Clear();
        stage?.Dispose();
        stage = null;
        IsOpen = false;
        InputManager.instance.keyInput = true;
        SetPanels(false);
        session.Fleet.RestoreView();
        session.RefreshBlueprints();
        session.NotifyRefitChanged();
        strategyHud.GetComponent<SiegeHud>()?.Refresh();
        return true;
    }

    private void SetPanels(bool editing)
    {
        panels.buildPanel.SetActive(editing);
        panels.playPanel.SetActive(false);
        panels.createPanel.SetActive(false);
        panels.deletePanel.SetActive(false);
        panels.deathPanel.SetActive(false);
        CanvasGroup group = panels.buildPanel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panels.buildPanel.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
        group.interactable = editing;
        group.blocksRaycasts = editing;
        group = panels.playPanel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = panels.playPanel.AddComponent<CanvasGroup>();
        }

        group.alpha = 1f;
        group.interactable = !editing;
        group.blocksRaycasts = !editing;
        strategyHud.alpha = editing ? 0f : 1f;
        strategyHud.interactable = !editing;
        strategyHud.blocksRaycasts = !editing;
    }

    private void OnDestroy()
    {
        buildButton?.onClick.RemoveListener(OpenSelected);
        stage?.Dispose();
        stage = null;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MainUIPanels : MonoBehaviour
{
    public static MainUIPanels instance;
    public GameObject buildPanel;
    public GameObject playPanel;
    public GameObject createPanel;
    public GameObject deletePanel;
    public GameObject debugPanel;
    public GameObject deathPanel;
    public InputField inputName;
    public InputField inputValue;
    [SerializeField] private Image _healthFill;
    public Text healthValue;
    public float fadeDuration = 0.3f;
    public Gradient healthBarColor;
    [SerializeField] private CombatHud _combatHud;
    [SerializeField] private ThrusterInfoPanel _thrusterInfoPanel;
    [SerializeField] private Text _flightTelemetry;
    public CombatHud CombatHud => _combatHud;
    public ThrusterInfoPanel ThrusterInfoPanel => _thrusterInfoPanel;
    private bool renameMode;
    private string renameTargetName;
    private UnityEngine.Events.UnityAction _deleteAction;
    private readonly System.Collections.Generic.Dictionary<GameObject, Coroutine> _transitions = new System.Collections.Generic.Dictionary<GameObject, Coroutine>();
    private float _nextHealthRefresh;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        //if (buildPanel != null)
        //{
        //    buildPanel.SetActive(true);
        //}
        //if (playPanel != null)
        //{
        //    playPanel.SetActive(false);
        //}
        //if (createPanel != null)
        //{
        //    createPanel.SetActive(false);
        //}
        //if (deletePanel != null)
        //{
        //    deletePanel.SetActive(false);
        //}
        //if (deathPanel != null)
        //{
        //    deathPanel.SetActive(false);
        //}
    }

    private void Update()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode || Time.unscaledTime < _nextHealthRefresh) return;
        _nextHealthRefresh = Time.unscaledTime + 0.2f;
        ControlUnit player = PlayManager.instance.blocksParent != null
            ? PlayManager.instance.blocksParent.GetComponent<ControlUnit>() : null;
        if (player != null && player.faction == UnitFaction.Player
            && player.TryGetTotalDurability(out float current, out float maximum))
            SetHealthBar(current, maximum);
        if (_flightTelemetry != null)
        {
            HoverFlightController controller = player != null ? player.hoverFlightController : null;
            _flightTelemetry.text = controller != null && controller.IsUsedByControlUnit
                ? $"TARGET HEIGHT   {controller.TargetHeight:0.00} m\nCURRENT HEIGHT  {controller.CurrentHeight:0.00} m\nHEIGHT P        {controller.HeightPValue:0.00}\nVERTICAL SPEED  {PlayManager.instance.verticalVelocity:0.00} m/s\nHORIZONTAL      {PlayManager.instance.horizontalVelocity:0.00} m/s"
                : "HOVER CONTROL   OFFLINE";
        }
    }

    private void Transition(GameObject panel, bool show)
    {
        if (panel == null) return;
        if (_transitions.TryGetValue(panel, out Coroutine running)) StopCoroutine(running);
        _transitions[panel] = StartCoroutine(Fade(panel, show));
    }

    private IEnumerator Fade(GameObject panel, bool show)
    {
        if (show)
        {
            panel.SetActive(true);
        }

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = panel.AddComponent<CanvasGroup>();
        }

        float start = cg.alpha;
        float end = show ? 1f : 0f;
        float t = 0f;

        cg.interactable = show;
        cg.blocksRaycasts = show;

        float duration = Mathf.Max(0f, fadeDuration);
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        cg.alpha = end;

        if (!show)
        {
            panel.SetActive(false);
        }
        _transitions.Remove(panel);
    }

    public void ShowCreatePanel()
    {
        renameMode = false;
        renameTargetName = string.Empty;
        Cursor.lockState = CursorLockMode.Confined;
        Transition(buildPanel, false);
        Transition(createPanel, true);
        BuildManager.instance.enabled = false;
        SetInputPlaceholder(BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint
            ? "Create new blueprint..."
            : "Create new save...");
        inputName.text = "";
        inputName.Select();
        inputName.ActivateInputField();
    }

    public void ShowRenamePanel(string save)
    {
        renameMode = true;
        renameTargetName = save;
        Cursor.lockState = CursorLockMode.Confined;
        Transition(buildPanel, false);
        Transition(createPanel, true);
        BuildManager.instance.enabled = false;
        SetInputPlaceholder(BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint
            ? "Rename blueprint..."
            : "Rename save...");
        inputName.text = save;
        inputName.Select();
        inputName.ActivateInputField();
    }

    public void HideCreatePanel()
    {
        renameMode = false;
        renameTargetName = string.Empty;
        Transition(createPanel, false);
        Transition(buildPanel, true);
        BuildManager.instance.enabled = true;
    }

    private void SetInputPlaceholder(string text)
    {
        Text placeholder = inputName.placeholder as Text;
        if (placeholder != null)
        {
            placeholder.text = text;
        }
    }

    public void ShowDeletePanel(string save)
    {
        if (_deleteAction != null) MainUIButtons.instance.confirmDeleteButton.onClick.RemoveListener(_deleteAction);
        _deleteAction = () => OnConfirmDelete(save);
        MainUIButtons.instance.confirmDeleteButton.onClick.AddListener(_deleteAction);
        Cursor.lockState = CursorLockMode.Confined;
        deletePanel.transform.Find("DeleteTextPanel").GetComponentInChildren<Text>().text = $"Are you sure you want to delete {save}?";
        Transition(buildPanel, false);
        Transition(deletePanel, true);
        BuildManager.instance.enabled = false;
    }

    public void HideDeletePanel()
    {
        Transition(deletePanel, false);
        Transition(buildPanel, true);
        BuildManager.instance.enabled = true;
        if (_deleteAction != null)
        {
            MainUIButtons.instance.confirmDeleteButton.onClick.RemoveListener(_deleteAction);
            _deleteAction = null;
        }
    }

    public void OnConfirmCreate()
    {
        string saveName = inputName.text.Trim();
        if (renameMode)
        {
            bool renamed = BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint
                ? SaveManager.instance.RenameEnemyBlueprint(renameTargetName, saveName)
                : SaveManager.instance.RenameSave(renameTargetName, saveName);

            if (renamed)
            {
                HideCreatePanel();
            }

            return;
        }

        if (!string.IsNullOrEmpty(saveName))
        {
            if (BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint)
            {
                SaveManager.instance.CreateNewEnemyBlueprint(saveName);
                SaveManager.instance.LoadEnemyBlueprint(saveName);
            }
            else
            {
                SaveManager.instance.CreateNewSave(saveName);
                SaveManager.instance.LoadSave(saveName);
            }
            
            HideCreatePanel();
        }
        else
        {
            Debug.LogWarning("Save name cannot be empty.");
        }
    }

    private void OnConfirmDelete(string save)
    {
        if (BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint)
        {
            SaveManager.instance.DeleteEnemyBlueprint(save);
        }
        else
        {
            SaveManager.instance.DeleteSave(save);
        }

        if (!string.IsNullOrEmpty(SaveManager.instance.currentSaveName))
        {
            SaveManager.instance.LoadSave(SaveManager.instance.currentSaveName);
        }
        HideDeletePanel();
    }

    public void PlayStart()
    {
        if (!PlayManager.instance.CanStartPlay(out string reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        debugPanel.transform.SetParent(playPanel.transform);

        _combatHud?.ResetHud();
        Transition(buildPanel, false);
        Transition(playPanel, true);
        PlayManager.instance.PlayStart();
    }

    public void PlayEnd()
    {
        if (!CargoPersistence.SaveReturnCargo(PlayManager.instance.playMode)) return;
        debugPanel.transform.SetParent(buildPanel.transform);

        PlayManager.instance.PlayEnd();
        Transition(deathPanel, false);
        Transition(playPanel, false);
        Transition(buildPanel, true);
    }

    public void PlayerDeath()
    {
        StopAllCoroutines();
        _transitions.Clear();
        PlayManager.instance.playMode = false;
        if (InputManager.instance != null)
        {
            InputManager.instance.EnterBuildMode();
        }

        // Death UI must accept the respawn click on the same frame as death; do not leave
        // interaction dependent on competing fade coroutines or a stale CanvasGroup state.
        SetPanelInteraction(deathPanel, true);
        if (MainUIButtons.instance != null && MainUIButtons.instance.respawnButton != null)
        {
            MainUIButtons.instance.respawnButton.interactable = true;
        }

        SetPanelInteraction(playPanel, false);
        if (playPanel != null)
        {
            playPanel.SetActive(false);
        }

        deathPanel.SetActive(true);
        CanvasGroup deathCanvasGroup = deathPanel.GetComponent<CanvasGroup>();
        deathCanvasGroup.alpha = 1f;
    }

    private void SetPanelInteraction(GameObject panel, bool interactable)
    {
        if (panel == null) return;

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    public void EnterEnemyBlueprintBuildMode()
    {
        MainUIButtons.instance.playButton.gameObject.SetActive(false);
        SaveUIPanel.instance.RefreshList();
    }

    public void ExitEnemyBlueprintBuildMode()
    {
        MainUIButtons.instance.playButton.gameObject.SetActive(true);
        SaveUIPanel.instance.RefreshList();
    }

    public void UpdateHealthBar(GameObject obj, float currentHealth, float maxHealth)
    {
        if (obj == null || obj.GetComponent<Cockpit>()?.faction != UnitFaction.Player) return;
        ControlUnit player = obj.GetComponentInParent<ControlUnit>();
        if (player != null && player.TryGetTotalDurability(out float current, out float maximum))
            SetHealthBar(current, maxHealth);
    }

    private void SetHealthBar(float currentHealth, float maxHealth)
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / PlayManager.instance.maxHealth) : 0f;
        if (_healthFill != null)
        {
            _healthFill.fillAmount = ratio;
            _healthFill.color = healthBarColor.Evaluate(ratio);
        }
        if (healthValue != null) healthValue.text = $"{Mathf.Max(0f, currentHealth):0} / {Mathf.Max(0f, PlayManager.instance.maxHealth):0}";
    }
}

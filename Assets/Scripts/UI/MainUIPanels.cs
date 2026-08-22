using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainUIPanels : MonoBehaviour
{
    public static MainUIPanels instance;
    public GameObject buildPanel;
    public GameObject playPanel;
    public GameObject createPanel;
    public GameObject deletePanel;
    public GameObject deathPanel;
    public InputField inputName;
    public InputField inputValue;
    public Scrollbar healthBar;
    public Text healthValue;
    public float fadeDuration = 0.3f;
    public Gradient healthBarColor;
    private bool renameMode;
    private string renameTargetName;

    private void Awake()
    {
        instance = this;
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

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / fadeDuration);
            yield return null;
        }

        cg.alpha = end;

        if (!show)
        {
            panel.SetActive(false);
        }
    }

    public void ShowCreatePanel()
    {
        renameMode = false;
        renameTargetName = string.Empty;
        Cursor.lockState = CursorLockMode.Confined;
        StartCoroutine(Fade(buildPanel, false));
        StartCoroutine(Fade(createPanel, true));
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
        StartCoroutine(Fade(buildPanel, false));
        StartCoroutine(Fade(createPanel, true));
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
        StartCoroutine(Fade(createPanel, false));
        StartCoroutine(Fade(buildPanel, true));
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
        MainUIButtons.instance.confirmDeleteButton.onClick.AddListener(() => OnConfirmDelete(save));
        Cursor.lockState = CursorLockMode.Confined;
        deletePanel.transform.Find("DeleteTextPanel").GetComponentInChildren<Text>().text = $"Are you sure you want to delete {save}?";
        StartCoroutine(Fade(buildPanel, false));
        StartCoroutine(Fade(deletePanel, true));
        BuildManager.instance.enabled = false;
    }

    public void HideDeletePanel()
    {
        StartCoroutine(Fade(deletePanel, false));
        StartCoroutine(Fade(buildPanel, true));
        BuildManager.instance.enabled = true;
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

        StartCoroutine(Fade(buildPanel, false));
        StartCoroutine(Fade(playPanel, true));
        PlayManager.instance.PlayStart();
    }

    public void PlayEnd()
    {
        PlayManager.instance.PlayEnd();
        StartCoroutine(Fade(deathPanel, false));
        StartCoroutine(Fade(playPanel, false));
        StartCoroutine(Fade(buildPanel, true));
    }

    public void PlayerDeath()
    {
        PlayManager.instance.playMode = false;
        if (InputManager.instance != null)
        {
            InputManager.instance.EnterBuildMode();
        }
        StartCoroutine(Fade(playPanel, false));
        StartCoroutine(Fade(deathPanel, true));
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
        if (obj.GetComponent<Cockpit>()?.faction == UnitFaction.Player)
        {
            healthBar.value = currentHealth / maxHealth;
            healthBar.transform.Find("Sliding Area/Handle").GetComponent<Image>().color = healthBarColor.Evaluate(healthBar.value);
            healthValue.text = $"{currentHealth} / {maxHealth}";
        }
    }
}

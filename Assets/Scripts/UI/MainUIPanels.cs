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
    public InputField inputName;
    public float fadeDuration = 0.3f;

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
        Cursor.lockState = CursorLockMode.Confined;
        StartCoroutine(Fade(buildPanel, false));
        StartCoroutine(Fade(createPanel, true));
        BuildManager.instance.enabled = false;
        inputName.text = "";
    }

    public void HideCreatePanel()
    {
        StartCoroutine(Fade(createPanel, false));
        StartCoroutine(Fade(buildPanel, true));
        BuildManager.instance.enabled = true;
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
        if (!string.IsNullOrEmpty(saveName))
        {
            SaveManager.instance.CreateNewSave(saveName);
            SaveManager.instance.LoadSave(saveName);
            HideCreatePanel();
        }
        else
        {
            Debug.LogWarning("Save name cannot be empty.");
        }
    }

    private void OnConfirmDelete(string save)
    {
        SaveManager.instance.DeleteSave(save);
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
        StartCoroutine(Fade(playPanel, false));
        StartCoroutine(Fade(buildPanel, true));
    }

    public void EnterEnemyBlueprintBuildMode()
    {
        MainUIButtons.instance.playButton.gameObject.SetActive(false);
    }

    public void ExitEnemyBlueprintBuildMode()
    {
        MainUIButtons.instance.playButton.gameObject.SetActive(true);
    }
}

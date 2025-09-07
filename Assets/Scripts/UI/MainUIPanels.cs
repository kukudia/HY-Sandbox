using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MainUIPanels : MonoBehaviour
{
    public static MainUIPanels instance;
    public GameObject buildPanel;
    public GameObject playPanel;
    public GameObject createPanel;
    public GameObject deletePanel;
    public InputField inputName; // 输入框
    public float fadeDuration = 0.3f; // 淡入淡出时长

    private void Awake()
    {
        instance = this;
    }

    // 通用淡入淡出方法
    private IEnumerator Fade(GameObject panel, bool show)
    {
        if (show)
            panel.SetActive(true);

        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = panel.AddComponent<CanvasGroup>();

        float start = cg.alpha;
        float end = show ? 1f : 0f;
        float t = 0f;

        cg.interactable = show;
        cg.blocksRaycasts = show;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // UI 用 unscaledDeltaTime 更自然
            cg.alpha = Mathf.Lerp(start, end, t / fadeDuration);
            yield return null;
        }

        cg.alpha = end;

        if (!show)
            panel.SetActive(false);
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
        string name = inputName.text.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            SaveManager.instance.CreateNewSave(name);
            SaveManager.instance.LoadSave(name);
            HideCreatePanel();
        }
        else
        {
            Debug.LogWarning("存档名字不能为空");
        }
    }

    void OnConfirmDelete(string save)
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
}

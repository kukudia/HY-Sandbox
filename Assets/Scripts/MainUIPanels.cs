using UnityEngine;
using UnityEngine.UI;

public class MainUIPanels : MonoBehaviour
{
    public GameObject buildPanel;
    public GameObject createPanel;
    public InputField inputName; // 输入框
    private MainUIButtons mainUIButtons;


    private void Start()
    {
        //mainUIButtons = GetComponent<MainUIButtons>();
        createPanel.SetActive(false); // 初始隐藏
    }

    public void ShowCreatePanel()
    {
        buildPanel.SetActive(false);
        createPanel.SetActive(true);
        inputName.text = "";
    }

    public void HideCreatePanel()
    {
        createPanel.SetActive(false);
        buildPanel.SetActive(true);
        BuildManager.instance.enabled = true;
    }

    public void OnConfirm()
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
            Debug.LogWarning("存档名字不能为空！");
        }
    }

    public void OnCancel()
    {
        HideCreatePanel();
    }
}

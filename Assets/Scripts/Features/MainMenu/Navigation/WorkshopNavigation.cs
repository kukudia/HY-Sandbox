// 职责：Main工坊的显式菜单返回入口，兼容直接运行Main的开发流程。
// 入口：OpenMenu()；菜单已加载时保持唯一EventSystem，Escape与按钮共用返回主链。
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class WorkshopNavigation : MonoBehaviour
{
    public const string MenuPath = "Assets/Scenes/MainMenu.unity";
    [SerializeField]
    private EventSystem workshopEvents;
    [SerializeField]
    private Button menuButton;
    [SerializeField]
    private Text notice;
    private bool opening;
    public bool MenuHosted => SceneManager.GetSceneByPath(MenuPath).isLoaded;

    private void Awake()
    {
        if (MenuHosted)
        {
            UseMenuEvents();
        }

        menuButton.onClick.AddListener(OpenMenu);
    }

    public void UseMenuEvents()
    {
        workshopEvents.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // A direct-Main load can park this root before its loading coroutine resumes.
        if (MenuHosted)
        {
            opening = false;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (opening)
        {
            return;
        }

        ShowNotice(string.Empty);
        if (MenuHosted)
        {
            SceneObjectQuery.FindInScene<MainMenuController>(SceneManager.GetSceneByPath(MenuPath)).ReturnToMenu();
        }
        else
        {
            StartCoroutine(LoadMenu());
        }
    }

    public void ShowNotice(string message)
    {
        notice.text = message;
        notice.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private IEnumerator LoadMenu()
    {
        opening = true;
        yield return SceneManager.LoadSceneAsync(MenuPath, LoadSceneMode.Additive);
        UseMenuEvents();
        opening = false;
    }

    private void OnDestroy()
    {
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OpenMenu);
        }
    }
}

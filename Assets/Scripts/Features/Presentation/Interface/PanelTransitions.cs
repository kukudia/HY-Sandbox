// 职责：控制面板淡入淡出、交互状态和取消；不参与游戏模式及存档决策。
// 入口：Show() / CancelAll()；由 MainUIPanels 注入协程宿主，禁用时取消。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class PanelTransitions
{
    private readonly MonoBehaviour _runner;
    private readonly Dictionary<GameObject, Coroutine> _transitions = new Dictionary<GameObject, Coroutine>();
    public PanelTransitions(MonoBehaviour runner)
    {
        _runner = runner;
    }

    public void CancelAll()
    {
        foreach (Coroutine routine in _transitions.Values)
        {
            if (_runner != null)
            {
                _runner.StopCoroutine(routine);
            }
        }

        _transitions.Clear();
    }

    public void Show(GameObject panel, bool show, float duration)
    {
        if (panel == null)
        {
            return;
        }

        if (_transitions.TryGetValue(panel, out Coroutine running))
        {
            _runner.StopCoroutine(running);
        }

        _transitions.Remove(panel);
        if (duration <= 0f)
        {
            panel.SetActive(show);
            SetInteraction(panel, show);
            panel.GetComponent<CanvasGroup>().alpha = show ? 1f : 0f;
            return;
        }

        _transitions[panel] = _runner.StartCoroutine(Fade(panel, show, duration));
    }

    private IEnumerator Fade(GameObject panel, bool show, float duration)
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
        while (t < duration && panel != null && cg != null)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        if (panel == null || cg == null)
        {
            _transitions.Remove(panel);
            yield break;
        }

        cg.alpha = end;
        if (!show)
        {
            panel.SetActive(false);
        }

        _transitions.Remove(panel);
    }

    public static void SetInteraction(GameObject panel, bool interactable)
    {
        if (panel == null)
        {
            return;
        }

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }
}

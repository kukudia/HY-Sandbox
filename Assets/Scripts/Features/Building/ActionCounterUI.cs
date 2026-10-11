// 职责：展示建造动作历史计数及 Undo/Redo 状态。
// 入口：UpdateUndoText() / UpdateRedoText()。
using UnityEngine;
using UnityEngine.UI;

public class ActionCounterUI : MonoBehaviour
{
    public static ActionCounterUI instance;
    public GameObject undoTextParent;
    public GameObject redoTextParent;
    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        UpdateUndoText(ActionManager.instance.GetUndoCount());
        UpdateRedoText(ActionManager.instance.GetRedoCount());
    }

    public void UpdateUndoText(int count)
    {
        if (count <= 0)
        {
            undoTextParent.SetActive(false);
        }
        else
        {
            undoTextParent.SetActive(true);
        }

        Text undoText = undoTextParent.GetComponentInChildren<Text>();
        undoText.text = count.ToString();
    }

    public void UpdateRedoText(int count)
    {
        if (count <= 0)
        {
            redoTextParent.SetActive(false);
        }
        else
        {
            redoTextParent.SetActive(true);
        }

        Text redoText = redoTextParent.GetComponentInChildren<Text>();
        redoText.text = count.ToString();
    }
}

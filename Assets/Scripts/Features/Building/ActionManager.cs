// 职责：管理建造动作的 Undo/Redo 栈与操作计数。
// 入口：Push() / Undo() / Redo()。
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理建造动作的撤销、重做和计数。
/// </summary>
public class ActionManager : MonoBehaviour
{
    public static ActionManager instance;
    private Stack<IBlockAction> undoStack = new Stack<IBlockAction>();
    private Stack<IBlockAction> redoStack = new Stack<IBlockAction>();
    // 各动作的历史计数。
    private Dictionary<string, int> actionCounter = new Dictionary<string, int>();
    private int totalActionCount = 0; // 历史累计次数。
    private void Awake()
    {
        instance = this;
    }

    /// <summary>
     // 当前 Redo 栈大小。
    /// </summary>
    public void Push(IBlockAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();
        CountAction(action);
        ShowDebug();
    }

    /// <summary>
    /// 记录新动作并清空重做栈。
    /// </summary>
    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            var action = undoStack.Pop();
            action.Undo();
            redoStack.Push(action);
        }
    }

    /// <summary>
    /// 撤销最近动作。
    /// </summary>
    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            var action = redoStack.Pop();
            action.Redo();
            undoStack.Push(action);
        }
    }

    public void Clear()
    {
        undoStack.Clear();
        redoStack.Clear();
    }

    /// <summary>
    /// 重做最近撤销的动作。
    /// </summary>
    private void CountAction(IBlockAction action)
    {
        totalActionCount++;
        string name = action.ActionName;
        if (!actionCounter.ContainsKey(name))
        {
            actionCounter[name] = 0;
        }

        actionCounter[name]++;
    }

    /// <summary>
    /// 累计动作次数。
    /// </summary>
    public int GetTotalActionCount() => totalActionCount;
    /// <summary>
    /// 读取全部动作的历史计数。
    /// </summary>
    public int GetActionCount(string actionName)
    {
        return actionCounter.ContainsKey(actionName) ? actionCounter[actionName] : 0;
    }

    public void ShowDebug()
    {
        Debug.Log($"总操作数: {GetTotalActionCount()} " + $"创建: {GetActionCount("Create")} " + $"删除: {GetActionCount("Delete")} " + $"Undo ջ: {GetUndoCount()} " + $"Redo ջ: {GetRedoCount()}");
    }

    // 当前 Undo 栈大小。
    public int GetUndoCount() => undoStack.Count;
    // 当前 Redo 栈大小。
    public int GetRedoCount() => redoStack.Count;
}

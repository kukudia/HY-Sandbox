using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 管理所有方块动作（Undo / Redo）
/// </summary>
public class ActionManager : MonoBehaviour
{
    public static ActionManager instance;

    private Stack<IBlockAction> undoStack = new Stack<IBlockAction>();
    private Stack<IBlockAction> redoStack = new Stack<IBlockAction>();

    // 动作计数器
    private Dictionary<string, int> actionCounter = new Dictionary<string, int>();
    private int totalActionCount = 0; // 历史累计

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 压入一个新动作（清空 Redo 栈）
    /// </summary>
    public void Push(IBlockAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();
        CountAction(action);
        ShowDebug();
    }

    /// <summary>
    /// 撤销
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
    /// 重做
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

    /// <summary>
    /// 统计动作
    /// </summary>
    private void CountAction(IBlockAction action)
    {
        totalActionCount++;
        string name = action.ActionName;
        if (!actionCounter.ContainsKey(name))
            actionCounter[name] = 0;
        actionCounter[name]++;
    }

    /// <summary>
    /// 获取动作总数（累计）
    /// </summary>
    public int GetTotalActionCount() => totalActionCount;

    /// <summary>
    /// 获取某种类型动作数
    /// </summary>
    public int GetActionCount(string actionName)
    {
        return actionCounter.ContainsKey(actionName) ? actionCounter[actionName] : 0;
    }

    public void ShowDebug()
    {
        Debug.Log($"总操作数: {GetTotalActionCount()} " +
            $"放置方块: {GetActionCount("Create")} " +
            $"删除方块: {GetActionCount("Delete")} " +
            $"Undo 栈: {GetUndoCount()} " +
            $"Redo 栈: {GetRedoCount()}");
    }

    // 当前 Undo 栈大小
    public int GetUndoCount() => undoStack.Count;

    // 当前 Redo 栈大小
    public int GetRedoCount() => redoStack.Count;
}

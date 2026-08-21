using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �������з��鶯����Undo / Redo��
/// </summary>
public class ActionManager : MonoBehaviour
{
    public static ActionManager instance;

    private Stack<IBlockAction> undoStack = new Stack<IBlockAction>();
    private Stack<IBlockAction> redoStack = new Stack<IBlockAction>();

    // ����������
    private Dictionary<string, int> actionCounter = new Dictionary<string, int>();
    private int totalActionCount = 0; // ��ʷ�ۼ�

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// ѹ��һ���¶�������� Redo ջ��
    /// </summary>
    public void Push(IBlockAction action)
    {
        undoStack.Push(action);
        redoStack.Clear();
        CountAction(action);
        ShowDebug();
    }

    /// <summary>
    /// ����
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
    /// ����
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
    /// ͳ�ƶ���
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
    /// ��ȡ�����������ۼƣ�
    /// </summary>
    public int GetTotalActionCount() => totalActionCount;

    /// <summary>
    /// ��ȡĳ�����Ͷ�����
    /// </summary>
    public int GetActionCount(string actionName)
    {
        return actionCounter.ContainsKey(actionName) ? actionCounter[actionName] : 0;
    }

    public void ShowDebug()
    {
        Debug.Log($"�ܲ�����: {GetTotalActionCount()} " +
            $"���÷���: {GetActionCount("Create")} " +
            $"ɾ������: {GetActionCount("Delete")} " +
            $"Undo ջ: {GetUndoCount()} " +
            $"Redo ջ: {GetRedoCount()}");
    }

    // ��ǰ Undo ջ��С
    public int GetUndoCount() => undoStack.Count;

    // ��ǰ Redo ջ��С
    public int GetRedoCount() => redoStack.Count;
}

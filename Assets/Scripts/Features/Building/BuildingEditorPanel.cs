// 职责：建筑编辑的可编辑 uGUI 面板，提交底座整数尺寸并显示结果。
// 入口：Refresh() / ApplySize()；Prefab 由 BuildingEditorAssets 制作，不改动主场景手工布局。
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildingEditorPanel : MonoBehaviour
{
    [SerializeField]
    private InputField _width;
    [SerializeField]
    private InputField _length;
    [SerializeField]
    private Text _status;
    public void Refresh()
    {
        BuildingFoundationData data = SaveManager.instance?.cachedData?.foundation;
        _width.text = (data?.width ?? BuildingFoundationData.DefaultSize).ToString();
        _length.text = (data?.length ?? BuildingFoundationData.DefaultSize).ToString();
        _status.text = "1-64 cells per side. Changes are saved automatically.";
    }

    public void ApplySize()
    {
        if (!int.TryParse(_width.text, out int width) || !int.TryParse(_length.text, out int length))
        {
            _status.text = "Enter whole numbers from 1 to 64.";
            return;
        }

        try
        {
            _status.text = BuildManager.instance.ResizeBuildingFoundation(width, length, out string reason) ? $"Saved {width} x {length}. Existing blocks keep their positions." : reason;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            _status.text = $"Could not save: {exception.Message}";
        }
    }
}

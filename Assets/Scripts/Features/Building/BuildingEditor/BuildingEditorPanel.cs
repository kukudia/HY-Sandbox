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
    [SerializeField]
    private GameObject _outpostControls;
    [SerializeField]
    private Text _title;
    private float _fullHeight;
    public void Refresh()
    {
        bool baseEditor = BuildManager.instance.IsEditingBaseBlueprint;
        if (_title != null)
        {
            _title.text = baseEditor ? "BASE  |  Foundation size" : "BUILDING  |  Ctrl+Alt+F to exit";
        }

        RectTransform rect = (RectTransform)transform;
        if (_fullHeight == 0)
        {
            _fullHeight = rect.sizeDelta.y;
        }

        if (_outpostControls != null)
        {
            _outpostControls.SetActive(!baseEditor);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, baseEditor ? 142f : _fullHeight);
        }

        BuildingOutpostEditor outpost = GetComponent<BuildingOutpostEditor>();
        if (outpost != null)
        {
            outpost.enabled = !baseEditor;
        }

        BuildingFoundationData data = SaveManager.instance?.cachedData?.foundation;
        _width.text = (data?.width ?? BuildingFoundationData.DefaultSize).ToString();
        _length.text = (data?.length ?? BuildingFoundationData.DefaultSize).ToString();
        _status.text = "1-64 cells per side. Changes are saved automatically.";
        if (!baseEditor)
        {
            outpost?.Refresh();
        }
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

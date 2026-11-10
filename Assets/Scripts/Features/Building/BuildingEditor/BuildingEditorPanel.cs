// 职责：显示自动占地和地面自由建造提示，建筑哨站控件独立保留。
// 入口：Refresh()；面板没有手动地基尺寸输入，实际范围在加载、退出编辑或明确完成时计算。
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildingEditorPanel : MonoBehaviour
{
    [SerializeField]
    private Text _status;
    [SerializeField]
    private GameObject _outpostControls;
    [SerializeField]
    private Text _title;
    private float _fullHeight;
    public void Complete()
    {
        try
        {
            BuildManager.instance.CompleteBuildingBlueprint();
            Refresh();
        }
        catch (System.IO.IOException error)
        {
            _status.text = "Could not save footprint: " + error.Message;
        }
        catch (System.UnauthorizedAccessException error)
        {
            _status.text = "Could not save footprint: " + error.Message;
        }
    }

    public void Refresh()
    {
        bool baseEditor = BuildManager.instance.IsEditingBaseBlueprint;
        if (_title != null)
        {
            _title.text = baseEditor ? "BASE  |  Free ground building" : "BUILDING  |  Free ground building";
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
        string footprint = data == null ? "Empty design" : $"Footprint: {data.width} x {data.length} m  |  origin ({data.MinX:0}, {data.MinZ:0})";
        _status.text = "Build freely. Crop on load, finish or exit.\n" + footprint;
        if (!baseEditor)
        {
            outpost?.Refresh();
        }
    }
}

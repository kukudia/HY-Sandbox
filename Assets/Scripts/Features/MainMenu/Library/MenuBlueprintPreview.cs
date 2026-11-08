// 职责：使用现有静态模块预览展示菜单蓝图；独立渲染层不会创建控制单元或参与战斗。
// 入口：Show() / Clear()；所有相机、灯光和预览挂点均保存在MainMenu场景。
using UnityEngine;

public sealed class MenuBlueprintPreview : MonoBehaviour
{
    [SerializeField]
    private Transform anchor;
    [SerializeField]
    private Camera previewCamera;
    [SerializeField, Range(0, 31)]
    private int previewLayer = 30;
    private GameObject model;
    public void Show(BlockDataList data)
    {
        Clear();
        if (data == null || data.blocks.Count == 0)
        {
            return;
        }

        model = BlueprintModelPreview.Create(data.blocks, anchor, "Blueprint preview");
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        bool first = true;
        foreach (Transform item in model.GetComponentsInChildren<Transform>(true))
        {
            item.gameObject.layer = previewLayer;
        }

        foreach (Renderer item in renderers)
        {
            if (first)
            {
                bounds = item.bounds;
                first = false;
            }
            else
            {
                bounds.Encapsulate(item.bounds);
            }
        }

        previewCamera.orthographicSize = Mathf.Max(4f, bounds.extents.magnitude * 1.25f);
        previewCamera.transform.position = bounds.center + new Vector3(1f, .85f, -1f).normalized * Mathf.Max(100f, bounds.size.magnitude * 2f);
        previewCamera.transform.LookAt(bounds.center);
        previewCamera.farClipPlane = Mathf.Max(500f, bounds.size.magnitude * 5f);
    }

    public void Clear()
    {
        if (model != null)
        {
            model.SetActive(false);
            Destroy(model);
            model = null;
        }
    }
}

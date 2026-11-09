// 职责：只显示面向镜头的已选机库尺寸控件；保持背面按钮不拦截点击。
// 入口：ConfigureGeometry() / LateUpdate()。
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public sealed class HangarFacePanel : MonoBehaviour
{
    [SerializeField]
    private HangarModule hangar;
    [SerializeField]
    private Vector3 outwardNormal = Vector3.forward;
    private CanvasGroup group;
    public void ConfigureGeometry(Vector3 outer)
    {
        transform.localPosition = Vector3.Scale(outwardNormal, outer * 0.5f + Vector3.one * 0.08f);
        transform.localRotation = Quaternion.LookRotation(-outwardNormal, Vector3.up);
        transform.localScale = Vector3.one * Mathf.Min(0.012f, Mathf.Max(0.004f, Mathf.Min(outer.x, outer.z) / 320f));
    }

    private void LateUpdate()
    {
        if (group == null)
        {
            group = GetComponent<CanvasGroup>();
        }

        Camera camera = Camera.main;
        bool visible = hangar != null && !hangar.IsCombat && camera != null && BuildManager.instance != null && BuildManager.instance.selectedBlock == hangar.GetComponent<Block>() && Vector3.Dot(hangar.transform.TransformDirection(outwardNormal), camera.transform.position - transform.position) > 0f;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.worldCamera != camera)
        {
            canvas.worldCamera = camera;
        }
    }
}

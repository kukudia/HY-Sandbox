// 职责：在HUD边缘标出不在视野内的来袭敌舰，给出方向与距离，夜色场景里仍能读到威胁。
// 入口：Start()建立有界标记池，Update()按间隔刷新；只读ControlUnit公开状态，不参与规则或物理。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeThreatMarkers : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField, Tooltip("用于投影的相机；留空时使用Camera.main。")]
    private Camera worldCamera;
    [SerializeField, Min(1), Tooltip("制作期标记数量；调整后执行 Bake Editable Components，运行时使用已保存的子对象。")]
    private int poolSize = 6;
    [SerializeField, Min(0f), Tooltip("标记离屏幕边缘的像素距离。")]
    private float edgeMargin = 96f;
    [SerializeField, Min(0.02f)]
    private float updateInterval = 0.1f;
    [SerializeField, Min(0f), Tooltip("超过该距离的敌舰即使入镜也标记，避免夜色里丢失目标。")]
    private float alwaysMarkBeyond = 90f;
    [SerializeField]
    private Color markerColor = new Color(1f, 0.46f, 0.22f, 0.92f);
    [SerializeField]
    private Color labelColor = new Color(1f, 0.72f, 0.55f, 0.92f);
    [SerializeField]
    private Vector2 markerSize = new Vector2(18f, 18f);
    [SerializeField, Min(8)]
    private int labelSize = 16;
    public int AuthoredPoolSize => poolSize;
    public int AuthoredLabelSize => labelSize;

    private readonly List<RectTransform> markers = new List<RectTransform>();
    private readonly List<Text> labels = new List<Text>();
    private readonly List<int> shownDistances = new List<int>();
    private RectTransform canvasRect;
    private float nextRefresh;
    private void Awake()
    {
        canvasRect = transform as RectTransform;
        Build();
    }

    private void Start()
    {
        if (session == null)
        {
            session = SceneObjectQuery.FindInScene<SiegeSession>(gameObject.scene);
        }
    }

    /// <summary>绑定已保存的标记池；制作参数变更后通过 EditableComponentBaker 生成缺少的标记。</summary>
    private void Build()
    {
        if (markers.Count > 0 || canvasRect == null)
        {
            return;
        }

        foreach (Transform child in canvasRect)
        {
            if (!child.name.StartsWith("Threat Marker "))
            {
                continue;
            }

            var rect = child as RectTransform;
            Text label = child.GetComponentInChildren<Text>(true);
            if (rect == null || label == null)
            {
                continue;
            }

            markers.Add(rect);
            labels.Add(label);
            shownDistances.Add(-1);
            child.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (session == null || session.Match == null || markers.Count == 0 || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + Mathf.Max(0.02f, updateInterval);
        Camera camera = worldCamera != null ? worldCamera : Camera.main;
        if (camera == null)
        {
            HideAll();
            return;
        }

        int used = 0;
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (used >= markers.Count)
            {
                break;
            }

            if (unit == null || unit.faction != UnitFaction.Enemy || unit.cockpit == null || !unit.cockpit.isActiveAndEnabled)
            {
                continue;
            }

            Vector3 world = unit.cockpit.transform.position;
            Vector3 viewport = camera.WorldToViewportPoint(world);
            float distance = Vector3.Distance(camera.transform.position, world);
            bool onScreen = viewport.z > 0f && viewport.x > 0.06f && viewport.x < 0.94f && viewport.y > 0.06f && viewport.y < 0.94f && distance <= alwaysMarkBeyond;
            if (onScreen)
            {
                continue;
            }

            PlaceMarker(used, camera, world, viewport, distance);
            used++;
        }

        for (int index = used; index < markers.Count; index++)
        {
            if (markers[index].gameObject.activeSelf)
            {
                markers[index].gameObject.SetActive(false);
            }
        }
    }

    /// <summary>把世界坐标压到屏幕边缘：视野内但过远时按真实方向，视野外时按画布方向夹取。</summary>
    private void PlaceMarker(int index, Camera camera, Vector3 world, Vector3 viewport, float distance)
    {
        RectTransform marker = markers[index];
        if (!marker.gameObject.activeSelf)
        {
            marker.gameObject.SetActive(true);
        }

        Vector2 screenPoint = camera.WorldToScreenPoint(world);
        // 覆盖层画布与屏幕分辨率不同（CanvasScaler），必须先把屏幕点换算到画布本地坐标再夹取。
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out Vector2 local))
        {
            marker.gameObject.SetActive(false);
            return;
        }

        if (viewport.z <= 0f)
        {
            // 目标在相机后方时方向翻转，否则标记会挤在错误的一侧。
            local = -local;
        }

        if (local.sqrMagnitude < 0.0001f)
        {
            local = Vector2.up;
        }

        Vector2 half = canvasRect.rect.size * 0.5f - new Vector2(edgeMargin, edgeMargin);
        float scale = Mathf.Min(half.x / Mathf.Max(0.001f, Mathf.Abs(local.x)), half.y / Mathf.Max(0.001f, Mathf.Abs(local.y)));
        scale = Mathf.Min(1f, scale);
        marker.anchoredPosition = local * scale;
        int rounded = Mathf.Max(1, Mathf.RoundToInt(distance));
        if (shownDistances[index] != rounded)
        {
            shownDistances[index] = rounded;
            labels[index].text = rounded + " m";
        }
    }

    private void HideAll()
    {
        foreach (RectTransform marker in markers)
        {
            if (marker.gameObject.activeSelf)
            {
                marker.gameObject.SetActive(false);
            }
        }
    }
}

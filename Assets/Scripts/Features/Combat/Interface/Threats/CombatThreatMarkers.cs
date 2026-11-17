// 职责：对共享HUD候选敌舰逐帧投影，稳定绑定屏内定位框/屏外箭头并合并同向威胁。
// 入口：Clear()；LateUpdate在镜头和CombatHud之后运行，文字独立限频。
using UnityEngine;

[DefaultExecutionOrder(220)]
public sealed class CombatThreatMarkers : MonoBehaviour
{
    [SerializeField]
    private CombatHud hud;
    [SerializeField]
    private CombatThreatView[] markers;
    [SerializeField]
    private Vector2 safeMargin = new Vector2(105f, 105f);
    [SerializeField, Min(0f)]
    private float clusterDistance = 64f;
    public int VisibleCount
    {
        get; private set;
    }

    public void Clear()
    {
        VisibleCount = 0;
        foreach (CombatThreatView marker in markers)
        {
            marker.Clear();
        }
    }

    private void OnDisable()
    {
        Clear();
    }

    private void LateUpdate()
    {
        if (hud == null || !hud.IsBattleActive || hud.WorldCamera == null)
        {
            Clear();
            return;
        }

        VisibleCount = 0;
        RectTransform root = (RectTransform)transform;
        Vector2 half = Vector2.Max(Vector2.one, root.rect.size * .5f - safeMargin);
        foreach (CombatThreatView marker in markers)
        {
            marker.BeginFrame();
        }

        for (int candidateIndex = 0; candidateIndex < hud.Candidates.Count; candidateIndex++)
        {
            ControlUnit unit = hud.Candidates[candidateIndex];
            if (unit == null || unit.faction != UnitFaction.Enemy || !unit.HasValidCockpit)
            {
                continue;
            }

            Camera camera = hud.WorldCamera;
            Vector3 viewport = camera.WorldToViewportPoint(unit.cockpit.transform.position);
            bool inView = viewport.z > 0f && viewport.x > .05f && viewport.x < .95f && viewport.y > .07f && viewport.y < .93f;
            if (inView && hud.HasVisibleNameplate(unit))
            {
                continue;
            }

            // Camera-local XY remains continuous across the near plane; perspective division would flip it.
            Vector3 direction = camera.transform.InverseTransformPoint(unit.cockpit.transform.position);
            Vector2 local;
            if (inView)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(root, camera.WorldToScreenPoint(unit.cockpit.transform.position), null, out local);
            }
            else
            {
                local = new Vector2(direction.x, direction.y);
                if (local.sqrMagnitude < .001f)
                {
                    local = Vector2.down;
                }

                float scale = Mathf.Min(half.x / Mathf.Max(.001f, Mathf.Abs(local.x)), half.y / Mathf.Max(.001f, Mathf.Abs(local.y)));
                local *= scale;
            }

            CombatThreatView clustered = null;
            foreach (CombatThreatView marker in markers)
            {
                if (marker.Used && marker.Offscreen && !inView && (marker.Position - local).sqrMagnitude < clusterDistance * clusterDistance)
                {
                    clustered = marker;
                    break;
                }
            }

            if (clustered != null)
            {
                clustered.AddCluster();
                continue;
            }

            CombatThreatView selected = null;
            foreach (CombatThreatView marker in markers)
            {
                if (!marker.Used && marker.Owner == unit.cockpit)
                {
                    selected = marker;
                    break;
                }
            }

            if (selected == null)
            {
                foreach (CombatThreatView marker in markers)
                {
                    if (!marker.Used && (marker.Owner == null || !ContainsCandidate(marker.Owner)))
                    {
                        selected = marker;
                        break;
                    }
                }
            }

            if (selected == null)
            {
                foreach (CombatThreatView marker in markers)
                {
                    if (!marker.Used)
                    {
                        selected = marker;
                        break;
                    }
                }
            }

            if (selected != null && VisibleCount < hud.Theme.maximumMarkers)
            {
                selected.Place(
                    unit.cockpit,
                    local,
                    !inView,
                    direction.z < 0f,
                    direction.magnitude,
                    hud.EliteWave,
                    hud.Theme);
                VisibleCount++;
            }
        }

        foreach (CombatThreatView marker in markers)
        {
            marker.EndFrame();
        }
    }

    private bool ContainsCandidate(Cockpit cockpit)
    {
        for (int i = 0; i < hud.Candidates.Count; i++)
        {
            ControlUnit unit = hud.Candidates[i];
            if (unit != null && unit.cockpit == cockpit)
            {
                return true;
            }
        }

        return false;
    }
}

// 职责：根据炮弹真实位置历史绘制短曳光，池复用时清空历史，不参与命中/物理计算。
// 入口：TurretProjectile.Launch() 调用 Begin()；LateUpdate 更新两端，OnDisable 清理。
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProjectileTracer : MonoBehaviour
{
    [SerializeField, Min(0.01f)]
    private float width = 0.1f;
    [SerializeField, Range(0.01f, 0.2f)]
    private float historySeconds = 0.09f;
    [SerializeField, Min(0.1f)]
    private float maximumLength = 7f;
    [SerializeField, ColorUsage(false, true)]
    private Color tint = new Color(7f, 1.5f, 0.2f);
    private readonly Vector3[] positions = new Vector3[32];
    private readonly float[] times = new float[32];
    private StylizedBeamEffect beam;
    private int count;
    private int next;
    public void Begin()
    {
        if (beam == null)
        {
            beam = gameObject.AddComponent<StylizedBeamEffect>();
        }

        beam.Configure(width, "ProjectileTracer");
        beam.SetColor(tint);
        count = next = 0;
        Record();
        beam.SetVisible(false);
    }

    private void Record()
    {
        positions[next] = transform.position;
        times[next] = Time.time;
        next = (next + 1) % positions.Length;
        count = Mathf.Min(count + 1, positions.Length);
    }

    private void LateUpdate()
    {
        if (beam == null || count == 0)
        {
            return;
        }

        Record();
        Vector3 head = transform.position;
        Vector3 tail = head;
        float cutoff = Time.time - historySeconds;
        for (int offset = 1; offset < count; offset++)
        {
            int index = (next - 1 - offset + positions.Length) % positions.Length;
            tail = positions[index];
            if (times[index] <= cutoff)
            {
                int newer = (index + 1) % positions.Length;
                tail = Vector3.Lerp(tail, positions[newer], Mathf.InverseLerp(times[index], times[newer], cutoff));
                break;
            }
        }

        tail = head + Vector3.ClampMagnitude(tail - head, maximumLength);
        beam.SetEndpoints(tail, head);
        beam.SetIntensity(1f);
        beam.SetVisible((head - tail).sqrMagnitude > 0.0001f);
    }

    private void OnDisable()
    {
        count = next = 0;
        if (beam != null)
        {
            beam.SetVisible(false);
        }
    }
}

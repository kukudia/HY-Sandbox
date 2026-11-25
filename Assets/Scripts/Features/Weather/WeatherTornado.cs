// 职责：移动龙卷风，在物理步向动态单位施加旋转风力与升力并驱动漏斗粒子。
// 入口：Begin() 设置经过事件中心的路径。
using UnityEngine;

public sealed class WeatherTornado : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem funnel;
    [SerializeField, Min(1f)]
    private float radius = 28f;
    [SerializeField, Min(1f)]
    private float height = 50f;
    [SerializeField, Min(0f)]
    private float speed = 5f;
    [SerializeField, Min(0f)]
    private float swirl = 18f;
    [SerializeField, Min(0f)]
    private float pull = 9f;
    [SerializeField, Min(0f)]
    private float lift = 11f;
    private Vector3 direction;
    private float elapsed;
    private float emissionCarry;
    private int particleIndex;
    private bool started;
    public void Begin(Vector3 focus, float angle)
    {
        direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        transform.position = focus - direction * radius * 1.6f;
        funnel?.Play();
        started = true;
    }

    private void FixedUpdate()
    {
        if (!started)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;
        elapsed += dt;
        transform.position += direction * (speed * dt);
        ApplyWind();
        EmitFunnel(dt);
    }

    private void ApplyWind()
    {
        var units = ControlUnit.RuntimeUnits;
        for (int index = 0; index < units.Count; index++)
        {
            ControlUnit unit = units[index];
            if (unit == null || unit.gameObject.scene != gameObject.scene || !unit.IsSimulationActive)
            {
                continue;
            }

            Vector3 offset = unit.transform.position - transform.position;
            if (offset.y < -radius || offset.y > height)
            {
                continue;
            }

            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance >= radius || !unit.TryGetComponent(out Rigidbody body) || body.isKinematic)
            {
                continue;
            }

            Vector3 radial = offset.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial);
            float strength = 1f - distance / radius;
            body.AddForce((tangent * swirl - radial * pull + Vector3.up * lift) * strength, ForceMode.Acceleration);
        }
    }

    private void EmitFunnel(float dt)
    {
        if (funnel == null)
        {
            return;
        }

        // 局部空间保证粒子随漏斗移动；各高度错开采样，避免远距离看成几层圆环。
        emissionCarry += dt * 190f;
        int count = Mathf.FloorToInt(emissionCarry);
        emissionCarry -= count;
        for (int index = 0; index < count; index++)
        {
            int sample = particleIndex++;
            float band = (sample % 37) / 36f;
            float angle = elapsed * 2.8f + sample * 2.399963f + band * 5f;
            Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float width = Mathf.Lerp(.12f, .58f, band) * radius;
            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams
            {
                position = radial * width + Vector3.up * (band * height),
                velocity = Vector3.Cross(Vector3.up, radial) * -8f + Vector3.up * 3f,
                startColor = new Color32(174, 196, 212, (byte)(band < .15f ? 95 : 72)),
                startLifetime = .85f,
                startSize = Mathf.Lerp(3f, 10f, band),
                rotation = sample * 43f % 360f
            };
            funnel.Emit(particle, 1);
        }
    }
}

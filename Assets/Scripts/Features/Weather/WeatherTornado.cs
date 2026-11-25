// 职责：移动龙卷风，在物理步向动态刚体施加旋转风力与升力并驱动漏斗粒子。
// 入口：Begin() 设置经过事件中心的路径。
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class WeatherTornado : MonoBehaviour
{
    private const float BodyRefreshSeconds = .15f;
    [SerializeField]
    private ParticleSystem funnel;
    [SerializeField, Min(1f)]
    private float radius = 45f;
    [SerializeField, Min(1f)]
    private float height = 70f;
    [SerializeField, Min(0f)]
    private float speed = 5f;
    [SerializeField, Min(0f)]
    private float swirl = 18f;
    [SerializeField, Min(0f)]
    private float pull = 24f;
    [SerializeField, Min(0f)]
    private float lift = 11f;
    private Rigidbody[] bodies = Array.Empty<Rigidbody>();
    private float refreshAt;
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
        if (elapsed >= refreshAt)
        {
            // A collider query would miss bodies without a collider or with collisions disabled.
            bodies = FindObjectsByType<Rigidbody>();
            refreshAt = elapsed + BodyRefreshSeconds;
        }

        Vector3 center = transform.position;
        float radiusSquared = radius * radius;
        PhysicsScene physicsScene = gameObject.scene.GetPhysicsScene();
        foreach (Rigidbody body in bodies)
        {
            if (body == null || body.isKinematic || !body.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 offset = body.position - center;
            if (offset.y < -radius || offset.y > height)
            {
                continue;
            }

            offset.y = 0f;
            float distanceSquared = offset.sqrMagnitude;
            if (distanceSquared >= radiusSquared || body.gameObject.scene.GetPhysicsScene() != physicsScene)
            {
                continue;
            }

            float distance = Mathf.Sqrt(distanceSquared);
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

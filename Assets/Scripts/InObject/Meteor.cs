using UnityEngine;

public class Meteor : MonoBehaviour
{
    public MeteorShower meteorShower;
    public GameObject impactEffect;
    public float destroyDelay = 5f;

    [Header("视觉效果")]
    public VfxEffect trailEffect; // 拖尾效果
    public Light glowLight; // 发光效果
    public float minGlowIntensity = 1f;
    public float maxGlowIntensity = 5f;

    private void Start()
    {
        VisualEffectsManager.TryDecorateMeteor(this);

        // 随机化视觉效果


        if (glowLight != null)
        {
            glowLight.intensity = Random.Range(minGlowIntensity, maxGlowIntensity);
            glowLight.range = transform.localScale.x * 2f;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (collision.gameObject.CompareTag("Meteor")) return;

        if (impactEffect != null)
        {
            ContactPoint contact = collision.contacts[0];
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, contact.normal);
            Vector3 position = contact.point;
            Instantiate(impactEffect, position, rotation);
        }

        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.contacts[0];
            VisualEffectsManager.TryPlayMeteorImpact(contact.point, contact.normal, transform.lossyScale.magnitude, collision.relativeVelocity.magnitude);
        }

        // 移除物理组件
        //Destroy(GetComponent<Rigidbody>());
        //Destroy(GetComponent<Collider>());

        // 禁用视觉效果
        if (trailEffect != null) { trailEffect.StopAndRelease(); trailEffect = null; }
        if (glowLight != null) glowLight.enabled = false;

        if (meteorShower != null)
        {
            meteorShower.MeteorDestroyed();
        }

        Destroy(gameObject, destroyDelay);
    }
}
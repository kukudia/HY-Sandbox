// 职责：播放材质碎片ParticleSystem模板，预算有界且不生成会伤害/碰撞的实体。
// 入口：Play()；模板及粒子参数保存在Prefab，结束后销毁并归还活动预算。
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(ParticleSystem))]
public sealed class MaterialBreakEffect : MonoBehaviour
{
    [SerializeField, Min(1)]
    private int maximumActive = 32;
    private static int active;
    private bool counted;
    public static int ActiveCount => active;

    public static bool Play(MaterialBreakEffect template, Block block)
    {
        if (template == null || block == null)
        {
            return false;
        }

        // A saturated cosmetic budget still counts as handled; never fall back to a bigger burst.
        if (active >= template.maximumActive)
        {
            return true;
        }

        MaterialBreakEffect effect = Instantiate(template, block.transform.position, block.transform.rotation);
        SceneManager.MoveGameObjectToScene(effect.gameObject, block.gameObject.scene);
        effect.counted = true;
        active++;
        ParticleSystem particles = effect.GetComponent<ParticleSystem>();
        var shape = particles.shape;
        shape.scale = new Vector3(block.x, block.y, block.z) * .9f;
        particles.Play(true);
        Destroy(effect.gameObject, particles.main.duration + particles.main.startLifetime.constantMax + .2f);
        return true;
    }

    private void OnDestroy()
    {
        if (counted)
        {
            active = Mathf.Max(0, active - 1);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetBudget()
    {
        active = 0;
    }
}

// 职责：按场景复用短时光束和点光源；只限制闲置缓存，不裁剪当前战斗效果。
// 入口：RentBeam() / RentLight()；淡出结束Return，场景卸载连同活动及闲置子对象释放。
using System.Collections.Generic;
using UnityEngine;

public sealed class TransientEffectPool : MonoBehaviour
{
    [SerializeField, Min(0)]
    private int maxIdleBeams = 32;
    [SerializeField, Min(0)]
    private int maxIdleLights = 16;
    private static TransientEffectPool instance;
    private readonly Stack<BeamFade> beams = new Stack<BeamFade>();
    private readonly Stack<LightFade> lights = new Stack<LightFade>();
    private static TransientEffectPool Instance
    {
        get
        {
            if (instance == null || instance.gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene())
            {
                instance = SceneObjectQuery.FindInActiveScene<TransientEffectPool>();
                if (instance == null)
                {
                    throw new MissingReferenceException("Active scene requires an authored TransientEffectPool.");
                }
            }

            return instance;
        }
    }

    public static BeamFade RentBeam()
    {
        TransientEffectPool pool = Instance;
        BeamFade fade = null;
        while (pool.beams.Count > 0 && fade == null)
        {
            fade = pool.beams.Pop();
        }

        if (fade == null)
        {
            fade = Instantiate(RuntimeObjectTemplates.Instance.TransientBeam, pool.transform);
            StylizedBeamEffect beam = fade.GetComponent<StylizedBeamEffect>();
            fade.SetPool(pool, beam);
        }

        fade.gameObject.SetActive(true);
        return fade;
    }

    public static LightFade RentLight(Vector3 position)
    {
        TransientEffectPool pool = Instance;
        LightFade fade = null;
        while (pool.lights.Count > 0 && fade == null)
        {
            fade = pool.lights.Pop();
        }

        if (fade == null)
        {
            fade = Instantiate(RuntimeObjectTemplates.Instance.TransientLight, pool.transform);
            Light light = fade.GetComponent<Light>();
            fade.SetPool(pool, light);
        }

        fade.transform.position = position;
        fade.gameObject.SetActive(true);
        return fade;
    }

    internal void Return(BeamFade fade)
    {
        if (fade == null || !fade.gameObject.activeSelf)
        {
            return;
        }

        fade.Beam.SetVisible(false);
        fade.gameObject.SetActive(false);
        if (beams.Count < Mathf.Max(0, maxIdleBeams))
        {
            beams.Push(fade);
        }
        else
        {
            Destroy(fade.gameObject);
        }
    }

    internal void Return(LightFade fade)
    {
        if (fade == null || !fade.gameObject.activeSelf)
        {
            return;
        }

        fade.Light.intensity = 0f;
        fade.gameObject.SetActive(false);
        if (lights.Count < Mathf.Max(0, maxIdleLights))
        {
            lights.Push(fade);
        }
        else
        {
            Destroy(fade.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        beams.Clear();
        lights.Clear();
    }
}

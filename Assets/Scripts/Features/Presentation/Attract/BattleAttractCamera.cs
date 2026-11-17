// 职责：轮换可编辑镜头轨道、基地仰视和存活飞船特写，独立于玩家输入相机。
// 入口：LateUpdate()；每段5–10秒，参数和路径保存在场景，暂停时不推进。
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleAttractCamera : MonoBehaviour
{
    [SerializeField]
    private Camera view;
    [SerializeField]
    private BattleAttractStage stage;
    [SerializeField]
    private Transform[] starts;
    [SerializeField]
    private Transform[] ends;
    [SerializeField]
    private Transform focalPoint;
    [SerializeField]
    private Text caption;
    [SerializeField, Range(5f, 10f)]
    private float shotSeconds = 7f;
    private float elapsed;
    private int shot;
    private ControlUnit framed;
    private float framingRadius = 12f;
    public int Shot => shot;

    private void LateUpdate()
    {
        if (starts == null || starts.Length == 0 || ends.Length != starts.Length || focalPoint == null)
        {
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed >= shotSeconds)
        {
            elapsed %= shotSeconds;
            shot = (shot + 1) % (starts.Length + 1);
        }

        float t = Mathf.SmoothStep(0f, 1f, elapsed / shotSeconds);
        ControlUnit featured = stage.Featured;
        if (featured != framed && featured != null)
        {
            framed = featured;
            Bounds bounds = new Bounds(featured.cockpit.transform.position, Vector3.one);
            foreach (Collider collider in featured.GetComponentsInChildren<Collider>())
            {
                if (!collider.isTrigger)
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            framingRadius = Mathf.Clamp(bounds.extents.magnitude, 8f, 80f);
        }

        if (shot == starts.Length && featured != null)
        {
            Transform hull = featured.cockpit.transform;
            view.transform.position = hull.position + hull.right * Mathf.Lerp(-1.5f, 1.5f, t) * framingRadius - hull.forward * framingRadius * 2f + Vector3.up * framingRadius;
            view.transform.LookAt(hull.position + hull.forward * 5f);
            view.fieldOfView = 52f;
        }
        else
        {
            int index = shot % starts.Length;
            view.transform.position = Vector3.Lerp(starts[index].position, ends[index].position, t);
            Vector3 target = index == 1 && featured != null ? Vector3.Lerp(focalPoint.position, featured.cockpit.transform.position, .45f) : focalPoint.position;
            view.transform.LookAt(target);
            view.fieldOfView = index == 0 ? 68f : 58f;
        }

        if (caption != null)
        {
            caption.text = "BASTION  /  LIVE DEFENCE  ·  ESC TO MENU\n" + (shot == starts.Length ? "FLEET CLOSE PASS" : starts[shot].name.ToUpperInvariant());
        }
    }
}

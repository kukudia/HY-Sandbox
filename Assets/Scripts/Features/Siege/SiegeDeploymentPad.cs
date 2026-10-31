// 职责：把场景泊位关联到部署位置；不拥有舰船、经济或输入状态。
// 入口：Index；由可重复执行的场景迁移工具配置。
using UnityEngine;

public sealed class SiegeDeploymentPad : MonoBehaviour
{
    [SerializeField]
    private int index;
    public int Index => index;
}

// 职责：保存模块的运行单元归属；组件预挂在模块 Prefab，运行时只更新数据。
// 入口：Ensure()；脱离主体后仍保留原阵营及身份。
using UnityEngine;

public class RuntimeUnitMember : MonoBehaviour
{
    public string ownerUnitId;
    public UnitFaction ownerFaction;
    public static RuntimeUnitMember Ensure(GameObject obj, string unitId, UnitFaction faction)
    {
        if (obj == null)
        {
            return null;
        }

        RuntimeUnitMember member = obj.GetComponent<RuntimeUnitMember>();
        if (member == null)
        {
            throw new MissingComponentException($"{obj.name} requires an authored RuntimeUnitMember.");
        }

        member.ownerUnitId = unitId;
        member.ownerFaction = faction;
        return member;
    }
}

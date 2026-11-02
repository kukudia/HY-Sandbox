// 职责：在独立压力场景分帧恢复附加建筑，沿用生产建筑预算/支撑/损毁，不参与基地胜负。
// 入口：Start()；Inspector配置真实建筑蓝图与锚点，加载期间暂停HUD交互，场景退出释放拥有的建筑。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class SiegeStressPopulation : MonoBehaviour
{
    [SerializeField]
    private SiegeSession session;
    [SerializeField]
    private TextAsset buildingBlueprint;
    [SerializeField]
    private Transform[] anchors = Array.Empty<Transform>();
    private readonly List<BuildingRuntime> owned = new List<BuildingRuntime>();
    private IEnumerator loading;
    private SiegeHud hud;
    private bool hudWasEnabled;
    public bool IsReady
    {
        get; private set;
    }
    public string Error
    {
        get; private set;
    }
    public int ConfiguredBlocks
    {
        get; private set;
    }
    public float LoadSeconds
    {
        get; private set;
    }
    public IReadOnlyList<BuildingRuntime> Buildings => owned;

    private IEnumerator Start()
    {
        float started = Time.realtimeSinceStartup;
        hud = Object.FindFirstObjectByType<SiegeHud>();
        hudWasEnabled = hud != null && hud.enabled;
        if (hudWasEnabled)
        {
            hud.enabled = false;
        }

        try
        {
            if (session == null || buildingBlueprint == null || Array.Exists(anchors, anchor => anchor == null))
            {
                Error = "Stress population requires a session, building blueprint and valid anchors.";
                Debug.LogError(Error);
                yield break;
            }

            while (session.Match == null)
            {
                yield return null;
            }

            BlockDataList blueprint;
            try
            {
                blueprint = JsonUtility.FromJson<BlockDataList>(buildingBlueprint.text);
                WorldBuildingValidation.ValidateBlueprint(blueprint);
                ConfiguredBlocks = checked(blueprint.blocks.Count * anchors.Length);
                if (!session.Buildings.HasCapacity(ConfiguredBlocks))
                {
                    throw new InvalidOperationException("Stress population exceeds the existing world building budget.");
                }
            }
            catch (Exception error)
            {
                Error = error.ToString();
                Debug.LogException(error);
                yield break;
            }

            foreach (BlockData block in blueprint.blocks)
            {
                block.cargo = null;
            }

            foreach (Transform anchor in anchors)
            {
                var state = new WorldBuildingState
                {
                    instanceId = Guid.NewGuid().ToString("N"),
                    blueprintName = buildingBlueprint.name + " / stress satellite",
                    faction = UnitFaction.Player,
                    position = anchor.position,
                    rotation = anchor.rotation,
                    anchored = true,
                    blueprint = blueprint,
                    durability = Enumerable.Repeat(float.MaxValue, blueprint.blocks.Count).ToArray()
                };
                loading = session.Buildings.RestoreBuildingRoutine(state, building => owned.Add(building));
                while (true)
                {
                    bool more;
                    try
                    {
                        more = loading.MoveNext();
                    }
                    catch (Exception error)
                    {
                        Error = error.ToString();
                        Debug.LogException(error);
                        yield break;
                    }

                    if (!more)
                    {
                        break;
                    }

                    yield return loading.Current;
                }

                (loading as IDisposable)?.Dispose();
                loading = null;
            }

            IsReady = true;
            LoadSeconds = Time.realtimeSinceStartup - started;
        }
        finally
        {
            (loading as IDisposable)?.Dispose();
            loading = null;
            RestoreHud();
        }
    }

    private void OnDestroy()
    {
        (loading as IDisposable)?.Dispose();
        loading = null;
        RestoreHud();
        if (session != null && session.Buildings != null)
        {
            foreach (BuildingRuntime building in owned)
            {
                session.Buildings.RemoveBuilding(building);
            }
        }

        owned.Clear();
    }

    private void RestoreHud()
    {
        if (hud != null && hudWasEnabled)
        {
            hud.enabled = true;
        }

        hudWasEnabled = false;
    }
}

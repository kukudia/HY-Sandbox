// 职责：为历史世界组件/性能套件显式装配旧生命周期夹具；正式Main不会调用，也不用于模式集成验收。
// 入口：Attach()；仅由Editor测试或带显式-world-smoke-report参数的Player调用，不保存场景和玩家数据。
using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WorldSessionValidationHost
{
    public static WorldSessionManager Attach()
    {
        if (PlayManager.instance.WorldSession != null)
        {
            return PlayManager.instance.WorldSession;
        }

        GameModeController modes = PlayManager.instance.Modes;
        if (modes == null)
        {
            throw new InvalidOperationException("World component checks require saved Main and its mode host.");
        }

        // Test-only reflection preserves the historical component fixture without adding a production bypass API.
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject[] environment = (GameObject[])typeof(GameModeController).GetField("buildEnvironment", flags).GetValue(modes);
        typeof(PlayManager).GetField("modes", flags).SetValue(PlayManager.instance, null);
        modes.enabled = false;
        WorldSessionManager template = Resources.Load<WorldSessionManager>("Validation/WorldSession");
        if (template == null)
        {
            throw new InvalidOperationException("World validation template is missing; run gameplay scene authoring.");
        }

        WorldSessionManager session = Object.Instantiate(template);
        session.name = "World Session - Component Validation Only";
        session.BindHost(environment, SceneObjectQuery.FindInActiveScene<WorldLoadingPanel>(), null);
        PlayManager.instance.BindWorldSession(session);
        SceneObjectQuery.FindInActiveScene<OutpostHud>(FindObjectsInactive.Include)?.Bind(session);
        return session;
    }
}

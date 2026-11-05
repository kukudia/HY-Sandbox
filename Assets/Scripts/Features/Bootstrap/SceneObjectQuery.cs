// 职责：显式限定场景的服务查找，拒绝用对象创建顺序解决重复实例。
// 入口：FindInScene()/FindInActiveScene()；缺失返回null，同场景重复抛出明确错误。
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneObjectQuery
{
    public static T FindInActiveScene<T>(FindObjectsInactive inactive = FindObjectsInactive.Exclude)
        where T : Component
    {
        return FindInScene<T>(SceneManager.GetActiveScene(), inactive);
    }

    public static T FindInScene<T>(Scene scene, FindObjectsInactive inactive = FindObjectsInactive.Exclude)
        where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        T found = null;
        foreach (T candidate in UnityEngine.Object.FindObjectsByType<T>(inactive))
        {
            if (candidate.gameObject.scene != scene)
            {
                continue;
            }

            if (found != null)
            {
                throw new InvalidOperationException($"Scene '{scene.path}' ({scene.name}) contains multiple {typeof(T).Name} components. Bind an explicit owner/reference instead of relying on object order.");
            }

            found = candidate;
        }

        return found;
    }
}

// 职责：回归多模块刚体导致感知缓冲误饱和的问题，检查重复采集与静态通道保留。
// 入口：Play Mode中Validate()；在隔离物理场景构造600模块交通对象，不修改已打开场景。
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NavigationSensorChecks
{
    public static string Validate()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode for an isolated PhysicsScene.");
        }

        Scene scene = SceneManager.CreateScene("Navigation sensor fixture", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        try
        {
            var root = new GameObject("Inactive sensor owner");
            root.SetActive(false);
            SceneManager.MoveGameObjectToScene(root, scene);
            ControlUnit owner = root.AddComponent<ControlUnit>();
            var traffic = new GameObject("600 module traffic");
            SceneManager.MoveGameObjectToScene(traffic, scene);
            traffic.transform.position = new Vector3(40f, 20f, 0f);
            Rigidbody body = traffic.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearVelocity = Vector3.forward;
            for (int i = 0; i < 600; i++)
            {
                var block = new GameObject("Module");
                block.transform.SetParent(traffic.transform, false);
                block.transform.localPosition = new Vector3(i % 20 * 0.3f, 0f, i / 20 * 0.3f);
                block.AddComponent<BoxCollider>().size = Vector3.one * 0.2f;
            }

            for (int i = 0; i < 2; i++)
            {
                var wall = new GameObject("Independent static wall");
                SceneManager.MoveGameObjectToScene(wall, scene);
                wall.transform.position = new Vector3(20f, 20f, 20f + 10f * i);
                wall.AddComponent<BoxCollider>();
            }

            Physics.SyncTransforms();
            Type type = typeof(EnemyController).Assembly.GetType("NavigationPrediction", true);
            object prediction = Activator.CreateInstance(type, true);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo observe = type.GetMethod("Observe", flags);
            object[] args =
            {
                owner,
                new Bounds(new Vector3(0f, 20f, 0f), Vector3.one * 2f),
                Vector3.zero,
                (LayerMask)~0,
                8f,
                2f,
                new Vector3(100f, 20f, 0f),
                null,
                0,
                20f
            };
            for (int pass = 0; pass < 2; pass++)
            {
                observe.Invoke(prediction, args);
                int raw = (int)type.GetProperty("RawColliderCount", flags).GetValue(prediction);
                int observed = (int)type.GetProperty("Observed", flags).GetValue(prediction);
                bool saturated = (bool)type.GetProperty("Saturated", flags).GetValue(prediction);
                int moving = (int)type.GetProperty("Moving", flags).GetValue(prediction);
                if (raw != 602 || observed != 3 || saturated || moving != 1)
                {
                    throw new InvalidOperationException($"Pass {pass}: raw={raw}, observed={observed}, saturated={saturated}, moving={moving}");
                }
            }

            return "PASS 602 colliders captured without false saturation; 600 moving modules merge to one body; two static walls remain separate; repeated capture resets aggregation";
        }
        finally
        {
            SceneManager.UnloadSceneAsync(scene);
        }
    }
}

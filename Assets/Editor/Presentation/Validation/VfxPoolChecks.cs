// 职责：验证 GPU 特效峰值扩容、模板复位和旧尾烟名额限制的移除。
// 入口：RunPlay()；仅在隔离 Play 场景执行，销毁本次创建的池和刚体。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VfxPoolChecks
{
    public static string RunPlay()
    {
        if (!Application.isPlaying || Object.FindAnyObjectByType<VfxEffectPool>() != null)
        {
            throw new InvalidOperationException("Requires isolated Play scene without a VFX pool.");
        }

        var bodies = new List<GameObject>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo tick = typeof(VfxEffect).GetMethod("FinishPlayback", flags);
        FieldInfo deadline = typeof(VfxEffect).GetField("_releaseAt", flags);
        try
        {
            for (int i = 0; i < 96; i++)
            {
                Require(
                    BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Impact, Vector3.zero, Quaternion.identity) == BlockVfxLibrary.SpawnResult.Spawned,
                    "Impact rejected above old limit");
            }

            for (int i = 0; i < 80; i++)
            {
                Require(
                    BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, Vector3.zero, Quaternion.identity) == BlockVfxLibrary.SpawnResult.Spawned,
                    "Explosion rejected above old limit");
            }

            VfxEffectPool pool = Object.FindAnyObjectByType<VfxEffectPool>();
            Require(pool.ActiveCount == 176, "Active effects evicted");
            for (int i = 0; i < 40; i++)
            {
                var body = new GameObject("Pool check debris");
                bodies.Add(body);
                DetachedPartSmokeTrail.Attach(body.AddComponent<Rigidbody>(), Vector3.zero, 1f);
            }

            Require(pool.ActiveCount == 216, "Debris trails rejected above old limit");
            foreach (GameObject body in bodies)
            {
                Object.DestroyImmediate(body);
            }

            bodies.Clear();
            foreach (VfxEffect effect in pool.GetComponentsInChildren<VfxEffect>())
            {
                deadline.SetValue(effect, Time.time - 1f);
                tick.Invoke(effect, null);
            }

            Require(
                pool.ActiveCount == 0 && pool.IdleCount <= BlockVfxLibrary.Instance.MaxIdleEffects,
                "Return/cache bound failed");
            VfxEffect prefab = BlockVfxLibrary.Instance.DetachedSmoke;
            VfxEffect first = VfxEffectPool.Rent(prefab, Vector3.zero, Quaternion.identity, SceneManager.GetActiveScene());
            int created = pool.CreatedCount;
            first.transform.localScale = Vector3.one * 4f;
            first.Graphs[1].transform.localPosition = Vector3.one * 10;
            first.Graphs[1].SetFloat("FlameSize", 99);
            first.ReleaseAfterPlayback();
            deadline.SetValue(first, Time.time - 1f);
            tick.Invoke(first, null);
            VfxEffect reused = VfxEffectPool.Rent(prefab, Vector3.right * 100, Quaternion.identity, SceneManager.GetActiveScene());
            Require(first == reused && pool.CreatedCount == created, "Instance not reused");
            Require(reused.transform.localScale == prefab.transform.localScale, "Scale leaked");
            Require(
                reused.Graphs[1].transform.localPosition == prefab.Graphs[1].transform.localPosition,
                "Envelope pose leaked");
            Require(
                reused.Graphs[1].GetFloat("FlameSize") == prefab.Graphs[1].GetFloat("FlameSize"),
                "Graph override leaked");
            tick.Invoke(reused, null);
            Require(reused.gameObject.activeSelf && pool.ActiveCount == 1, "Previous return timer affected new rental");
            var meteor = new GameObject("Pool meteor check");
            bodies.Add(meteor);
            var follower = reused.gameObject.AddComponent<MeteorTrailFollower>();
            follower.Initialize(meteor.transform, reused);
            reused.StopAndRelease();
            deadline.SetValue(reused, Time.time - 1f);
            tick.Invoke(reused, null);
            VfxEffect afterMeteor = VfxEffectPool.Rent(prefab, Vector3.zero, Quaternion.identity, SceneManager.GetActiveScene());
            Require(afterMeteor == reused && !follower.enabled, "Meteor follower leaked into debris rental");
            follower.Initialize(meteor.transform, afterMeteor);
            Require(follower.enabled, "Meteor follower not re-enabled on reuse");
            Object.DestroyImmediate(pool.gameObject);
            Require(afterMeteor == null, "Active instance survived pool cleanup");
            VfxEffect fresh = VfxEffectPool.Rent(prefab, Vector3.zero, Quaternion.identity, SceneManager.GetActiveScene());
            VfxEffectPool freshPool = Object.FindAnyObjectByType<VfxEffectPool>();
            Require(
                fresh != null && freshPool.CreatedCount == 1 && freshPool.IdleCount == 0,
                "Stale scene pool reused after cleanup");
            return "PASS: 176 bursts + 40 trails without rejection/eviction; bounded idle cache; reused instance resets scale, graph pose, parameters, timer and meteor follower.";
        }
        finally
        {
            foreach (GameObject body in bodies)
            {
                Object.DestroyImmediate(body);
            }

            foreach (VfxEffectPool pool in Object.FindObjectsByType<VfxEffectPool>())
            {
                Object.DestroyImmediate(pool.gameObject);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

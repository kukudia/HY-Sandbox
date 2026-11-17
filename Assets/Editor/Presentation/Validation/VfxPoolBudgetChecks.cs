// 职责：用真实VFX模板验证显式活动预算、复用、销毁及清场后容量恢复。
// 入口：RunPlay()；独立临时场景，退出恢复原活动场景，不修改生产资源。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VfxPoolBudgetChecks
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
    public static string RunPlay()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.CreateScene("VFX budget isolated checks");
        SceneManager.SetActiveScene(scene);
        var root = new GameObject("Owned VFX budget fixture");
        var inactive = new GameObject("Inactive");
        inactive.transform.SetParent(root.transform, false);
        inactive.SetActive(false);
        VfxEffectPool pool = root.AddComponent<VfxEffectPool>();
        var settings = new SerializedObject(pool);
        settings.FindProperty("_inactiveRoot").objectReferenceValue = inactive.transform;
        settings.ApplyModifiedPropertiesWithoutUndo();
        VfxEffect explosion = BlockVfxLibrary.Instance.Resolve("Explosion");
        VfxEffect impact = BlockVfxLibrary.Instance.Resolve("ImpactBurst");
        var checks = new List<string>();
        try
        {
            Configure(settings, 3, 2);
            VfxEffect first = VfxEffectPool.Rent(explosion, Vector3.zero, Quaternion.identity, scene);
            VfxEffect second = VfxEffectPool.Rent(explosion, Vector3.one, Quaternion.identity, scene);
            Require(
                checks,
                "Real explosion templates rent up to per-template budget",
                first != null && second != null && pool.ActiveCount == 2);
            Require(
                checks,
                "Per-template saturation omits additional visual",
                VfxEffectPool.Rent(explosion, Vector3.zero, Quaternion.identity, scene) == null && pool.DroppedCount == 1);
            VfxEffect other = VfxEffectPool.Rent(impact, Vector3.zero, Quaternion.identity, scene);
            Require(checks, "Other template retains reserved capacity", other != null && pool.ActiveCount == 3);
            Require(
                checks,
                "Global budget blocks another template",
                VfxEffectPool.Rent(impact, Vector3.zero, Quaternion.identity, scene) == null && pool.DroppedCount == 2);
            Require(
                checks,
                "Production burst consumer reports omitted visual without exception",
                BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Explosion, Vector3.zero, Quaternion.identity) == BlockVfxLibrary.SpawnResult.BurstLimit);
            first.ReleaseAfterPlayback();
            typeof(VfxEffect).GetField("_releaseAt", Private).SetValue(first, Time.time - 1f);
            typeof(VfxEffect).GetMethod("FinishPlayback", Private).Invoke(first, null);
            Require(
                checks,
                "Playback completion frees active capacity and caches the instance",
                pool.ActiveCount == 2 && pool.IdleCount == 1);
            VfxEffect reused = VfxEffectPool.Rent(explosion, Vector3.one * 123, Quaternion.identity, scene);
            Require(
                checks,
                "Re-rent reuses instance with reset location",
                reused == first && reused.transform.position == Vector3.one * 123 && pool.ActiveCount == 3 && pool.IdleCount == 0);
            Object.DestroyImmediate(reused.gameObject);
            Require(
                checks,
                "External destruction releases both global and template counts",
                pool.ActiveCount == 2 && VfxEffectPool.Rent(explosion, Vector3.zero, Quaternion.identity, scene) != null);
            pool.ClearActive();
            Require(
                checks,
                "ClearActive immediately releases all budgets",
                pool.ActiveCount == 0 && VfxEffectPool.Rent(explosion, Vector3.zero, Quaternion.identity, scene) != null);
            pool.ClearActive();
            Configure(settings, 0, 0);
            for (int index = 0; index < 5; index++)
            {
                VfxEffectPool.Rent(explosion, Vector3.zero, Quaternion.identity, scene);
            }

            Require(checks, "Default zero limits preserve unlimited production-scene behavior", pool.ActiveCount == 5);
            string result = string.Join("\n", checks);
            Directory.CreateDirectory("Temp/NightDevelopment");
            File.WriteAllText("Temp/NightDevelopment/vfx-budget-checks.txt", result);
            return result;
        }
        finally
        {
            Object.DestroyImmediate(root);
            SceneManager.SetActiveScene(previous);
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    private static void Configure(SerializedObject settings, int total, int perTemplate)
    {
        settings.Update();
        settings.FindProperty("maximumActiveEffects").intValue = total;
        settings.FindProperty("maximumActivePerTemplate").intValue = perTemplate;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(List<string> checks, string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        checks.Add(label);
    }
}

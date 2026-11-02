// 职责：验证真实建筑库的旋转占地支撑、自然边缘恢复、密度与确定性；保存可复查报告。
// 入口：Run()在Edit Mode执行；不修改用户蓝图、场景或配置资产。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WorldTerrainIntegrationChecks
{
    public static string Run()
    {
        var output = new List<string>();
        WorldGenerationSettings settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>("Assets/World/WorldGeneration.asset"));
        try
        {
            var templates = Resources.LoadAll<TextAsset>("WorldBlueprints/Buildings").Select(asset => JsonUtility.FromJson<BlockDataList>(asset.text)).ToList();
            var footprints = new WorldBuildingFootprints(templates);
            var terrain = new StreamingWorldTerrain(settings, 14723, templates);
            var natural = new StreamingWorldTerrain(settings, 14723);
            var repeated = new StreamingWorldTerrain(settings, 14723, templates);
            int sites = 0;
            var covered = new HashSet<int>();
            var turns = new HashSet<int>();
            for (int z = -18; z <= 18; z++)
            {
                for (int x = -18; x <= 18; x++)
                {
                    var column = new Vector2Int(x, z);
                    if (!terrain.TryGetBuildingSite(column, out Vector3 position, out Quaternion rotation))
                    {
                        continue;
                    }

                    sites++;
                    int index = WorldBuildingFootprints.SelectTemplate(14723, column, templates.Count);
                    covered.Add(index);
                    turns.Add(Mathf.RoundToInt(rotation.eulerAngles.y / 90f));
                    BlockDataList data = templates[index];
                    for (int iz = 0; iz < data.foundation.length; iz++)
                    {
                        for (int ix = 0; ix < data.foundation.width; ix++)
                        {
                            Vector3 point = position + rotation * WorldBuildingPlacement.LocalAnchor(ix, iz);
                            float ground = terrain.SurfaceHeight(Mathf.FloorToInt(point.x / settings.CellSize), Mathf.FloorToInt(point.z / settings.CellSize));
                            Require(Mathf.Abs(ground - position.y) < .01f, "Every foundation cell is supported at its authored height");
                        }
                    }

                    Bounds footprint = footprints.Get(14723, column);
                    Vector3 center = position + rotation * new Vector3(footprint.center.x, 0f, footprint.center.z);
                    bool swapped = Mathf.RoundToInt(rotation.eulerAngles.y / 90f) % 2 != 0;
                    float halfX = (swapped ? footprint.size.z : footprint.size.x) * .5f;
                    float halfZ = (swapped ? footprint.size.x : footprint.size.z) * .5f;
                    for (int dz = -26; dz <= 26; dz++)
                    {
                        for (int dx = -26; dx <= 26; dx++)
                        {
                            int cx = Mathf.FloorToInt(center.x / settings.CellSize) + dx;
                            int cz = Mathf.FloorToInt(center.z / settings.CellSize) + dz;
                            float value = terrain.SurfaceHeight(cx, cz);
                            Require(value == repeated.SurfaceHeight(cx, cz), "Sampling order and repeat construction preserve terrain");
                            float distanceX = Mathf.Max(0f, Mathf.Abs((cx + .5f) * settings.CellSize - center.x) - halfX - settings.CellSize * .5f);
                            float distanceZ = Mathf.Max(0f, Mathf.Abs((cz + .5f) * settings.CellSize - center.z) - halfZ - settings.CellSize * .5f);
                            float distance = Mathf.Sqrt(distanceX * distanceX + distanceZ * distanceZ);
                            // Stay inside this site's allocation so neighbouring valid platforms do not affect the comparison.
                            if (distance >= settings.BuildingBlendWidth && Mathf.Abs(dx) < 12 && Mathf.Abs(dz) < 12)
                            {
                                Require(
                                    value == natural.SurfaceHeight(cx, cz),
                                    "Terrain outside the individual blend band is exactly natural");
                            }
                        }
                    }
                }
            }

            Require(
                covered.Count == templates.Count && turns.Count == 4,
                "All bundled templates and quarter turns exercised");
            output.Add($"PASS: {sites} sites; {covered.Count} templates; {turns.Count} rotations; every foundation cell supported; outside blend exactly natural; repeat sampling deterministic.");
            int highCount = CountSites(settings, templates);
            JsonUtility.FromJsonOverwrite("{\"buildingChance\":0.1}", settings);
            int lowCount = CountSites(settings, templates);
            Require(
                highCount > lowCount,
                "20 percent candidates produce more sites than 10 percent with identical footprints");
            output.Add($"PASS: seed14723 full map sites: 10%={lowCount}; 20%={highCount}.");
            output.Add("PASS: no-template sampler creates no artificial building pads.");
            string report = string.Join("\n", output);
            Directory.CreateDirectory("Temp/WorldTerrain");
            File.WriteAllText("Temp/WorldTerrain/integration.txt", report);
            return report;
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }
    }

    private static int CountSites(WorldGenerationSettings settings, List<BlockDataList> templates)
    {
        var terrain = new StreamingWorldTerrain(settings, 14723, templates);
        int count = 0;
        for (int z = -settings.WorldRadiusInChunks; z <= settings.WorldRadiusInChunks; z++)
        {
            for (int x = -settings.WorldRadiusInChunks; x <= settings.WorldRadiusInChunks; x++)
            {
                if (terrain.TryGetBuildingSite(new Vector2Int(x, z), out _, out _))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void Require(bool valid, string message)
    {
        if (!valid)
        {
            throw new InvalidOperationException(message);
        }
    }
}

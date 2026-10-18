// 职责：在隔离空场景 Play Mode 对照逐块连接重建与批处理的拓扑和耗时。
// 入口：unity command run_script --file Tools/Validation/BlueprintReviewProbe.cs --entry BlueprintReviewProbe.Run。
// 隔离：仅创建/清理临时方块，原始结果写 Temp/blueprint-review-benchmark.json；先进入空场景 Play。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlueprintReviewProbe
{
    static readonly Type Builder = typeof(Block).Assembly.GetType("BatchConnectionBuilder");
    static object Build(List<Block> blocks, float budget) => Activator.CreateInstance(Builder, new object[] { blocks, budget });
    public static string Run()
    {
        if (!Application.isPlaying) throw new Exception("Play required");
        var report = new Dictionary<string,object>();
        var samples = new List<object>();
        var prefab = Resources.Load<GameObject>("Blocks/1x1x1");
        if (prefab == null) throw new Exception("Missing cube");
        foreach (int count in new [] { 100, 256 })
        {
            var baseline = new List<double>(); var batch = new List<double>();
            string expected = null;
            for (int rep=0; rep<8; rep++)
            {
                bool useBatch = rep % 2 != 0;
                var root = new GameObject("Isolated blueprint benchmark");
                var blocks = new List<Block>();
                try
                {
                    for (int i=0;i<count;i++)
                    {
                        var go=Object.Instantiate(prefab, new Vector3(10000+i,2000,0), Quaternion.identity,root.transform);
                        var block=go.GetComponent<Block>(); block.RefreshRuntimeData(true); blocks.Add(block);
                    }
                    var watch=Stopwatch.StartNew();
                    if (useBatch) Builder.GetMethod("RebuildBatchImmediate").Invoke(Build(blocks,2f),null);
                    else foreach (var b in blocks) { b.neighbors=b.Neighbors(); b.CheckConnection(); }
                    watch.Stop();
                    var snapshot=string.Join(";",blocks.Select(b => string.Join(",",b.neighbors.Select(n=>blocks.IndexOf(n)).OrderBy(n=>n))+":"+string.Join("",b.connectors.Select(c=>c.isConnected?"1":"0"))));
                    if(expected==null) expected=snapshot;
                    if(snapshot!=expected) throw new Exception("Connectivity mismatch");
                    if(blocks[0].neighbors.Count!=1 || blocks[count-1].neighbors.Count!=1 || blocks[count/2].neighbors.Count!=2) throw new Exception("Chain topology failure");
                    foreach(var b in blocks) foreach(var c in b.connectors.Where(c=>c.isConnected))
                    {
                        if(c.connector==null) throw new Exception("Missing connector visual");
                        if(blocks.Sum(n=>n.connectors.Count(o=>o.connector==c.connector))!=2) throw new Exception("One-sided connection");
                    }
                    if(rep>=2) (useBatch?batch:baseline).Add(watch.Elapsed.TotalMilliseconds);
                }
                finally { Object.DestroyImmediate(root); }
            }
            samples.Add(new { blocks=count, baselineMs=baseline, batchMs=batch, equivalent=true });
        }
        var empty=(IEnumerator)Builder.GetMethod("RebuildBatchRoutine").Invoke(Build(new List<Block>(),.1f),null);
        if(empty.MoveNext()) throw new Exception("Empty yielded");
        report["samples"]=samples; report["emptyPass"]=true;
        report["unity"]=Application.unityVersion;
        var json=Newtonsoft.Json.JsonConvert.SerializeObject(report,Newtonsoft.Json.Formatting.Indented);
        System.IO.File.WriteAllText("Temp/blueprint-review-benchmark.json",json);
        return json;
    }
}

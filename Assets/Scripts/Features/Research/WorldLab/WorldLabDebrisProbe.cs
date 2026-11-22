// 职责：记录真实帧物理碎片下落，并在完成后重生成清理测试碎片。
// 入口：Begin()；2秒后比较真实位置，非手动Physics.Simulate。
using System.IO;
using UnityEngine;

public sealed class WorldLabDebrisProbe : MonoBehaviour
{
    private VoxelStructureCollapse collapse;
    private ProceduralVoxelWorld world;
    private Vector3 initialPosition;
    private float began;
    private int frames;
    public void Begin(VoxelStructureCollapse owner, ProceduralVoxelWorld source)
    {
        collapse = owner;
        world = source;
        initialPosition = transform.position;
        began = Time.time;
    }

    private void FixedUpdate()
    {
        frames++;
        if (world == null || Time.time - began < 2f)
        {
            return;
        }

        var report = new Result
        {
            fixedFrames = frames,
            initial = initialPosition,
            final = transform.position,
            fell = transform.position.y < initialPosition.y - 1f
        };
        world.Generate();
        report.fragmentsClearedByRegenerate = collapse.ActivePieces == 0;
        File.WriteAllText("Docs/Validation/2026-10-15-world-collapse-physics.json", JsonUtility.ToJson(report, true));
        world = null;
    }

    [System.Serializable]
    private sealed class Result
    {
        public int fixedFrames;
        public Vector3 initial;
        public Vector3 final;
        public bool fell;
        public bool fragmentsClearedByRegenerate;
    }
}

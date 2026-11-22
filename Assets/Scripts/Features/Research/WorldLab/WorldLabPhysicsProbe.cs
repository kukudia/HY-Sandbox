// 职责：实验场真实帧落体与体素地面接触探针，输出接触证据而不驱动Physics.Simulate。
// 入口：Begin()由WorldLabChecks调用；10秒超时失败，结束移除探针对象。
using System.IO;
using UnityEngine;

public sealed class WorldLabPhysicsProbe : MonoBehaviour
{
    private string outputPath;
    private float startedAt;
    private int fixedFrames;
    public void Begin(string path)
    {
        outputPath = path;
        startedAt = Time.unscaledTime;
    }

    private void FixedUpdate()
    {
        fixedFrames++;
        if (!string.IsNullOrEmpty(outputPath) && Time.unscaledTime - startedAt > 10f)
        {
            Save(false);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!string.IsNullOrEmpty(outputPath) && collision.collider.GetComponent<VoxelWorldChunk>() != null)
        {
            Save(true);
        }
    }

    private void Save(bool contact)
    {
        File.WriteAllText(
            outputPath,
            JsonUtility.ToJson(
                new Result { terrainContact = contact, fixedFrames = fixedFrames, position = transform.position, elapsed = Time.unscaledTime - startedAt },
                true));
        outputPath = null;
        Destroy(gameObject);
    }

    [System.Serializable]
    private sealed class Result
    {
        public bool terrainContact;
        public int fixedFrames;
        public Vector3 position;
        public float elapsed;
    }
}

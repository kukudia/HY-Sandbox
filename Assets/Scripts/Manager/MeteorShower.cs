using UnityEngine;
using System.Collections;

public class MeteorShower : MonoBehaviour
{
    [Header("陨石设置")]
    public GameObject meteorPrefab;
    public float spawnAreaSize = 500f;
    public float minMeteorSize = 0.5f;
    public float maxMeteorSize = 5f;
    public float minSpawnHeight = 100f;
    public float maxSpawnHeight = 300f;
    public float minFallSpeed = 10f;
    public float maxFallSpeed = 50f;

    [Header("随机初速度设置")]
    public float minHorizontalSpeed = 0f; // 最小水平速度
    public float maxHorizontalSpeed = 20f; // 最大水平速度
    public float minRotationSpeed = 10f; // 最小旋转速度
    public float maxRotationSpeed = 100f; // 最大旋转速度

    [Header("生成控制")]
    public float spawnRate = 0.5f;
    public int maxMeteors = 100;

    private int currentMeteorCount = 0;

    void Start()
    {
        StartCoroutine(SpawnMeteors());
    }

    IEnumerator SpawnMeteors()
    {
        while (true)
        {
            if (currentMeteorCount < maxMeteors)
            {
                SpawnMeteor();
                currentMeteorCount++;
            }

            yield return new WaitForSeconds(1f / spawnRate);
        }
    }

    void SpawnMeteor()
    {
        // 随机生成位置
        Vector3 spawnPosition = new Vector3(
            Random.Range(-spawnAreaSize / 2f, spawnAreaSize / 2f),
            Random.Range(minSpawnHeight, maxSpawnHeight),
            Random.Range(-spawnAreaSize / 2f, spawnAreaSize / 2f)
        );

        // 实例化陨石
        GameObject meteor = Instantiate(meteorPrefab, spawnPosition, Random.rotation);
        meteor.transform.SetParent(transform);

        // 设置随机大小
        float size = Random.Range(minMeteorSize, maxMeteorSize);
        meteor.transform.localScale = new Vector3(size, size, size);

        // 添加物理组件
        Rigidbody rb = meteor.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = meteor.AddComponent<Rigidbody>();
        }

        rb.mass = size * 100;

        // 设置随机下落速度
        float fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);

        // 添加随机水平初速度
        float horizontalSpeed = Random.Range(minHorizontalSpeed, maxHorizontalSpeed);
        Vector3 horizontalDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        // 合成最终速度：水平初速度 + 垂直下落速度
        rb.linearVelocity = horizontalDirection * horizontalSpeed + Vector3.down * fallSpeed;

        // 添加随机旋转
        float rotationSpeed = Random.Range(minRotationSpeed, maxRotationSpeed);
        rb.angularVelocity = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized * rotationSpeed;

        // 添加碰撞检测
        //SphereCollider collider = meteor.GetComponent<SphereCollider>();
        //if (collider == null)
        //{
        //    collider = meteor.AddComponent<SphereCollider>();
        //}
        //collider.radius = 0.5f;

        // 添加销毁脚本
        Meteor meteorScript = meteor.GetComponent<Meteor>();
        if (meteorScript == null)
        {
            meteorScript = meteor.AddComponent<Meteor>();
        }
        meteorScript.meteorShower = this;
    }

    public void MeteorDestroyed()
    {
        currentMeteorCount--;
    }
}
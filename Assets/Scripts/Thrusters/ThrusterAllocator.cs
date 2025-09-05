using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最佳推进器分配器：解 min ||W(Af - b)||^2 + λ||f||^2, s.t. 0 ≤ f ≤ fmax
/// A: 6×N，前3行是力贡献，后3行是力矩贡献。f: N×1 推力标量（沿各喷口方向）
/// 采用阻尼最小二乘 + 主动集（把触界的推进器固定，重解自由集）
/// </summary>
public static class ThrusterAllocator
{
    // 主入口：返回每个推进器的推力值（world空间，标量，沿各自dir）
    public static float[] Solve(
        Vector3[] forceDirs,      // 每个推进器的力方向（单位向量，一般 t.transform.up）
        Vector3[] torqueDirs,     // 每个推进器对单位推力产生的力矩方向 = r × dir
        float[] fMax,             // 每个推进器的最大推力
        Vector3 desiredForce,     // 期望总力（世界坐标）
        Vector3 desiredTorque,    // 期望总力矩（世界坐标）
        float forceWeight = 1f,   // 力的权重（放大力误差在目标函数中的比重）
        float torqueWeight = 1f,  // 力矩的权重
        float damping = 1e-3f,    // 阻尼λ（数值稳定）
        int maxIters = 8          // 主动集最大迭代次数
    )
    {
        int n = forceDirs.Length;
        if (torqueDirs.Length != n || fMax.Length != n)
            throw new ArgumentException("Input array sizes mismatch.");

        // --- 构建 A(6×N), b(6)
        // 行0..2: 力贡献；行3..5: 力矩贡献
        // 先按权重缩放行：等价于 W·A, W·b
        float[,] A = new float[6, n];
        for (int i = 0; i < n; i++)
        {
            Vector3 force = forceDirs[i] * forceWeight;
            Vector3 t = torqueDirs[i] * torqueWeight;
            A[0, i] = force.x; A[1, i] = force.y; A[2, i] = force.z;
            A[3, i] = t.x; A[4, i] = t.y; A[5, i] = t.z;
        }

        float[] b = new float[6] {
            desiredForce.x * forceWeight,
            desiredForce.y * forceWeight,
            desiredForce.z * forceWeight,
            desiredTorque.x * torqueWeight,
            desiredTorque.y * torqueWeight,
            desiredTorque.z * torqueWeight
        };

        // 主动集
        var free = new List<int>(n);
        var clamped = new Dictionary<int, float>(n); // idx -> fixed value (0 or fMax)
        for (int i = 0; i < n; i++) free.Add(i);

        float[] f = new float[n];

        for (int iter = 0; iter < maxIters; iter++)
        {
            // 计算 b' = b - A_fixed * f_fixed
            float[] bPrime = (float[])b.Clone();
            if (clamped.Count > 0)
            {
                foreach (var kv in clamped)
                {
                    int j = kv.Key;
                    float fj = kv.Value;
                    for (int r = 0; r < 6; r++)
                        bPrime[r] -= A[r, j] * fj;
                }
            }

            // Afree
            int k = free.Count;
            if (k == 0) break; // 全部被夹住
            float[,] Afree = new float[6, k];
            for (int col = 0; col < k; col++)
            {
                int j = free[col];
                for (int r = 0; r < 6; r++)
                    Afree[r, col] = A[r, j];
            }

            // 解 (Afree^T Afree + λI) x = Afree^T b'
            float[,] H = MultiplyAT_A(Afree, 6, k);          // k×k
            AddDamping(H, k, damping);
            float[] rhs = MultiplyAT_b(Afree, 6, k, bPrime); // k

            bool spdOk = CholeskySolveInPlace(H, rhs, k);    // 解得rhs = x
            if (!spdOk)
            {
                // 如果分解失败，增大阻尼重试
                AddDamping(H, k, 1e-2f);
                spdOk = CholeskySolveInPlace(H, rhs, k);
            }

            // 合成 f：先写入自由集解
            for (int col = 0; col < k; col++)
                f[free[col]] = rhs[col];
            // 已夹住的索引用固定值
            foreach (var kv in clamped)
                f[kv.Key] = kv.Value;

            // 处理越界，更新主动集
            bool changed = false;
            for (int i = 0; i < n; i++)
            {
                float fi = f[i];
                float lo = 0f, hi = fMax[i];
                if (fi < lo - 1e-4f)
                {
                    f[i] = 0f;
                    if (!clamped.ContainsKey(i)) { clamped[i] = 0f; free.Remove(i); changed = true; }
                }
                else if (fi > hi + 1e-4f)
                {
                    f[i] = hi;
                    if (!clamped.ContainsKey(i)) { clamped[i] = hi; free.Remove(i); changed = true; }
                }
                else
                {
                    // 位于可行域内：如果之前被夹住且现在在界内，可释放（一般需要KKT检查，这里简化）
                    if (clamped.ContainsKey(i) && fi > lo + 1e-4f && fi < hi - 1e-4f)
                    {
                        clamped.Remove(i);
                        if (!free.Contains(i)) free.Add(i);
                        changed = true;
                    }
                }
            }

            if (!changed) break; // 收敛
        }

        // 最终夹紧到可行域（数值保险）
        for (int i = 0; i < n; i++)
            f[i] = Mathf.Clamp(f[i], 0f, fMax[i]);

        return f;
    }

    // ===== 线代小工具 =====

    // H = A^T A
    static float[,] MultiplyAT_A(float[,] A, int rows, int cols)
    {
        float[,] H = new float[cols, cols];
        for (int i = 0; i < cols; i++)
        {
            for (int j = i; j < cols; j++)
            {
                double sum = 0;
                for (int r = 0; r < rows; r++)
                    sum += (double)A[r, i] * A[r, j];
                H[i, j] = H[j, i] = (float)sum;
            }
        }
        return H;
    }

    // rhs = A^T b
    static float[] MultiplyAT_b(float[,] A, int rows, int cols, float[] b)
    {
        float[] rhs = new float[cols];
        for (int i = 0; i < cols; i++)
        {
            double sum = 0;
            for (int r = 0; r < rows; r++)
                sum += (double)A[r, i] * b[r];
            rhs[i] = (float)sum;
        }
        return rhs;
    }

    static void AddDamping(float[,] H, int n, float lambda)
    {
        for (int i = 0; i < n; i++)
            H[i, i] += lambda;
    }

    /// <summary>
    /// 用Cholesky分解解对称正定方程 Hx=rhs；H被覆盖为分解结果，rhs被覆盖为解x
    /// 返回false表示未能分解（数值问题）
    /// </summary>
    static bool CholeskySolveInPlace(float[,] H, float[] rhs, int n)
    {
        // 分解：H = L L^T
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j <= i; j++)
            {
                double sum = H[i, j];
                for (int k = 0; k < j; k++)
                    sum -= H[i, k] * H[j, k];

                if (i == j)
                {
                    if (sum <= 1e-12) return false;
                    H[i, j] = (float)Math.Sqrt(sum);
                }
                else
                {
                    H[i, j] = (float)(sum / H[j, j]);
                }
            }
            // 清上三角
            for (int j = i + 1; j < n; j++)
                H[i, j] = 0f;
        }

        // 前代：Ly=rhs
        for (int i = 0; i < n; i++)
        {
            double sum = rhs[i];
            for (int k = 0; k < i; k++)
                sum -= H[i, k] * rhs[k];
            rhs[i] = (float)(sum / H[i, i]);
        }
        // 回代：L^T x = y
        for (int i = n - 1; i >= 0; i--)
        {
            double sum = rhs[i];
            for (int k = i + 1; k < n; k++)
                sum -= H[k, i] * rhs[k];
            rhs[i] = (float)(sum / H[i, i]);
        }
        return true;
    }
}

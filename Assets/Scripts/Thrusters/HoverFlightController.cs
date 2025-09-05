using UnityEditor;
using UnityEngine;

public class HoverFlightController : MonoBehaviour
{
    public float[] LastAlloc => lastAlloc;
    public float HeightIntegral => heightIntegral;

    public HoverThruster[] thrusters;

    [Header("Hover Target & PID (forces in N)")]
    public float hoverHeight = 5f;
    public float kpPerMass = 1f;   // kp = kpPerMass * rb.mass
    public float ki = 1f;
    public float kdPerMass = 2f;    // kd = kdPerMass * rb.mass

    [Header("Attitude (Leveling)")]
    public float attitudeKp = 10f;

    [Header("Allocator settings")]
    public float forceWeight = 1f;
    public float torqueWeight = 1f;
    public float damping = 1e-2f;
    public int allocatorIters = 12;

    [Header("Thrust smoothing")]
    public float thrustSmoothTime = 0.08f; // 秒，越小响应越快但更易振荡

    [Header("PID anti-windup limits")]
    public float integralMin = -1000f;
    public float integralMax = 1000f;

    private Rigidbody rb;
    private float[] lastAlloc;
    private float heightIntegral = 0f;

    public bool showUI = true;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;

    public void Init()
    {
        rb = GetComponent<Rigidbody>();
        if (thrusters != null) lastAlloc = new float[thrusters.Length];
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (thrusters != null) lastAlloc = new float[thrusters.Length];
    }

    void FixedUpdate()
    {
        if (thrusters == null || thrusters.Length == 0) return;

        ConfigureThrust();
    }

    void ConfigureThrust()
    {
        float dt = Time.fixedDeltaTime;

        // 1) desired total lift = gravity (feedforward) + PID correction
        float gravity = rb.mass * Mathf.Abs(Physics.gravity.y);

        float heightError = hoverHeight - transform.position.y;

        // P (in N): choose kp such that kp * 1m ~ needed force (~gravity)
        float kp = kpPerMass * rb.mass;
        float kd = kdPerMass * rb.mass;

        // D term: use vertical velocity as derivative (negative sign because if moving up, reduce thrust)
        float verticalVel = rb.linearVelocity.y;
        float pTerm = kp * heightError;
        heightIntegral += heightError * dt;
        // anti-windup
        heightIntegral = Mathf.Clamp(heightIntegral, integralMin, integralMax);
        float iTerm = ki * heightIntegral;
        float dTerm = -kd * verticalVel; // negative: upward velocity reduces thrust

        float pidOutput = pTerm + iTerm + dTerm; // in Newtons

        float desiredLift = gravity + pidOutput;
        if (desiredLift < 0f) desiredLift = 0f; // 不能要负的总升力

        Vector3 desiredForce = Vector3.up * desiredLift;

        // 2) desired torque to level the craft
        Vector3 tiltError = Vector3.Cross(transform.up, Vector3.up);
        Vector3 desiredTorque = tiltError * attitudeKp;

        // 3) prepare allocator inputs
        int n = thrusters.Length;
        var fDirs = new Vector3[n];
        var tauDirs = new Vector3[n];
        var fMax = new float[n];

        Vector3 com = rb.worldCenterOfMass;
        for (int i = 0; i < n; i++)
        {
            var t = thrusters[i];
            fDirs[i] = t.transform.up.normalized;
            tauDirs[i] = Vector3.Cross(t.transform.position - com, fDirs[i]);
            fMax[i] = t.maxThrust;
        }

        // 4) call allocator with stronger damping / iterations
        var alloc = ThrusterAllocator.Solve(
            fDirs, tauDirs, fMax,
            desiredForce, desiredTorque,
            forceWeight, torqueWeight,
            damping, maxIters: allocatorIters
        );

        // 5) smooth the allocations to avoid jumps
        float alpha = dt / (thrustSmoothTime + dt); // exponential smoothing factor
        for (int i = 0; i < n; i++)
        {
            float smoothed = Mathf.Lerp(lastAlloc[i], alloc[i], alpha);
            lastAlloc[i] = smoothed;
            
            if (thrusters[i].ShouldActivate())
            {
                thrusters[i].thrust = smoothed;
                thrusters[i].ApplyThrust();
            }
            else
            {
                thrusters[i].thrust = 0;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (thrusters == null) return;

        if (rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(rb.worldCenterOfMass, 0.12f);
        }

        if (lastAlloc != null && thrusters.Length == lastAlloc.Length)
        {
            for (int i = 0; i < thrusters.Length; i++)
            {
                var t = thrusters[i];
                if (t == null) continue;
                float norm = t.maxThrust > 1e-5f ? lastAlloc[i] / t.maxThrust : 0f;
                Gizmos.color = Color.Lerp(Color.red, Color.green, norm);
                Vector3 p = t.transform.position;
                Vector3 d = t.transform.up;
                Gizmos.DrawLine(p, p + d * (1.5f + 2f * norm));
            }
        }
    }

    void OnGUI()
    {
        if (!showUI || thrusters == null || lastAlloc == null) return;

        headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 16;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = Color.cyan;

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = Color.white;

        GUILayout.BeginArea(new Rect(20, 20, 320, 600), GUI.skin.window);

        GUILayout.Label("Hover Flight Debug", headerStyle);

        GUILayout.Space(8);
        GUILayout.Label($"Target Height: {hoverHeight:F2}", labelStyle);
        GUILayout.Label($"Current Height: {transform.position.y:F2}", labelStyle);

        GUILayout.Space(5);
        GUILayout.Label($"PID Integral: {heightIntegral:F2}", labelStyle);

        GUILayout.Space(10);
        GUILayout.Label("Thrusters:", headerStyle);

        for (int i = 0; i < thrusters.Length; i++)
        {
            if (thrusters[i] == null) continue;

            float norm = thrusters[i].maxThrust > 1e-5f ? thrusters[i].thrust / thrusters[i].maxThrust : 0f;
            Color barColor = Color.Lerp(Color.red, Color.green, norm);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"#{i} {thrusters[i].thrust:F1}/{thrusters[i].maxThrust}", labelStyle);

            if (thrusters[i].thrust > 0)
            {
                // 画进度条背景
                Rect r = GUILayoutUtility.GetRect(100, 18);
                GUI.color = Color.gray;
                GUI.Box(r, GUIContent.none);

                // 画推力值条
                Rect filled = new Rect(r.x, r.y, r.width * norm, r.height);
                GUI.color = barColor;
                GUI.Box(filled, GUIContent.none);
            }

            // 恢复颜色
            GUI.color = Color.white;

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }
}

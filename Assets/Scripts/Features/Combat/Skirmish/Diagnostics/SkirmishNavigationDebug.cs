// 职责：在随机5v5中连续追踪红队五个槽位，展示可切换导航文字、实速轨迹与未来路线。
// 入口：Update()降频绑定/采样；OnGUI()只读缓存；FocusSelected()保持当前相机朝向观察所选单位。
// 生命周期：换轮复用场景保存的五组追踪对象；驾驶舱重组续接，死亡保留轨迹，退出自动释放。
using UnityEngine;

public sealed class SkirmishNavigationDebug : MonoBehaviour
{
    [SerializeField]
    private SkirmishMatch match;
    [SerializeField]
    private NavigationTrack[] enemyTracks;
    [SerializeField]
    private Camera viewCamera;
    [SerializeField]
    private Font debugFont;
    [SerializeField, Min(0.05f)]
    private float sampleInterval = 0.1f;
    [SerializeField]
    private bool showPanel = true;
    [SerializeField]
    private bool showAllTraces = true;
    [SerializeField]
    private bool showRoute = true;
    [SerializeField]
    private bool showWorldLabels = true;
    [SerializeField, Range(0, 4)]
    private int selectedEnemy;
    private float nextSample;
    private int round = -1;
    private string report = "Waiting for combat units";
    private GUIStyle textStyle;
    public NavigationTrack Selected => enemyTracks != null && selectedEnemy < enemyTracks.Length ? enemyTracks[selectedEnemy] : null;
    public string Report => report;
    public int TrackedCount => enemyTracks != null ? enemyTracks.Length : 0;

    public NavigationTrack TrackAt(int index) => index >= 0 && index < TrackedCount ? enemyTracks[index] : null;
    private void Update()
    {
        if (match == null || Time.unscaledTime < nextSample)
        {
            return;
        }

        nextSample = Time.unscaledTime + Mathf.Max(0.05f, sampleInterval);
        bool newRound = round != match.Round;
        round = match.Round;
        for (int i = 0; i < TrackedCount; i++)
        {
            NavigationTrack track = enemyTracks[i];
            if (track == null)
            {
                continue;
            }

            Cockpit cockpit = match.CockpitAt(5 + i);
            ControlUnit owner = cockpit != null ? cockpit.GetComponentInParent<ControlUnit>() : null;
            if (newRound || owner != null && owner != track.Owner)
            {
                track.Bind(owner, match.NameAt(5 + i), newRound);
            }

            track.Sample();
            track.SetVisible(showAllTraces || i == selectedEnemy, showRoute && i == selectedEnemy);
        }

        report = Selected != null ? NavigationDebugText.Build(Selected) : "No configured tracking slot";
    }

    public void SelectEnemy(int index)
    {
        selectedEnemy = Mathf.Clamp(index, 0, Mathf.Max(0, TrackedCount - 1));
        nextSample = 0f;
    }

    public void FocusSelected()
    {
        if (viewCamera != null && Selected != null && Selected.Alive)
        {
            // 不锁定或接管自由相机；平移到舰船后上方，继续使用既有自由观察输入。
            viewCamera.transform.position = Selected.Owner.transform.position - viewCamera.transform.forward * 55f;
        }
    }

    private void OnGUI()
    {
        if (!showPanel)
        {
            return;
        }

        float scale = Mathf.Min(Screen.width / 1600f, Screen.height / 900f);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * Mathf.Max(0.25f, scale));
        textStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = debugFont,
            fontSize = 14,
            wordWrap = true,
            richText = false
        };
        textStyle.normal.textColor = Color.white;
        Rect panel = new Rect(Screen.width / Mathf.Max(0.25f, scale) - 525f, 245f, 505f, 605f);
        Color previousColor = GUI.color;
        GUI.color = new Color(0.025f, 0.035f, 0.055f, 1f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = previousColor;
        GUILayout.BeginArea(panel, GUI.skin.box);
        GUILayout.Label("ENEMY NAVIGATION / live combat", textStyle);
        GUILayout.BeginHorizontal();
        for (int i = 0; i < TrackedCount; i++)
        {
            if (GUILayout.Button((i == selectedEnemy ? "> " : "") + "RED-" + (i + 1)))
            {
                SelectEnemy(i);
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Focus ship"))
        {
            FocusSelected();
        }

        if (GUILayout.Button("Clear selected trace"))
        {
            Selected?.ClearHistory();
        }

        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        showAllTraces = GUILayout.Toggle(showAllTraces, "All enemy traces");
        showRoute = GUILayout.Toggle(showRoute, "Selected route");
        GUILayout.EndHorizontal();
        GUILayout.Label(report, textStyle);
        GUILayout.EndArea();
        GUI.matrix = previousMatrix;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying || !showWorldLabels || UnityEditor.SceneView.currentDrawingSceneView == null)
        {
            return;
        }

        for (int i = 0; i < TrackedCount; i++)
        {
            NavigationTrack track = enemyTracks[i];
            if (track != null && track.Alive)
            {
                Gizmos.color = i == selectedEnemy ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(track.Snapshot.position, i == selectedEnemy ? 3f : 1f);
                UnityEditor.Handles.Label(
track.Snapshot.position + Vector3.up * 5f,
track.Label + "\n" + NavigationDebugText.Mode(track.Snapshot));
            }
        }
#endif
    }
}

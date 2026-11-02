// 职责：集中绘制已受损模块的可选耐久标签，避免每个模块注册IMGUI回调。
// 入口：DebugManager装配；标签关闭时不遍历模块，不更改伤害或耐久状态。
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DurabilityDebugLabels : MonoBehaviour
{
    private GUIStyle style;
    private void OnGUI()
    {
        PlayManager play = PlayManager.instance;
        if (play == null || !play.showLabel || Event.current.type != EventType.Repaint)
        {
            return;
        }

        Camera camera = play.mainCamera != null ? play.mainCamera : Camera.main;
        if (camera == null)
        {
            return;
        }

        style ??= new GUIStyle(GUI.skin.label);
        foreach (Durability health in Durability.Active)
        {
            if (health == null || !health.debugLog || health.currentDurability >= health.maxDurability)
            {
                continue;
            }

            Vector3 position = camera.WorldToScreenPoint(health.transform.position);
            if (position.z <= 0 || position.x < -50 || position.x > Screen.width + 50 || position.y < -20 || position.y > Screen.height + 20)
            {
                continue;
            }

            style.normal.textColor = Color.Lerp(Color.red, Color.green, health.currentDurability / Mathf.Max(.001f, health.maxDurability));
            GUI.Label(
                new Rect(position.x - 50, Screen.height - position.y - 10, 100, 20),
                $"{health.currentDurability:F1}/{health.maxDurability}",
                style);
        }
    }
}

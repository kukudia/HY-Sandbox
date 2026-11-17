// 职责：以无贴图矢量网格绘制双层威胁箭头，保持各分辨率下的清晰边缘。
// 入口：uGUI OnPopulateMesh；组件与颜色保存在HUD Prefab，不创建运行时对象。
using UnityEngine;
using UnityEngine.UI;

public sealed class CombatThreatIcon : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Vector2 center = rectTransform.rect.center;
        float scale = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) / 36f;
        Chevron(mesh, center, scale, 5f, color);
        Color tail = color;
        tail.a *= .4f;
        Chevron(mesh, center, scale, -5f, tail);
    }

    private static void Chevron(VertexHelper mesh, Vector2 center, float scale, float offset, Color tint)
    {
        Vector2[] points =
        {
            new Vector2(-12, offset - 5),
            new Vector2(0, offset + 7),
            new Vector2(12, offset - 5),
            new Vector2(12, offset - 10),
            new Vector2(0, offset + 2),
            new Vector2(-12, offset - 10)
        };
        int start = mesh.currentVertCount;
        foreach (Vector2 point in points)
        {
            mesh.AddVert(center + point * scale, tint, Vector2.zero);
        }

        mesh.AddTriangle(start, start + 1, start + 4);
        mesh.AddTriangle(start, start + 4, start + 5);
        mesh.AddTriangle(start + 1, start + 2, start + 3);
        mesh.AddTriangle(start + 1, start + 3, start + 4);
    }
}

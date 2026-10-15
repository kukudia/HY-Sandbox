// 职责：为选中的程序化世界提供Undo预览、重生成与真实网格Bake入口。
// 入口：ProceduralVoxelWorld Inspector；只写所选世界及WorldLab网格目录。
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProceduralVoxelWorld))]
public sealed class ProceduralVoxelWorldEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var world = (ProceduralVoxelWorld)target;
        EditorGUILayout.HelpBox("WorldLab研究原型：根物体单位缩放。运行期编辑不写蓝图存档。更改种子/尺寸后先预览，再Bake保存。", MessageType.Info);
        if (GUILayout.Button("Preview / regenerate from seed"))
        {
            Undo.RegisterFullObjectHierarchyUndo(world.gameObject, "Regenerate voxel world");
            world.Generate();
            EditorUtility.SetDirty(world);
        }

        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Bake mesh assets + save scene"))
            {
                WorldLabAuthoring.BakeMeshes(world);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(world.gameObject.scene);
            }
        }
    }
}

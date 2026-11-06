// 职责：用玩法名称展示现有播放槽位并检查配置，保留原序列化字段及 GUID。
// 入口：BlockVfxLibrary Inspector；可从资源管理窗口直接编辑，支持 Undo。
using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BlockVfxLibrary))]
public sealed class BlockVfxLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty bursts = serializedObject.FindProperty("_bursts");
        string[] names = Enum.GetNames(typeof(BlockVfxLibrary.Effect));
        EditorGUILayout.LabelField("一次性特效 / 玩法槽位", EditorStyles.boldLabel);
        if (bursts.arraySize != names.Length)
        {
            EditorGUILayout.HelpBox(
                $"预期 {names.Length} 个槽位，当前 {bursts.arraySize} 个。修复保留前 {names.Length} 槽；多余槽位会移除，新增槽位为空。",
                MessageType.Warning);
            if (GUILayout.Button("修复槽位数量"))
            {
                int previous = bursts.arraySize;
                bursts.arraySize = names.Length;
                for (int i = previous; i < bursts.arraySize; i++)
                {
                    bursts.GetArrayElementAtIndex(i).objectReferenceValue = null;
                }
            }
        }

        for (int i = 0; i < Mathf.Min(names.Length, bursts.arraySize); i++)
        {
            DrawReference(bursts.GetArrayElementAtIndex(i), names[i]);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("持续特效与共享材质", EditorStyles.boldLabel);
        DrawReference(serializedObject.FindProperty("_detachedSmoke"), "残骸烟雾");
        DrawReference(serializedObject.FindProperty("_beam"), "能量光束");
        DrawReference(serializedObject.FindProperty("_lineMaterial"), "共享线条材质");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("挂载特效 / 集中配置", EditorStyles.boldLabel);
        SerializedProperty attached = serializedObject.FindProperty("_attached");
        for (int i = 0; i < attached.arraySize; i++)
        {
            SerializedProperty entry = attached.GetArrayElementAtIndex(i);
            string id = entry.FindPropertyRelative("id").stringValue;
            VfxEffect template = entry.FindPropertyRelative("prefab").objectReferenceValue as VfxEffect;
            string label = id.StartsWith("Authored_") && template != null ? template.name : id;
            DrawReference(entry.FindPropertyRelative("prefab"), label);
        }

        EditorGUILayout.HelpBox(
            "替换此处 Prefab 后，新生成的挂载点统一使用新效果。挂载位置和缩放保留在玩法 Prefab；已生成实例需重新进入 Play 或重新生成。模板不得再引用库中的效果编号。",
            MessageType.Info);
        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawReference(SerializedProperty property, string label)
    {
        EditorGUILayout.PropertyField(property, new GUIContent(label));
        if (property.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox(label + " 未配置，相关效果将不可用。", MessageType.Warning);
        }
    }
}

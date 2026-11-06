// 职责：统一浏览特效/音频资产，编辑原始配置并按需检查引用；不生成副本、不改场景。
// 入口：Tools/HY Sandbox/资源管理/特效与音频；关闭/重载释放内嵌 Inspector。
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class PresentationResourceWindow : EditorWindow
{
    [SerializeField]
    private PresentationAssetIndex.Kind kind;
    [SerializeField]
    private string search = "";
    [SerializeField]
    private bool includeDependencies;
    [SerializeField]
    private string selectedGuid;
    private List<PresentationAssetIndex.Entry> entries = new List<PresentationAssetIndex.Entry>();
    private string category = "";
    private Vector2 listScroll;
    private Vector2 detailScroll;
    private Editor inspector;
    private string[] references;
    private bool needsRefresh = true;
    [MenuItem("Tools/HY Sandbox/资源管理/特效与音频")]
    public static void Open()
    {
        GetWindow<PresentationResourceWindow>("特效与音频资源");
    }

    private void OnEnable()
    {
        minSize = new Vector2(900f, 520f);
        EditorApplication.projectChanged += Invalidate;
        needsRefresh = true;
    }

    private void OnDisable()
    {
        EditorApplication.projectChanged -= Invalidate;
        ReleaseInspector();
    }

    private void Invalidate()
    {
        needsRefresh = true;
        references = null;
        Repaint();
    }

    private void ReleaseInspector()
    {
        if (inspector != null)
        {
            DestroyImmediate(inspector);
        }
    }

    private void OnGUI()
    {
        DrawToolbar();
        if (needsRefresh && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
        {
            entries = PresentationAssetIndex.Scan(kind, includeDependencies);
            needsRefresh = false;
            SelectAsset(selectedGuid);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawList();
            DrawDetails();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUI.BeginChangeCheck();
            kind = (PresentationAssetIndex.Kind)GUILayout.Toolbar((int)kind, new[] { "特效", "音频" }, EditorStyles.toolbarButton, GUILayout.Width(130f));
            includeDependencies = GUILayout.Toggle(includeDependencies, "包含依赖", EditorStyles.toolbarButton, GUILayout.Width(80f));
            if (EditorGUI.EndChangeCheck())
            {
                category = "";
                SelectAsset(null);
                Invalidate();
            }

            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50f)))
            {
                Invalidate();
            }

            if (GUILayout.Button("运行时播放库", EditorStyles.toolbarButton, GUILayout.Width(110f)))
            {
                SelectAsset(AssetDatabase.AssetPathToGUID("Assets/Resources/VFX/BlockVfxLibrary.asset"));
            }
        }
    }

    private void DrawList()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(position.width * 0.45f)))
        {
            string[] categories = new[]
            {
                "全部"
            }.Concat(entries.Select(entry => entry.Category).Distinct()).ToArray();
            int index = string.IsNullOrEmpty(category) ? 0 : System.Array.IndexOf(categories, category);
            int next = EditorGUILayout.Popup("类型", Mathf.Max(0, index), categories);
            category = next == 0 ? "" : categories[next];
            var filtered = entries.Where(entry => PresentationAssetIndex.Matches(entry, search, category)).ToList();
            EditorGUILayout.LabelField($"Assets 范围：{filtered.Count} / {entries.Count}");
            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (PresentationAssetIndex.Entry entry in filtered)
            {
                bool selected = entry.Guid == selectedGuid;
                if (GUILayout.Toggle(
                    selected,
                    new GUIContent($"[{entry.Category}] {System.IO.Path.GetFileNameWithoutExtension(entry.Path)}", entry.Path),
                    "Button") && !selected)
                {
                    SelectAsset(entry.Guid);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    private void SelectAsset(string guid)
    {
        ReleaseInspector();
        selectedGuid = guid;
        references = null;
        detailScroll = Vector2.zero;
        if (!string.IsNullOrEmpty(guid))
        {
            Object asset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null)
            {
                inspector = Editor.CreateEditor(asset);
            }
        }
    }

    private void DrawDetails()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (inspector == null || inspector.target == null)
            {
                EditorGUILayout.HelpBox("选择资源查看原始 Inspector；特效包含 Graph、粒子、拖尾、线条和挂载它们的 Prefab。音频页复用同一资源索引。", MessageType.Info);
                return;
            }

            string path = AssetDatabase.GetAssetPath(inspector.target);
            EditorGUILayout.SelectableLabel(path, EditorStyles.wordWrappedLabel, GUILayout.Height(42f));
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("定位"))
                {
                    EditorGUIUtility.PingObject(inspector.target);
                    Selection.activeObject = inspector.target;
                }

                if (GUILayout.Button("打开编辑"))
                {
                    AssetDatabase.OpenAsset(inspector.target);
                }

                if (GUILayout.Button("查找引用"))
                {
                    references = PresentationAssetIndex.FindReferencers(path);
                }

                if (GUILayout.Button("保存此资源"))
                {
                    AssetDatabase.SaveAssetIfDirty(inspector.target);
                }
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            if (references != null)
            {
                EditorGUILayout.HelpBox($"直接序列化引用：{references.Length}；不包含代码字符串加载和运行时引用。", MessageType.Info);
                foreach (string reference in references)
                {
                    if (GUILayout.Button(reference, EditorStyles.linkLabel))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(reference);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                    }
                }
            }

            // Prefab 通过 Prefab Mode 编辑，避免内嵌根对象 Inspector 隐藏子节点与 override。
            if (inspector.target is GameObject)
            {
                EditorGUILayout.HelpBox("点击“打开编辑”进入 Prefab Mode，编辑完整层级并保存。", MessageType.Info);
            }
            else
            {
                inspector.OnInspectorGUI();
            }

            if (inspector.HasPreviewGUI())
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    GUILayout.Label("Unity 原生预览", EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    inspector.OnPreviewSettings();
                }

                inspector.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(200f, 230f), EditorStyles.helpBox);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}

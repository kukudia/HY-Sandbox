// 职责：WorldLab 建筑库面板、只含网格的放置预览与输入；模板读取失败不影响世界。
// 入口：Update() / OnGUI()；B开关建筑模式，Q/T旋转，左键放置，Esc取消。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-50)]
public sealed class WorldBuildingPalette : MonoBehaviour
{
    [SerializeField]
    private WorldBuildingManager manager;
    [SerializeField, Min(1f)]
    private float placementDistance = 180f;
    private readonly List<string> names = new List<string>();
    private readonly List<BlockDataList> templates = new List<BlockDataList>();
    private readonly List<string> errors = new List<string>();
    private GameObject preview;
    private Material previewMaterial;
    private int selected;
    private int quarterTurns;
    private bool hasPose;
    private bool valid;
    private Vector3 position;
    private Quaternion rotation;
    private string message;
    private Vector2 scroll;
    private UnitFaction faction = UnitFaction.Player;
    public bool IsPlacing
    {
        get; private set;
    }
    public bool BlocksWorldInput => IsPlacing || PointerOverPanel;
    public bool PointerOverPanel => Mouse.current != null && Mouse.current.position.x.ReadValue() > Screen.width - 350 && Mouse.current.position.y.ReadValue() > Screen.height - 480;

    private void Start()
    {
        RefreshCatalog();
    }

    public void RefreshCatalog()
    {
        Cancel();
        names.Clear();
        templates.Clear();
        errors.Clear();
        if (manager.Catalog != null)
        {
            foreach (TextAsset asset in manager.Catalog.blueprints)
            {
                if (asset != null)
                {
                    AddTemplate(asset.name, asset.text);
                }
            }
        }

        string directory = Path.Combine(Application.persistentDataPath, "BuildingBlueprints");
        try
        {
            if (Directory.Exists(directory))
            {
                string[] files = Directory.GetFiles(directory, "*.json");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string path in files)
                {
                    if (new FileInfo(path).Length <= 4 * 1024 * 1024)
                    {
                        AddTemplate(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
                    }
                }
            }
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
        {
            errors.Add(error.Message);
        }

        selected = Mathf.Clamp(selected, 0, templates.Count - 1);
    }

    private void AddTemplate(string name, string json)
    {
        try
        {
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(json);
            WorldBuildingValidation.ValidateBlueprint(blueprint);
            names.Add(name);
            templates.Add(blueprint);
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
        {
            errors.Add(name + ": " + error.Message);
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null || manager == null)
        {
            return;
        }

        if (keyboard.f5Key.wasPressedThisFrame || keyboard.f9Key.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame)
        {
            Cancel();
            return;
        }

        if (keyboard.bKey.wasPressedThisFrame)
        {
            if (IsPlacing)
            {
                Cancel();
            }
            else
            {
                BeginPlacement();
            }
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cancel();
        }

        if (!IsPlacing)
        {
            return;
        }

        if (keyboard.qKey.wasPressedThisFrame || keyboard.tKey.wasPressedThisFrame)
        {
            quarterTurns = (quarterTurns + (keyboard.qKey.wasPressedThisFrame ? 3 : 1)) % 4;
        }

        Vector3 screen = Cursor.lockState == CursorLockMode.Locked ? new Vector3(Screen.width * 0.5f, Screen.height * 0.5f) : (Vector3)mouse.position.ReadValue();
        hasPose = Physics.Raycast(
            GetComponent<Camera>().ScreenPointToRay(screen),
            out RaycastHit hit,
            placementDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore) && hit.collider.GetComponent<VoxelWorldChunk>() != null;
        preview.SetActive(hasPose);
        if (!hasPose)
        {
            message = "Point at terrain.";
            return;
        }

        position = WorldBuildingPlacement.Snap(manager.World, hit.point);
        rotation = manager.World.transform.rotation * Quaternion.Euler(0f, quarterTurns * 90f, 0f);
        preview.transform.SetPositionAndRotation(position, rotation);
        valid = manager.CanPlace(templates[selected], position, rotation, out message);
        Color color = valid ? new Color(0.1f, 0.9f, 0.5f, 0.45f) : new Color(1f, 0.15f, 0.1f, 0.45f);
        previewMaterial.SetColor("_BaseColor", color);
        if (valid && mouse.leftButton.wasPressedThisFrame && !PointerOverPanel)
        {
            try
            {
                manager.Place(templates[selected], names[selected], position, rotation, faction);
            }
            catch (InvalidOperationException error)
            {
                message = error.Message;
            }
        }
    }

    public void BeginPlacement()
    {
        Cancel();
        if (templates.Count == 0)
        {
            message = "No valid building blueprints.";
            return;
        }

        IsPlacing = true;
        preview = new GameObject("Building preview");
        preview.layer = LayerMask.NameToLayer("Ignore Raycast");
        previewMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        previewMaterial.SetFloat("_Surface", 1f);
        previewMaterial.SetFloat("_ZWrite", 0f);
        previewMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        previewMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        previewMaterial.renderQueue = 3000;
        BlockDataList blueprint = templates[selected];
        foreach (BlockData block in blueprint.blocks)
        {
            AddPreviewMeshes(
                Resources.Load<GameObject>(BlueprintBlockFactory.ToResourcePath(block.resourcePath)),
                new Vector3(block.posX, block.posY, block.posZ),
                new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW),
                new Vector3(block.x, block.y, block.z));
        }
    }

    private void AddPreviewMeshes(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3? size = null)
    {
        var renderers = new HashSet<Renderer>();
        foreach (LODGroup lod in prefab.GetComponentsInChildren<LODGroup>())
        {
            LOD[] levels = lod.GetLODs();
            for (int i = 1; i < levels.Length; i++)
            {
                foreach (Renderer renderer in levels[i].renderers)
                {
                    renderers.Add(renderer);
                }
            }
        }

        Block block = prefab.GetComponent<Block>();
        Vector3 scale = size.HasValue ? new Vector3(size.Value.x / block.x, size.Value.y / block.y, size.Value.z / block.z) : Vector3.one;
        // 只复制 Mesh，不实例化含供电、机器人、耐久回调的功能 Prefab。
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null || renderers.Contains(filter.GetComponent<Renderer>()))
            {
                continue;
            }

            var part = new GameObject("Preview mesh");
            part.layer = LayerMask.NameToLayer("Ignore Raycast");
            part.transform.SetParent(preview.transform, false);
            part.transform.localPosition = localPosition + localRotation * Vector3.Scale(prefab.transform.InverseTransformPoint(filter.transform.position), scale);
            part.transform.localRotation = localRotation * Quaternion.Inverse(prefab.transform.rotation) * filter.transform.rotation;
            part.transform.localScale = Vector3.Scale(filter.transform.lossyScale, scale);
            part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>();
            var materials = new Material[filter.sharedMesh.subMeshCount];
            Array.Fill(materials, previewMaterial);
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void Cancel()
    {
        IsPlacing = false;
        if (preview != null)
        {
            preview.SetActive(false);
            Destroy(preview);
        }

        if (previewMaterial != null)
        {
            Destroy(previewMaterial);
        }
    }

    private void OnDisable()
    {
        Cancel();
    }

    private void OnGUI()
    {
        if (manager == null)
        {
            return;
        }

        GUILayout.BeginArea(new Rect(Screen.width - 342, 18, 324, 452), GUI.skin.box);
        GUILayout.Label("BUILDINGS / B to place");
        GUILayout.Label("Q / T rotate · LMB place · Esc cancel");
        if (GUILayout.Button("Refresh blueprint library"))
        {
            RefreshCatalog();
        }

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(180));
        for (int i = 0; i < names.Count; i++)
        {
            if (GUILayout.Button((selected == i ? "> " : "") + names[i]))
            {
                selected = i;
                BeginPlacement();
            }
        }

        GUILayout.EndScrollView();
        if (GUILayout.Button("Faction: " + faction))
        {
            faction = faction == UnitFaction.Player ? UnitFaction.Enemy : UnitFaction.Player;
        }

        GUILayout.Label(IsPlacing ? message : manager.LastMessage);
        int unstable = 0;
        foreach (BuildingRuntime building in manager.Buildings)
        {
            if (!building.Anchored || building.SupportRatio < 1f)
            {
                unstable++;
            }
        }

        GUILayout.Label($"Buildings {manager.Buildings.Count} · unstable {unstable}");
        if (errors.Count > 0)
        {
            GUILayout.Label("Skipped: " + errors[0]);
        }

        GUILayout.EndArea();
    }
}

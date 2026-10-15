// 职责：在建筑编辑面板配置据点类型、敌机蓝图快照和逐名驻军本地部署姿态。
// 入口：Refresh() / AddDefender() / SavePose() / RemoveDefender() / SetOutpost()。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildingOutpostEditor : MonoBehaviour
{
    [SerializeField]
    private Toggle _isOutpost;
    [SerializeField]
    private Dropdown _blueprint;
    [SerializeField]
    private Dropdown _defender;
    [SerializeField]
    private InputField _x;
    [SerializeField]
    private InputField _y;
    [SerializeField]
    private InputField _z;
    [SerializeField]
    private InputField _yaw;
    [SerializeField]
    private Text _status;
    [SerializeField]
    private Dropdown _buildingTemplate;
    private readonly List<BlockDataList> templates = new List<BlockDataList>();
    private readonly List<string> names = new List<string>();
    private bool refreshing;
    private GameObject preview;
    public void Refresh()
    {
        refreshing = true;
        templates.Clear();
        names.Clear();
        _buildingTemplate.ClearOptions();
        var buildingNames = new List<string>();
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>("WorldBlueprints/Buildings"))
        {
            buildingNames.Add(asset.name);
        }

        _buildingTemplate.AddOptions(buildingNames);
        foreach (TextAsset asset in Resources.LoadAll<TextAsset>("WorldBlueprints/Enemies"))
        {
            AddTemplate(asset.name, asset.text);
        }

        SaveManager.instance.GetAllEnemyBlueprintNames();
        foreach (string name in SaveManager.instance.enemyBlueprints)
        {
            try
            {
                AddTemplate(name, File.ReadAllText(SaveManager.instance.GetEnemyBlueprintPath(name)));
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            {
                Debug.LogWarning("Skipping defender blueprint " + name + ": " + error.Message);
            }
        }

        _blueprint.ClearOptions();
        _blueprint.AddOptions(names);
        EnemyOutpostData data = SaveManager.instance.cachedData.IsOutpost ? SaveManager.instance.cachedData.outpost : null;
        _isOutpost.isOn = data != null;
        RefreshSlots(data);
        _status.text = data == null ? "Ordinary building. Choose a defender and enable Enemy outpost." : "Defeated defenders stay defeated during this expedition. Local positions are in metres.";
        refreshing = false;
        SelectDefender();
    }

    private void AddTemplate(string name, string json)
    {
        BlockDataList data = JsonUtility.FromJson<BlockDataList>(json);
        if (data?.blocks == null || data.blocks.Count > 256 || !EnemyBlueprintFactory.IsValidBlueprint(data, name))
        {
            return;
        }

        templates.Add(data);
        names.Add(name);
    }

    public void CopyBuildingTemplate()
    {
        if (_buildingTemplate.options.Count == 0)
        {
            return;
        }

        try
        {
            string name = SaveManager.instance.CopyBundledBuilding(_buildingTemplate.options[_buildingTemplate.value].text);
            BuildManager.instance.EnterBuildingBlueprintBuildMode(name);
        }
        catch (Exception error) when (error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException)
        {
            _status.text = "Could not copy template: " + error.Message;
        }
    }

    private void RefreshSlots(EnemyOutpostData data)
    {
        int selected = _defender.value;
        var labels = new List<string>();
        if (data != null)
        {
            for (int i = 0; i < data.garrison.Count; i++)
            {
                labels.Add((i + 1) + ". " + data.garrison[i].name);
            }
        }

        _defender.ClearOptions();
        _defender.AddOptions(labels);
        _defender.SetValueWithoutNotify(Mathf.Clamp(selected, 0, Mathf.Max(0, labels.Count - 1)));
    }

    private EnemyOutpostData Copy()
    {
        EnemyOutpostData current = SaveManager.instance.cachedData.outpost;
        return current == null || !current.enabled ? new EnemyOutpostData
        {
            enabled = true
        }

        : JsonUtility.FromJson<EnemyOutpostData>(JsonUtility.ToJson(current));
    }

    public void SetOutpost()
    {
        if (refreshing)
        {
            return;
        }

        if (_isOutpost.isOn)
        {
            AddDefender();
        }
        else
        {
            Save(null);
        }
    }

    public void AddDefender()
    {
        if (templates.Count == 0)
        {
            _status.text = "Create an enemy blueprint first.";
            _isOutpost.SetIsOnWithoutNotify(false);
            return;
        }

        EnemyOutpostData data = Copy();
        if (data.garrison.Count >= 8)
        {
            _status.text = "Maximum 8 defenders per outpost.";
            return;
        }

        data.garrison.Add(new OutpostGarrisonData { id = Guid.NewGuid().ToString("N"), name = names[_blueprint.value], blueprint = new OutpostEnemyBlueprint { blocks = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(templates[_blueprint.value])).blocks }, position = Quaternion.Euler(0f, data.garrison.Count * 45f, 0f) * new Vector3(-40f, 12f, 0f) });
        if (Save(data))
        {
            _defender.SetValueWithoutNotify(data.garrison.Count - 1);
            SelectDefender();
        }
    }

    public void SelectDefender()
    {
        if (refreshing)
        {
            return;
        }

        EnemyOutpostData data = SaveManager.instance.cachedData.IsOutpost ? SaveManager.instance.cachedData.outpost : null;
        if (data == null || data.garrison.Count == 0)
        {
            ClearPreview();
            return;
        }

        OutpostGarrisonData guard = data.garrison[Mathf.Clamp(_defender.value, 0, data.garrison.Count - 1)];
        _x.text = guard.position.x.ToString(CultureInfo.InvariantCulture);
        _y.text = guard.position.y.ToString(CultureInfo.InvariantCulture);
        _z.text = guard.position.z.ToString(CultureInfo.InvariantCulture);
        _yaw.text = guard.yaw.ToString(CultureInfo.InvariantCulture);
        ShowPreview(data);
    }

    public void SavePose()
    {
        EnemyOutpostData data = Copy();
        if (data.garrison.Count == 0)
        {
            _status.text = "Add a defender first.";
            return;
        }

        if (!Parse(_x, out float x) || !Parse(_y, out float y) || !Parse(_z, out float z) || !Parse(_yaw, out float yaw))
        {
            _status.text = "Enter finite numbers. Height must be at least 1m; deployment within 128m.";
            return;
        }

        data.garrison[_defender.value].position = new Vector3(x, y, z);
        data.garrison[_defender.value].yaw = yaw;
        Save(data);
    }

    private static bool Parse(InputField field, out float value)
    {
        return float.TryParse(field.text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && WorldBuildingValidation.Finite(value);
    }

    public void RemoveDefender()
    {
        EnemyOutpostData data = Copy();
        if (data.garrison.Count > 0)
        {
            data.garrison.RemoveAt(Mathf.Clamp(_defender.value, 0, data.garrison.Count - 1));
            Save(data.garrison.Count > 0 ? data : null);
        }
    }

    private bool Save(EnemyOutpostData data)
    {
        try
        {
            SaveManager.instance.SaveBuildingOutpost(data);
            refreshing = true;
            _isOutpost.SetIsOnWithoutNotify(data != null);
            RefreshSlots(data);
            refreshing = false;
            SelectDefender();
            _status.text = data != null ? "Saved outpost: " + data.garrison.Count + " defenders. Blueprint snapshots are embedded." : "Saved as ordinary building.";
            return true;
        }
        catch (Exception error) when (error is InvalidOperationException || error is IOException || error is UnauthorizedAccessException)
        {
            refreshing = false;
            _isOutpost.SetIsOnWithoutNotify(SaveManager.instance.cachedData.IsOutpost);
            _status.text = "Could not save: " + error.Message;
            return false;
        }
    }

    private void ShowPreview(EnemyOutpostData data)
    {
        ClearPreview();
        preview = new GameObject("Outpost deployment preview");
        for (int i = 0; i < data.garrison.Count; i++)
        {
            OutpostGarrisonData guard = data.garrison[i];
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Defender " + (i + 1) + " " + guard.name;
            Destroy(marker.GetComponent<Collider>());
            marker.transform.SetParent(preview.transform, false);
            marker.transform.SetPositionAndRotation(guard.position, Quaternion.Euler(0f, guard.yaw, 0f));
            marker.transform.localScale = new Vector3(0.5f, 0.5f, 2f);
            var label = new GameObject("Label");
            label.transform.SetParent(preview.transform, false);
            label.transform.position = guard.position + Vector3.up;
            TextMesh text = label.AddComponent<TextMesh>();
            text.text = (i + 1) + " " + guard.name;
            text.characterSize = 0.25f;
            text.fontSize = 32;
        }
    }

    private void ClearPreview()
    {
        if (preview != null)
        {
            preview.SetActive(false);
            Destroy(preview);
            preview = null;
        }
    }

    private void OnDisable()
    {
        ClearPreview();
    }
}

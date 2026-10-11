// 职责：严格解析 CSV、校验完整配置，并按白名单字段构造组件 JSON；不修改资源文件。
// 入口：Parse() / Apply()；支持逗号、引号、多行说明与 UTF-8 BOM。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public sealed class BlockCsvDocument
{
    public const string AssetPath = "Assets/Resources/BlockData/Blocks.csv";
    public const string ResourcePath = "BlockData/Blocks";
    public static readonly string[] Identity =
    {
        "Prefab名称",
        "Prefab路径",
        "GUID",
        "组件（只读）"
    };
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private readonly Dictionary<string, List<string>> _rows = new Dictionary<string, List<string>>();
    private readonly List<BlockDataTableSchema.Column> _columns = BlockDataTableSchema.CreateColumns();
    public int RowCount => _rows.Count;

    [Serializable]
    private sealed class StringValue
    {
        public string value;
    }

    public static BlockCsvDocument Parse(string text)
    {
        var document = new BlockCsvDocument();
        List<List<string>> records = ParseRecords(text.TrimStart('\uFEFF'));
        string[] expected = Identity.Concat(document._columns.Select(column => column.Id + " | " + column.Label)).ToArray();
        if (records.Count == 0 || records[0].Count != expected.Length || records[0].Distinct().Count() != expected.Length || expected.Any(name => !records[0].Contains(name)))
        {
            throw new InvalidDataException("Blocks.csv 列名缺失、重复或不匹配；不要修改列名，允许调整列顺序。");
        }

        var indexes = records[0].Select((name, index) => new { name, index }).ToDictionary(item => item.name, item => item.index);
        for (int r = 1; r < records.Count; r++)
        {
            var record = records[r];
            if (record.Count != expected.Length)
            {
                throw new InvalidDataException($"Blocks.csv 第 {r + 1} 条记录列数错误。");
            }

            string guid = record[indexes["GUID"]];
            string path = record[indexes["Prefab路径"]];
            if (guid.Length != 32 || !guid.All(Uri.IsHexDigit) || document._rows.ContainsKey(guid) || !path.StartsWith("Assets/Resources/Blocks/", StringComparison.Ordinal) || !path.EndsWith(".prefab", StringComparison.Ordinal) || path.Contains(".."))
            {
                throw new InvalidDataException($"Blocks.csv 第 {r + 1} 条记录身份无效或 GUID 重复。");
            }

            var values = Identity.Select(name => record[indexes[name]]).ToList();
            foreach (var column in document._columns)
            {
                string value = record[indexes[column.Id + " | " + column.Label]];
                values.Add(value.Length == 0 ? "" : Normalize(column, value, values[0]));
            }

            document.ValidateRow(values);
            document._rows.Add(guid, values);
        }

        return document;
    }

    public bool Apply(GameObject root, string guid)
    {
        if (!_rows.TryGetValue(guid, out List<string> values))
        {
            return false;
        }

        var componentJson = new Dictionary<Component, List<string>>();
        bool customMass = false;
        float mass = 0f;
        for (int i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            string value = values[i + Identity.Length];
            if (value.Length == 0)
            {
                continue;
            }

            Component[] targets = column.FindTargets(root);
            if (targets.Length == 0 || (targets.Length > 1 && column.Id != "requiresPower"))
            {
                throw new InvalidDataException($"{root.name} / {column.Id}：要求唯一组件，实际 {targets.Length} 个。");
            }

            if (column.Id == "Block.mass")
            {
                customMass = true;
                mass = float.Parse(value, Invariant);
                continue;
            }

            // 仅白名单里的序列化标量字段进入 JSON；引用、存档内容和运行时状态保持原值。
            string jsonValue = column.Kind == BlockDataTableSchema.ValueKind.String ? Quote(value) : value;
            foreach (Component target in targets)
            {
                if (!componentJson.TryGetValue(target, out List<string> properties))
                {
                    properties = new List<string>();
                    componentJson.Add(target, properties);
                }

                properties.Add(Quote(column.Property) + ":" + jsonValue);
            }
        }

        Block block = root.GetComponent<Block>();
        Vector3Int previousSize = block != null ? new Vector3Int(block.x, block.y, block.z) : Vector3Int.zero;
        bool previouslyHadConnectors = block != null && block.canSpawnConnector;
        foreach (var pair in componentJson)
        {
            JsonUtility.FromJsonOverwrite("{" + string.Join(",", pair.Value) + "}", pair.Key);
        }

        if (block != null)
        {
            block.SetMassSettings(customMass, mass);
            if (Application.isPlaying)
            {
                bool shapeChanged = previousSize != new Vector3Int(block.x, block.y, block.z) || (!previouslyHadConnectors && block.canSpawnConnector);
                block.RefreshRuntimeData(shapeChanged);
            }
            else
            {
                block.RefreshAuthoringData();
            }
        }

        return true;
    }

    public string PathFor(string guid)
    {
        return _rows.TryGetValue(guid, out var values) ? values[1] : null;
    }

    public IEnumerable<string> Guids => _rows.Keys;

    private void ValidateRow(List<string> values)
    {
        string Value(string id)
        {
            return values[_columns.FindIndex(column => column.Id == id) + Identity.Length];
        }

        string minimum = Value("Power.minWorkingPower");
        string standard = Value("Power.standardWorkingPower");
        if (minimum.Length > 0 && standard.Length > 0 && float.Parse(minimum, Invariant) > float.Parse(standard, Invariant))
        {
            throw new InvalidDataException(values[0] + "：最低工作功率不能大于额定工作功率。");
        }

        if (Value("Block.info").Length > 0 && Value("Block.canEditInfo") != "true")
        {
            throw new InvalidDataException(values[0] + "：填写说明文本需要开启 canEditInfo。");
        }
    }

    private static string Normalize(BlockDataTableSchema.Column column, string value, string name)
    {
        switch (column.Kind)
        {
            case BlockDataTableSchema.ValueKind.Boolean:
                if (value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return "true";
                }

                if (value == "0" || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return "false";
                }

                break;
            case BlockDataTableSchema.ValueKind.Integer:
                if (int.TryParse(value, NumberStyles.Integer, Invariant, out int integer) && integer >= column.Minimum && integer <= column.Maximum)
                {
                    return integer.ToString(Invariant);
                }

                break;
            case BlockDataTableSchema.ValueKind.Float:
                if (float.TryParse(value, NumberStyles.Float, Invariant, out float number) && !float.IsNaN(number) && !float.IsInfinity(number) && number >= column.Minimum && number <= column.Maximum)
                {
                    return number.ToString("R", Invariant);
                }

                break;
            case BlockDataTableSchema.ValueKind.Enum:
                int index = Array.IndexOf(column.EnumNames, value);
                if (index >= 0)
                {
                    return index.ToString(Invariant);
                }

                break;
            case BlockDataTableSchema.ValueKind.String:
                return value;
        }

        throw new InvalidDataException($"{name} / {column.Label}：值 {value} 无效；检查类型、枚举名称或范围 [{column.Minimum}, {column.Maximum}]。");
    }

    private static string Quote(string text)
    {
        string json = JsonUtility.ToJson(new StringValue { value = text });
        return json.Substring(9, json.Length - 10);
    }

    public static string WriteRecords(IEnumerable<IEnumerable<string>> records)
    {
        var csv = new StringBuilder();
        foreach (var record in records)
        {
            csv.Append(string.Join(",", record.Select(value => "\"" + value.Replace("\"", "\"\"") + "\"")));
            csv.Append("\r\n");
        }

        return csv.ToString();
    }

    public static List<List<string>> ParseRecords(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        bool quoted = false;
        bool closed = false;
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (quoted)
            {
                if (character == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = false;
                        closed = true;
                    }
                }
                else
                {
                    field.Append(character);
                }

                continue;
            }

            if (character == ',' || character == '\r' || character == '\n')
            {
                record.Add(field.ToString());
                field.Clear();
                closed = false;
                if (character != ',')
                {
                    records.Add(record);
                    record = new List<string>();
                    if (character == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                }
            }
            else if (character == '"' && field.Length == 0 && !closed)
            {
                quoted = true;
            }
            else if (closed || character == '"')
            {
                throw new InvalidDataException("CSV 引号格式错误。");
            }
            else
            {
                field.Append(character);
            }
        }

        if (quoted)
        {
            throw new InvalidDataException("CSV 存在未闭合引号。");
        }

        if (closed || field.Length > 0 || record.Count > 0)
        {
            record.Add(field.ToString());
            records.Add(record);
        }

        return records;
    }
}

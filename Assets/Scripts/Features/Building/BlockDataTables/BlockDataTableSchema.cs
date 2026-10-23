// 职责：定义 CSV 支持的配置字段、类型、枚举和有效范围，Editor 与运行时共享同一契约。
// 入口：CreateColumns()；不包含运行时状态或资源引用。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BlockDataTableSchema
{
    public enum ValueKind
    {
        Boolean,
        Integer,
        Float,
        String,
        Enum
    }

    public sealed class Column
    {
        public ValueKind Kind;
        public string[] EnumNames;
        public string Group;
        public Type ComponentType;
        public string Property;
        public string Label;
        public double Minimum;
        public double Maximum;
        public string Id => Property == "_requiresPower" ? "requiresPower" : ComponentType.Name + "." + Property;

        public Component[] FindTargets(GameObject root)
        {
            Component[] targets = root.GetComponentsInChildren(ComponentType, true);
            return Id == "requiresPower" ? targets.Concat(root.GetComponentsInChildren<CollectionBot>(true)).Concat(root.GetComponentsInChildren<RepairBot>(true)).ToArray() : targets;
        }

        public Column(
            string group,
            Type type,
            string property,
            string label,
            double minimum,
            double maximum,
            ValueKind kind,
            string[] enumNames)
        {
            Kind = kind;
            EnumNames = enumNames;
            Group = group;
            ComponentType = type;
            Property = property;
            Label = label;
            Minimum = minimum;
            Maximum = maximum;
        }
    }

    public static readonly string[] Groups =
    {
        "基础",
        "动力",
        "武器",
        "货仓",
        "无人机"
    };
    public static List<Column> CreateColumns()
    {
        // CSV 只负责常用平衡数值；高级行为与表现参数保留在 Prefab Inspector。
        var columns = new List<Column>();
        AddInteger<Block>(columns, "基础", "cost", "建造成本");
        AddFloat<Block>(columns, "基础", "mass", "质量 kg", 0.000001);
        AddFloat<Durability>(columns, "基础", "maxDurability", "最大耐久", 0.000001);
        AddFloat<Power>(columns, "基础", "minWorkingPower", "最低工作功率");
        AddFloat<Power>(columns, "基础", "standardWorkingPower", "额定工作功率");
        AddInteger<Block>(
            columns,
            "基础",
            "x",
            "尺寸 X",
            1,
            64);
        AddInteger<Block>(
            columns,
            "基础",
            "y",
            "尺寸 Y",
            1,
            64);
        AddInteger<Block>(
            columns,
            "基础",
            "z",
            "尺寸 Z",
            1,
            64);
        AddBoolean<Block>(columns, "基础", "canExplode", "销毁时爆炸");
        AddFloat<Block>(columns, "基础", "explosionForce", "爆炸力度");
        AddFloat<Block>(columns, "基础", "explosionRadius", "爆炸半径 m");
        AddFloat<Thruster>(columns, "动力", "maxThrust", "最大推力 N");
        AddFloat<PowerGeneratingUnit>(columns, "动力", "outputPower", "发电量");
        AddInteger<PowerTransmissionDevice>(columns, "动力", "powerRange", "供电盒半边长 m");
        AddInteger<PowerTransmissionDevice>(columns, "动力", "maxConnectionDistance", "连接盒半边长 m");
        AddFloat<Turret>(columns, "武器", "range", "射程 m");
        AddFloat<Turret>(columns, "武器", "damage", "单发伤害");
        AddFloat<Turret>(columns, "武器", "fireInterval", "射击间隔 s", 0.000001);
        AddFloat<ProjectileTurret>(columns, "武器", "_projectileSpeed", "弹速 m/s", 1);
        AddEnum<CargoHold>(columns, "货仓", "_kind", "货物类型", new[] { "SpecialPart", "Coins", "Technology" });
        AddInteger<CargoHold>(columns, "货仓", "_capacity", "容量", 1);
        AddBoolean<CargoHold>(columns, "货仓", "_requiresPower", "需要供电");
        AddInteger<Bot>(columns, "无人机", "targetRange", "搜索范围");
        AddFloat<Bot>(columns, "无人机", "movementSpeed", "移动速度 m/s");
        AddFloat<RepairBot>(columns, "无人机", "repairAmount", "单次维修量");
        AddFloat<RepairBot>(columns, "无人机", "repairCooldown", "维修间隔 s", 0.000001);
        return columns;
    }

    private static void AddFloat<T>(
        List<Column> columns,
        string group,
        string property,
        string label,
        double minimum = 0,
        double maximum = double.PositiveInfinity)
    {
        columns.Add(new Column(
            group,
            typeof(T),
            property,
            label,
            minimum,
            maximum,
            ValueKind.Float,
            null));
    }

    private static void AddInteger<T>(
        List<Column> columns,
        string group,
        string property,
        string label,
        double minimum = 0,
        double maximum = double.PositiveInfinity)
    {
        columns.Add(new Column(
            group,
            typeof(T),
            property,
            label,
            minimum,
            maximum,
            ValueKind.Integer,
            null));
    }

    private static void AddBoolean<T>(List<Column> columns, string group, string property, string label)
    {
        columns.Add(new Column(
            group,
            typeof(T),
            property,
            label,
            double.NegativeInfinity,
            double.PositiveInfinity,
            ValueKind.Boolean,
            null));
    }

    private static void AddEnum<T>(List<Column> columns, string group, string property, string label, string[] names)
    {
        columns.Add(new Column(
            group,
            typeof(T),
            property,
            label,
            double.NegativeInfinity,
            double.PositiveInfinity,
            ValueKind.Enum,
            names));
    }
}

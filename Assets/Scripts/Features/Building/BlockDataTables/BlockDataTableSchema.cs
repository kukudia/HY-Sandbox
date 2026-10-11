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
        "无人机",
        "高级·尺寸碰撞",
        "高级·导航",
        "高级·表现"
    };
    public static List<Column> CreateColumns()
    {
        var columns = new List<Column>();
        AddInteger<Block>(columns, "基础", "cost", "建造成本");
        AddFloat<Block>(columns, "基础", "density", "旧密度（不影响质量）");
        AddFloat<Block>(columns, "基础", "mass", "质量 kg", 0.000001);
        AddFloat<Durability>(columns, "基础", "maxDurability", "最大耐久", 0.000001);
        AddFloat<Power>(columns, "基础", "minWorkingPower", "最低工作功率");
        AddFloat<Power>(columns, "基础", "standardWorkingPower", "额定工作功率");
        AddBoolean<Block>(columns, "基础", "canRotate", "允许旋转");
        AddBoolean<Block>(columns, "基础", "canBeDeleted", "允许删除");
        AddBoolean<Block>(columns, "基础", "canExplode", "销毁时爆炸");
        AddFloat<Block>(columns, "基础", "explosionForce", "爆炸力度");
        AddFloat<Block>(columns, "基础", "explosionRadius", "爆炸半径 m");
        AddFloat<Block>(
            columns,
            "基础",
            "explosionDisconnectProbability",
            "爆炸断连概率",
            0,
            1);
        AddBoolean<Block>(columns, "基础", "canEditInfo", "自定义说明");
        AddString<Block>(columns, "基础", "info", "说明文本");
        AddFloat<Thruster>(columns, "动力", "maxThrust", "最大推力 N");
        AddFloat<Thruster>(columns, "动力", "maxThrustChangeRate", "推力变化率 N/s");
        AddFloat<PowerGeneratingUnit>(columns, "动力", "outputPower", "发电量");
        AddInteger<PowerTransmissionDevice>(columns, "动力", "powerRange", "供电盒半边长 m");
        AddInteger<PowerTransmissionDevice>(columns, "动力", "maxConnectionDistance", "连接盒半边长 m");
        AddFloat<Turret>(columns, "武器", "range", "射程 m");
        AddFloat<Turret>(columns, "武器", "damage", "单发伤害");
        AddFloat<Turret>(columns, "武器", "fireInterval", "射击间隔 s", 0.000001);
        AddFloat<Turret>(columns, "武器", "horizontalTurnSpeed", "水平转速 °/s");
        AddFloat<Turret>(columns, "武器", "verticalTurnSpeed", "垂直转速 °/s");
        AddFloat<Turret>(
            columns,
            "武器",
            "minElevation",
            "最低俯仰 °",
            -89,
            0);
        AddFloat<Turret>(
            columns,
            "武器",
            "maxElevation",
            "最高俯仰 °",
            0,
            89);
        AddFloat<Turret>(
            columns,
            "武器",
            "maxFireAngle",
            "允许开火偏角 °",
            0,
            180);
        AddFloat<Turret>(columns, "武器", "targetRefreshInterval", "索敌间隔 s", 0.000001);
        AddEnum<Turret>(columns, "武器", "targetFaction", "默认目标阵营", new[] { "Player", "Enemy", "Neutral" });
        AddBoolean<Turret>(columns, "武器", "_isStarted", "默认启动");
        AddFloat<ProjectileTurret>(columns, "武器", "_projectileSpeed", "弹速 m/s", 1);
        AddFloat<ProjectileTurret>(columns, "武器", "_projectileLifetime", "炮弹寿命 s", 0.05);
        AddFloat<ProjectileTurret>(columns, "武器", "_impactImpulse", "命中冲量 N·s");
        AddEnum<CargoHold>(columns, "货仓", "_kind", "货物类型", new[] { "SpecialPart", "Coins", "Technology" });
        AddInteger<CargoHold>(columns, "货仓", "_capacity", "容量", 1);
        AddBoolean<CargoHold>(columns, "货仓", "_requiresPower", "需要供电");
        AddBoolean<CargoHold>(columns, "货仓", "_dropPlayerContents", "玩家货物掉落");
        AddBoolean<CargoHold>(columns, "货仓", "_dropEnemyContents", "敌方货物掉落");
        AddInteger<Bot>(columns, "无人机", "targetRange", "搜索范围");
        AddFloat<Bot>(columns, "无人机", "findTargetInterval", "搜索间隔 s", 0.000001);
        AddFloat<Bot>(columns, "无人机", "movementSpeed", "移动速度 m/s");
        AddFloat<Bot>(columns, "无人机", "rotationSpeed", "转向响应");
        AddFloat<RepairBot>(columns, "无人机", "repairAmount", "单次维修量");
        AddFloat<RepairBot>(columns, "无人机", "repairCooldown", "维修间隔 s", 0.000001);
        AddFloat<CollectionBot>(columns, "无人机", "_pickupDistance", "拾取距离 m", 0.1);
        AddFloat<CollectionBot>(columns, "无人机", "_missionTimeout", "任务超时 s", 1);
        AddInteger<Block>(
            columns,
            "高级·尺寸碰撞",
            "x",
            "尺寸 X",
            1,
            64);
        AddInteger<Block>(
            columns,
            "高级·尺寸碰撞",
            "y",
            "尺寸 Y",
            1,
            64);
        AddInteger<Block>(
            columns,
            "高级·尺寸碰撞",
            "z",
            "尺寸 Z",
            1,
            64);
        AddBoolean<Block>(columns, "高级·尺寸碰撞", "canSpawnConnector", "生成连接点");
        AddBoolean<Block>(columns, "高级·尺寸碰撞", "showConnectors", "显示连接点");
        AddBoolean<Block>(columns, "高级·尺寸碰撞", "showLabel", "显示标签");
        AddFloat<Durability>(columns, "高级·尺寸碰撞", "collisionSpeedThreshold", "碰撞起伤速度 m/s");
        AddFloat<Durability>(columns, "高级·尺寸碰撞", "damageMultiplier", "碰撞损伤 / J");
        AddFloat<Durability>(
            columns,
            "高级·尺寸碰撞",
            "_collisionDamageScale",
            "碰撞损伤倍率",
            0,
            1);
        AddFloat<Durability>(
            columns,
            "高级·尺寸碰撞",
            "_maximumCollisionHealthFraction",
            "单次耐久损失上限",
            0,
            1);
        AddFloat<Durability>(columns, "高级·尺寸碰撞", "_collisionDamageCooldown", "碰撞损伤冷却 s");
        AddFloat<Bot>(columns, "高级·导航", "emergencyAvoidanceRange", "紧急避障距离 m");
        AddFloat<Bot>(columns, "高级·导航", "predictiveAvoidanceRange", "预测避障距离 m");
        AddInteger<Bot>(
            columns,
            "高级·导航",
            "avoidanceRays",
            "避障采样数",
            1,
            128);
        AddFloat<Bot>(columns, "高级·导航", "directionUpdateInterval", "方向更新间隔 s", 0.05);
        AddFloat<Bot>(columns, "高级·导航", "directionResponseRate", "方向响应", 0.1);
        AddFloat<Bot>(columns, "高级·导航", "_navigationClearanceRadius", "机体净空半径 m", 0.05);
        AddFloat<Bot>(columns, "高级·导航", "_navigationStuckTimeout", "脱困超时 s", 0.5);
        AddFloat<Bot>(columns, "高级·导航", "_navigationProgressDistance", "有效前进距离 m", 0.05);
        AddFloat<Bot>(columns, "高级·导航", "_navigationDetourHoldTime", "绕障保持时间 s", 0.2);
        AddFloat<Bot>(
            columns,
            "高级·导航",
            "returnAlignmentAngle",
            "返航对齐角 °",
            0.1,
            180);
        AddFloat<Bot>(columns, "高级·导航", "returnBrakeDistance", "返航制动距离 m", 0.1);
        AddFloat<Bot>(columns, "高级·导航", "returnPositionTolerance", "停靠位置容差 m", 0.01);
        AddFloat<Bot>(columns, "高级·导航", "returnStopSpeed", "停靠速度阈值 m/s", 0.01);
        AddFloat<Bot>(columns, "高级·导航", "returnBrakingAcceleration", "返航制动加速度", 0.1);
        AddFloat<Bot>(columns, "高级·导航", "dockingApproachHeight", "停靠进场高度 m", 0.1);
        AddFloat<Bot>(columns, "高级·导航", "dockingApproachTolerance", "进场距离容差 m", 0.01);
        AddFloat<Bot>(columns, "高级·导航", "dockingPositionSpeed", "停靠速度 m/s", 0.1);
        AddFloat<ProjectileTurret>(columns, "高级·表现", "_recoilDistance", "后坐距离 m");
        AddFloat<ProjectileTurret>(columns, "高级·表现", "_recoilKickDuration", "后坐时间 s", 0.001);
        AddFloat<ProjectileTurret>(columns, "高级·表现", "_recoilReturnDuration", "复位时间 s", 0.001);
        AddFloat<Thruster>(columns, "高级·表现", "visualizationInterval", "推进视觉刷新 s", 0.001);
        AddBoolean<MainThruster>(columns, "高级·表现", "alignVisual", "主推进模型对齐");
        AddFloat<MainThruster>(columns, "高级·表现", "rotationSpeed", "主推进模型响应");
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

    private static void AddString<T>(List<Column> columns, string group, string property, string label)
    {
        columns.Add(new Column(
            group,
            typeof(T),
            property,
            label,
            double.NegativeInfinity,
            double.PositiveInfinity,
            ValueKind.String,
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

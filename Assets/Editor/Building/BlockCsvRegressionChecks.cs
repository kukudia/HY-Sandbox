// 职责：用真实维修仓跨物理帧验证 CSV、供电、离仓、维修和断电恢复，隔离创建并清理测试对象。
// 入口：Begin()（Play Mode）；结果写入 Temp/BlockCsvChecks/regression-play.json，不修改制作资产或场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlockCsvRegressionChecks
{
    [Serializable]
    private sealed class Report
    {
        public string status = "running";
        public string error;
        public List<string> checks = new List<string>();
    }

    private static GameObject _fixture;
    private static RepairBot _bot;
    private static Durability _target;
    private static Power _power;
    private static PowerGeneratingUnit _generator;
    private static Report _report;
    private static float _phaseStarted;
    private static int _phase;
    private static bool _originalPlayMode;
    private static PlayManager _play;
    public static string Begin()
    {
        if (!EditorApplication.isPlaying || _fixture != null)
        {
            throw new InvalidOperationException("Requires Play Mode and no active probe.");
        }

        _report = new Report();
        _play = PlayManager.instance;
        _originalPlayMode = _play.playMode;
        _fixture = new GameObject("CSV repair regression fixture");
        _fixture.SetActive(false);
        _fixture.transform.position = Vector3.one * 5000f;
        _fixture.AddComponent<Rigidbody>().isKinematic = true;
        var owner = _fixture.AddComponent<ControlUnit>();
        owner.enabled = false;
        var cockpit = new GameObject("CSV fixture cockpit");
        cockpit.transform.SetParent(_fixture.transform, false);
        owner.cockpit = cockpit.AddComponent<Cockpit>();
        owner.hasValidCockpit = true;
        var bay = Object.Instantiate(Resources.Load<GameObject>("Blocks/RepairBotContianer"), _fixture.transform);
        _bot = bay.GetComponentInChildren<RepairBot>(true);
        _power = bay.GetComponent<Power>();
        var targetObject = Object.Instantiate(Resources.Load<GameObject>("Blocks/1x1x1"), _fixture.transform);
        targetObject.transform.localPosition = Vector3.right * 3f;
        _target = targetObject.GetComponent<Durability>();
        // 验证 CSV Awake 覆盖错误的实例值，随后真实 Start/FixedUpdate 执行任务。
        _bot.repairAmount = -1f;
        _power.minWorkingPower = -1f;
        var generator = new GameObject("CSV fixture generator");
        generator.transform.SetParent(_fixture.transform, false);
        _generator = generator.AddComponent<PowerGeneratingUnit>();
        _generator.outputPower = 0f;
        var relay = new GameObject("CSV fixture relay");
        relay.transform.SetParent(_fixture.transform, false);
        relay.AddComponent<PowerTransmissionDevice>().powerRange = 10;
        _play.playMode = true;
        _fixture.SetActive(true);
        _phase = 0;
        _phaseStarted = Time.time;
        EditorApplication.update += Tick;
        WriteReport();
        return "started";
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || _fixture == null)
            {
                throw new InvalidOperationException("Probe interrupted.");
            }

            if (_phase == 0)
            {
                Check(
                    _bot.repairAmount > 0f && _power.minWorkingPower == 50f,
                    "CSV Awake restores repair amount and required power before Start");
                _target.currentDurability = _target.maxDurability * 0.5f;
                CheckRuntimeState();
                _phase = 1;
                _phaseStarted = Time.time;
            }
            else if (_phase == 1 && Time.time - _phaseStarted >= 4f)
            {
                Check(
                    _bot.currentTarget == _target && _bot.transform.parent == _bot.home && !_power.isWorking,
                    "Damaged target selected but unpowered robot stays docked");
                _generator.outputPower = 1000f;
                _phase = 2;
                _phaseStarted = Time.time;
            }
            else if (_phase == 2 && _target.currentDurability > _target.maxDurability * 0.5f)
            {
                Check(
                    _bot.transform.parent != _bot.home && _power.isWorking,
                    "Real generator and relay supply power, robot leaves bay and repairs target");
                _generator.outputPower = 0f;
                _phase = 3;
                _phaseStarted = Time.time;
            }
            else if (_phase == 3 && Time.time - _phaseStarted >= 0.5f)
            {
                Check(!_power.isWorking && !_bot.isRepairing, "Loss of supply pauses repair without losing Home");
                _target.currentDurability = _target.maxDurability * 0.25f;
                JsonUtility.FromJsonOverwrite("{\"_requiresPower\":false}", _bot);
                _phase = 4;
                _phaseStarted = Time.time;
            }
            else if (_phase == 4 && _target.currentDurability > _target.maxDurability * 0.25f)
            {
                Check(!_power.IsWorkRequested, "requiresPower=false allows repair without requesting power");
                _report.status = "passed";
                Finish();
            }

            if (_fixture != null && Time.time - _phaseStarted > 12f)
            {
                throw new InvalidOperationException("Timed out in repair phase " + _phase);
            }
        }
        catch (Exception exception)
        {
            _report.status = "failed";
            _report.error = exception.ToString();
            Finish();
        }
    }

    private static void CheckRuntimeState()
    {
        var block = _target.GetComponent<Block>();
        var connector = block.connectors[0];
        connector.isConnected = true;
        block.resourcePath = "saved-instance-resource-path";
        float health = _target.currentDurability;
        block.raycastCollider.isTrigger = true;
        block.GetComponent<BlockCsvBinding>().Apply();
        Check(
            ReferenceEquals(connector, block.connectors[0]) && connector.isConnected,
            "Repeat runtime CSV application preserves connector identity and connection state");
        Check(
            block.resourcePath == "saved-instance-resource-path" && block.raycastCollider.isTrigger && _target.currentDurability == health,
            "Runtime CSV application preserves saved path, trigger semantics and current health");
        block.raycastCollider.isTrigger = false;
        var document = BlockCsvDocument.Parse(Resources.Load<TextAsset>(BlockCsvDocument.ResourcePath).text);
        var root = _bot.home.GetComponentInParent<Block>().gameObject;
        Transform home = _bot.home;
        Transform outside = _bot.outside;
        document.Apply(root, root.GetComponent<BlockCsvBinding>().PrefabGuid);
        Check(
            _bot.home == home && _bot.outside == outside,
            "Runtime nested robot application preserves Home and Outside references");
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }

        _report.checks.Add(description);
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        if (_play != null)
        {
            _play.playMode = _originalPlayMode;
        }

        if (_fixture != null)
        {
            _fixture.SetActive(false);
            Object.Destroy(_fixture);
            _fixture = null;
        }

        WriteReport();
    }

    private static void WriteReport()
    {
        Directory.CreateDirectory("Temp/BlockCsvChecks");
        File.WriteAllText(
            "Temp/BlockCsvChecks/regression-play.json",
            JsonUtility.ToJson(_report, true),
            new UTF8Encoding(false));
    }
}

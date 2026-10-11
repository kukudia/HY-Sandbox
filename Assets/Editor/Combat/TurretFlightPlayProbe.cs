// 职责：验证真实引擎帧中的无目标空闲、自动申请供电、双管发射及有限飞行时间。
// 入口：Run()；在已进入的Play Mode隔离执行，输出JSON与飞行截图，不保存场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TurretFlightPlayProbe
{
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    private static GameObject _root;
    private static ProjectileTurret _turret;
    private static Durability _health;
    private static PlayManager _manager;
    private static List<ControlUnit> _previousUnits;
    private static bool _previousMode;
    private static bool _previousEnabled;
    private static bool _previousBackground;
    private static float _start;
    private static float _launchTime;
    private static Vector3 _launchPosition;
    private static int _phase;
    private static bool _sawMovement;
    private static bool _captured;
    private static Camera _camera;
    private static Report _report;
    public static void Run()
    {
        if (!EditorApplication.isPlaying || _root != null)
        {
            throw new InvalidOperationException("Requires Play Mode and no pending flight probe.");
        }

        _manager = Object.FindFirstObjectByType<PlayManager>(FindObjectsInactive.Include);
        PlayManager.instance = _manager;
        _previousUnits = _manager.allControlUnits;
        _previousMode = _manager.playMode;
        _previousEnabled = _manager.enabled;
        _previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        _manager.enabled = false;
        _manager.playMode = true;
        _manager.allControlUnits = new List<ControlUnit>();
        _report = new Report();
        _phase = 0;
        _sawMovement = false;
        _captured = false;
        _root = new GameObject("Turret actual flight probe");
        _root.transform.position = new Vector3(4000f, 4000f, 4000f);
        ControlUnit owner = Unit("Owner", Vector3.zero, UnitFaction.Player);
        _turret = Object.Instantiate(Resources.Load<GameObject>("Blocks/ProjectileTurretDouble"), owner.transform).GetComponent<ProjectileTurret>();
        _turret.fireInterval = 3f;
        GameObject relayObject = new GameObject("Relay");
        relayObject.transform.SetParent(owner.transform, false);
        relayObject.AddComponent<PowerTransmissionDevice>().powerRange = 20;
        relayObject.AddComponent<PowerGeneratingUnit>().outputPower = 1000f;
        GameObject cameraObject = new GameObject("Flight camera");
        cameraObject.transform.SetParent(_root.transform, false);
        _camera = cameraObject.AddComponent<Camera>();
        _camera.enabled = false;
        _camera.transform.localPosition = new Vector3(7f, 5f, 6f);
        _camera.transform.LookAt(_root.transform.position + Vector3.forward * 3f);
        _camera.nearClipPlane = 0.01f;
        _camera.farClipPlane = 30f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.065f, 0.08f, 0.11f);
        GameObject light = new GameObject("Flight light");
        light.transform.SetParent(_root.transform, false);
        light.AddComponent<Light>().type = LightType.Directional;
        light.GetComponent<Light>().intensity = 3f;
        light.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
        _start = Time.time;
        EditorApplication.update += Tick;
    }

    private static ControlUnit Unit(string name, Vector3 position, UnitFaction faction)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(_root.transform, false);
        obj.transform.localPosition = position;
        Rigidbody body = obj.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;
        ControlUnit unit = obj.AddComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = faction;
        GameObject cockpit = new GameObject("Cockpit");
        cockpit.transform.SetParent(obj.transform, false);
        unit.cockpit = cockpit.AddComponent<Cockpit>();
        unit.cockpit.enabled = false;
        unit.hasValidCockpit = true;
        _manager.allControlUnits.Add(unit);
        return unit;
    }

    private static void Tick()
    {
        try
        {
            if (!EditorApplication.isPlaying || Time.time - _start > 6f)
            {
                _report.errors.Add("Flight probe timed out or Play Mode stopped.");
                Finish();
                return;
            }

            if (_phase == 0 && Time.time - _start > 0.2f)
            {
                Check(
                    "Automatic turret without target requests and consumes zero",
                    !_turret.GetComponent<Power>().IsWorkRequested && _turret.GetComponent<Power>().currentPower == 0f);
                ControlUnit enemy = Unit("Enemy", Vector3.forward * 10f, UnitFaction.Enemy);
                GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                target.transform.SetParent(enemy.transform, false);
                target.transform.localScale = new Vector3(2f, 2f, 0.3f);
                _health = target.AddComponent<Durability>();
                _health.maxDurability = _health.currentDurability = 1000f;
                _health.debugLog = false;
                _phase = 1;
            }

            TurretProjectile[] shots = Object.FindObjectsByType<TurretProjectile>(FindObjectsSortMode.None);
            if (_phase == 1 && shots.Length > 0)
            {
                Check("Natural FixedUpdate launches exactly two physical shells", shots.Length == 2);
                Check("Target remains undamaged at launch", _health.currentDurability == 1000f);
                Check(
                    "Automatic target acquisition starts 150 rated power",
                    Mathf.Abs(_turret.GetComponent<Power>().currentPower - 150f) < 0.01f);
                _launchTime = Time.time;
                _launchPosition = shots[0].transform.position;
                _phase = 2;
            }

            if (_phase == 2 && shots.Length > 0 && Time.time - _launchTime > 0.04f)
            {
                _sawMovement |= Vector3.Distance(shots[0].transform.position, _launchPosition) > 0.1f;
                if (!_captured)
                {
                    Capture();
                    _captured = true;
                }
            }

            if (_phase == 2 && _health.currentDurability < 1000f)
            {
                Check("PhysX advances visible shells before damage", _sawMovement);
                Check("Shell damage arrives after finite flight", Time.time - _launchTime > 0.1f);
                Check("Both barrels hit in first volley", Mathf.Abs(_health.currentDurability - 976f) < 0.01f);
                _turret.SetStarted(false);
                Check(
                    "Stopping automatic turret immediately releases demand and supply",
                    !_turret.GetComponent<Power>().IsWorkRequested && _turret.GetComponent<Power>().currentPower == 0f);
                Finish();
            }
        }
        catch (Exception exception)
        {
            _report.errors.Add(exception.ToString());
            Finish();
        }
    }

    private static void Check(string label, bool passed)
    {
        (passed ? _report.checks : _report.errors).Add(label);
    }

    private static void Capture()
    {
        RenderTexture target = new RenderTexture(960, 600, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D texture = new Texture2D(960, 600, TextureFormat.RGB24, false);
        try
        {
            _camera.targetTexture = target;
            _camera.Render();
            RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, 960, 600), 0, 0);
            texture.Apply();
            Directory.CreateDirectory("Temp/TurretRuntime");
            File.WriteAllBytes("Temp/TurretRuntime/Flight.png", texture.EncodeToPNG());
        }
        finally
        {
            _camera.targetTexture = null;
            RenderTexture.active = previous;
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target);
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        _manager.playMode = _previousMode;
        _manager.allControlUnits = _previousUnits;
        _manager.enabled = _previousEnabled;
        Application.runInBackground = _previousBackground;
        foreach (TurretProjectile shot in Object.FindObjectsByType<TurretProjectile>(FindObjectsSortMode.None))
        {
            if (Vector3.Distance(shot.transform.position, _root.transform.position) < 50f)
            {
                Object.DestroyImmediate(shot.gameObject);
            }
        }

        Object.DestroyImmediate(_root);
        _root = null;
        Directory.CreateDirectory("Temp/TurretRuntime");
        File.WriteAllText("Temp/TurretRuntime/FlightValidation.json", JsonUtility.ToJson(_report, true));
    }
}

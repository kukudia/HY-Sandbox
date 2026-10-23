// 职责：显式Player验证通过虚拟Input System设备走键盘、HUD点击与真实飞行物理链。
// 入口：Run()；只由-siege-checks-driving调用，finally恢复设备与后台输入设置。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

internal static class SiegeDrivingPlayerChecks
{
    internal static IEnumerator Run(SiegeSession session, Action<string, bool> check, Report report)
    {
        InputSettings.BackgroundBehavior background = InputSystem.settings.backgroundBehavior;
        Keyboard previousKeyboard = Keyboard.current;
        Mouse previousMouse = Mouse.current;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.EnableDevice(keyboard);
            InputSystem.EnableDevice(mouse);
            keyboard.MakeCurrent();
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            SiegeFleetEntry first = session.Fleet.Driven;
            ControlUnit unit = first.Unit;
            Thruster[] directional = DirectionalThrusters(unit);
            check(
                "driving input fixture has a configured manual ship",
                unit.AcceptsPlayerInput && directional.Length > 0 && unit.hoverFlightController != null);
            Vector3 start = unit.transform.position;
            float height = unit.hoverFlightController.targetHoverHeight;
            float end = Time.realtimeSinceStartup + 1.5f;
            Sample forward = new Sample
            {
                action = "W + Q",
                start = start
            };
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.Q));
            while (Time.realtimeSinceStartup < end)
            {
                keyboard.MakeCurrent();
                forward.frames++;
                Vector3 expected = Camera.main.transform.forward;
                expected.y = 0f;
                if (Vector3.Dot(unit.MovementInput, expected.normalized) > 0.9f)
                {
                    forward.inputFrames++;
                }

                forward.maximumThrust = Mathf.Max(
                    forward.maximumThrust,
                    directional.Where(thruster => thruster != null).Sum(thruster => thruster.thrust));
                yield return null;
            }

            forward.end = unit.transform.position;
            forward.heightChange = unit.hoverFlightController.targetHoverHeight - height;
            report.samples.Add(forward);
            check(
                "W drives camera-relative input across real frames",
                forward.frames > 10 && forward.inputFrames > forward.frames * 0.6f);
            check(
                "W produces real thrust and horizontal displacement",
                forward.maximumThrust > 0f && HorizontalDistance(forward.start, forward.end) > 0.05f);
            check("Q changes driven hover target through actual fixed steps", forward.heightChange > 0.5f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            end = Time.realtimeSinceStartup + 0.2f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            check("released keyboard clears manual movement", unit.MovementInput.sqrMagnitude < 0.001f);
            bool previousHover = unit.hoverFlightController.setHeight;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            end = Time.realtimeSinceStartup + 0.15f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            check("space toggles driven hover exactly once", unit.hoverFlightController.setHeight != previousHover);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            end = Time.realtimeSinceStartup + 0.15f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            check("second space press restores hover", unit.hoverFlightController.setHeight == previousHover);
            Button switchButton = Object.FindFirstObjectByType<SiegeHud>().GetComponentsInChildren<Button>().Single(button => button.name == "Pilot Bay 1");
            var corners = new Vector3[4];
            switchButton.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 center = (corners[0] + corners[2]) * 0.5f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center, buttons = 1 });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = center });
            end = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < end)
            {
                yield return null;
            }

            check(
                "actual HUD pointer click transfers to Bay1",
                session.Fleet.Driven == session.Fleet.Ships[0] && session.Fleet.Driven != first);
            unit = session.Fleet.Driven.Unit;
            directional = DirectionalThrusters(unit);
            check(
                "HUD switch updates camera and unique input authority",
                CameraController.instance.playerBody == unit.transform && session.Fleet.Ships.Count(ship => ship != null && ship.IsAlive && ship.Unit.AcceptsPlayerInput) == 1);
            check(
                "previous pilot resumes autonomous controller",
                !first.Unit.AcceptsPlayerInput && first.Unit.GetComponent<EnemyController>().enabled);
            Sample right = new Sample
            {
                action = "D + E after HUD switch",
                start = unit.transform.position
            };
            height = unit.hoverFlightController.targetHoverHeight;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.E));
            end = Time.realtimeSinceStartup + 1.2f;
            while (Time.realtimeSinceStartup < end)
            {
                keyboard.MakeCurrent();
                right.frames++;
                Vector3 expected = Camera.main.transform.right;
                expected.y = 0f;
                if (Vector3.Dot(unit.MovementInput, expected.normalized) > 0.9f)
                {
                    right.inputFrames++;
                }

                right.maximumThrust = Mathf.Max(
                    right.maximumThrust,
                    directional.Where(thruster => thruster != null).Sum(thruster => thruster.thrust));
                yield return null;
            }

            right.end = unit.transform.position;
            right.heightChange = unit.hoverFlightController.targetHoverHeight - height;
            report.samples.Add(right);
            check(
                "D routes to newly driven ship across real frames",
                right.frames > 10 && right.inputFrames > right.frames * 0.6f);
            check(
                "newly driven ship applies real thrust and moves",
                right.maximumThrust > 0f && HorizontalDistance(right.start, right.end) > 0.05f);
            check("E lowers newly driven hover target", right.heightChange < -0.5f);
        }
        finally
        {
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            if (previousKeyboard != null && previousKeyboard.added)
            {
                previousKeyboard.MakeCurrent();
            }

            if (previousMouse != null && previousMouse.added)
            {
                previousMouse.MakeCurrent();
            }

            report.devicesRestored = !keyboard.added && !mouse.added && InputSystem.settings.backgroundBehavior == background;
        }
    }

    private static Thruster[] DirectionalThrusters(ControlUnit unit)
    {
        return unit.GetComponentsInChildren<Thruster>().Where(thruster => thruster is MainThruster || thruster is UniversalThruster).ToArray();
    }

    private static float HorizontalDistance(Vector3 from, Vector3 to)
    {
        Vector3 difference = to - from;
        difference.y = 0f;
        return difference.magnitude;
    }

    [Serializable]
    internal sealed class Report
    {
        public bool devicesRestored;
        public string methodology = "Explicit virtual Input System keyboard/mouse in standalone Player, actual Update/FixedUpdate and HUD pointer routing. Enemy battle continues naturally. Controlled input probe, not human playtesting or performance comparison. Devices and background setting restored in finally.";
        public List<Sample> samples = new List<Sample>();
    }

    [Serializable]
    internal sealed class Sample
    {
        public string action;
        public int frames;
        public int inputFrames;
        public float maximumThrust;
        public float heightChange;
        public Vector3 start;
        public Vector3 end;
    }
}

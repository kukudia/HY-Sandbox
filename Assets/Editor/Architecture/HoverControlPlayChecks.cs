// 职责：以真实运行组件验证悬浮控制的有效归属、停供和禁用清理契约。
// 入口：Run()；由 ArchitecturePlayProbe 在暂停场景业务的 Play 会话中调用。
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

internal static class HoverControlPlayChecks
{
    internal static string[] Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        var checks = new List<string>();
        GameObject root = new GameObject("Architecture hover probe");
        GameObject other = new GameObject("Architecture replacement controller");
        bool previousMode = PlayManager.instance.playMode;
        try
        {
            Rigidbody body = root.AddComponent<Rigidbody>();
            body.useGravity = false;
            root.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            body.rotation = root.transform.rotation;
            ControlUnit owner = root.AddComponent<ControlUnit>();
            owner.enabled = false;
            owner.faction = UnitFaction.Enemy;
            GameObject controlObject = new GameObject("Controller");
            controlObject.transform.SetParent(root.transform, false);
            HoverFlightController controller = controlObject.AddComponent<HoverFlightController>();
            controller.GetComponent<Power>().currentPower = 1000f;
            GameObject thrusterObject = new GameObject("Thruster");
            thrusterObject.transform.SetParent(root.transform, false);
            HoverThruster thruster = thrusterObject.AddComponent<HoverThruster>();
            thruster.GetComponent<Power>().currentPower = 1000f;
            thruster.isHovered = true;
            thruster.SetRuntimeReferences(owner, body);
            controller.thrusters = new[]
            {
                thruster
            };
            owner.hoverFlightController = controller;
            controller.Init();
            PlayManager.instance.playMode = true;
            Vector3 originalVelocity = new Vector3(0.5f, 0.8f, 1.2f);
            body.angularVelocity = originalVelocity;
            thruster.thrust = 30f;
            thruster.lastThrustValue = 20f;
            Tick(controller);
            Check(
                checks,
                "Powered cockpit-less fragment never writes angular velocity and clears thrust",
                body.angularVelocity == originalVelocity && thruster.thrust == 0f && thruster.lastThrustValue == 0f);
            GameObject cockpitObject = new GameObject("Cockpit");
            cockpitObject.transform.SetParent(root.transform, false);
            owner.cockpit = cockpitObject.AddComponent<Cockpit>();
            owner.cockpit.enabled = false;
            owner.hasValidCockpit = true;
            owner.cockpits = new[]
            {
                owner.cockpit
            };
            controller.GetComponent<Power>().SetWorkRequested(controller, true);
            controller.GetComponent<Power>().ReceivePower(1000f);
            thruster.GetComponent<Power>().SetWorkRequested(thruster, true);
            thruster.GetComponent<Power>().ReceivePower(1000f);
            Tick(controller);
            Check(
                checks,
                $"Valid powered owner produces thrust and angular correction (thrust={thruster.thrust}, angular={body.angularVelocity}, up={controller.transform.up}, valid={owner.HasValidCockpit})",
                thruster.thrust > 0f && body.angularVelocity != originalVelocity);
            controller.GetComponent<Power>().currentPower = 0f;
            body.angularVelocity = originalVelocity;
            Tick(controller);
            Check(
                checks,
                "Controller power loss clears command history without steering",
                thruster.thrust == 0f && thruster.lastThrustValue == 0f && body.angularVelocity == originalVelocity);
            controller.GetComponent<Power>().currentPower = 1000f;
            thruster.thrust = 30f;
            thruster.lastThrustValue = 20f;
            PlayManager.instance.playMode = false;
            Tick(controller);
            Check(
                checks,
                "Leaving play mode clears hover output",
                thruster.thrust == 0f && thruster.lastThrustValue == 0f);
            PlayManager.instance.playMode = true;
            thruster.thrust = 30f;
            thruster.lastThrustValue = 20f;
            controller.enabled = false;
            Check(
                checks,
                "Disabling controller clears hover output",
                thruster.thrust == 0f && thruster.lastThrustValue == 0f);
            ControlUnit replacementOwner = other.AddComponent<ControlUnit>();
            replacementOwner.enabled = false;
            replacementOwner.hoverFlightController = other.AddComponent<HoverFlightController>();
            replacementOwner.hoverFlightController.enabled = false;
            thruster.controlUnit = replacementOwner;
            thruster.thrust = 17f;
            thruster.lastThrustValue = 21f;
            controller.enabled = true;
            controller.enabled = false;
            Check(
                checks,
                "Old controller cleanup preserves transferred thruster output",
                thruster.thrust == 17f && thruster.lastThrustValue == 21f);
            return checks.ToArray();
        }
        finally
        {
            PlayManager.instance.playMode = previousMode;
            Object.Destroy(root);
            Object.Destroy(other);
        }
    }

    private static void Tick(HoverFlightController controller)
    {
        typeof(HoverFlightController).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}

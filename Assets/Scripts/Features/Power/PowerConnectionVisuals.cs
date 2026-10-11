// 职责：复用 LineRenderer 绘制中继与发电机之间的虚线连接。
// 入口：Refresh() / Hide()；由所属 PowerTransmissionDevice 的生命周期调用。
// 边界：只读取电网结果，不计算供电；线对象由所属 GameObject 持有并随其销毁。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed class PowerConnectionVisuals
{
    private static Material _dashedLineMaterial;
    private readonly PowerTransmissionDevice _device;
    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
    public PowerConnectionVisuals(PowerTransmissionDevice device)
    {
        _device = device;
    }

    public void Refresh(DebugManager settings)
    {
        if (settings == null || !settings.showPowerConnections)
        {
            Hide();
            return;
        }

        int lineIndex = 0;
        foreach (PowerGeneratingUnit generator in _device.connectedGenerators)
        {
            if (generator != null)
            {
                DrawConnection(lineIndex++, generator.transform.position, settings.generatorConnectionColor, settings);
            }
        }

        foreach (PowerTransmissionDevice other in _device.connectedDevices)
        {
            // 无向边由实例编号较小的一端绘制，避免重复线与重复对象。
            if (other != null && _device.GetInstanceID() < other.GetInstanceID())
            {
                DrawConnection(lineIndex++, other.transform.position, settings.deviceConnectionColor, settings);
            }
        }

        HideUnusedLines(lineIndex);
    }

    public void Hide()
    {
        HideUnusedLines(0);
    }

    private void HideUnusedLines(int firstUnusedIndex)
    {
        for (int i = firstUnusedIndex; i < _lines.Count; i++)
        {
            if (_lines[i] != null)
            {
                _lines[i].enabled = false;
            }
        }
    }

    private void DrawConnection(int index, Vector3 end, Color color, DebugManager settings)
    {
        LineRenderer line = GetOrCreateLine(index);
        if (line == null)
        {
            return;
        }

        Vector3 start = _device.transform.position;
        float distance = Vector3.Distance(start, end);
        line.enabled = true;
        line.startWidth = settings.connectionLineWidth;
        line.endWidth = settings.connectionLineWidth;
        line.startColor = color;
        line.endColor = color;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.GetPropertyBlock(_properties);
        _properties.SetVector(
            "_MainTex_ST",
            new Vector4(
                Mathf.Max(1f, distance / Mathf.Max(0.02f, settings.connectionDashLength * 2f)),
                1f,
                -Time.unscaledTime * settings.connectionDashScrollSpeed,
                0f));
        line.SetPropertyBlock(_properties);
    }

    private LineRenderer GetOrCreateLine(int index)
    {
        while (_lines.Count <= index)
        {
            LineRenderer line;
            if (_lines.Count == 0 && _device.lineRenderer != null)
            {
                line = _device.lineRenderer;
            }
            else
            {
                GameObject lineObject = new GameObject($"Power Connection {_lines.Count + 1}");
                lineObject.transform.SetParent(_device.transform, false);
                line = lineObject.AddComponent<LineRenderer>();
            }

            ConfigureLine(line);
            _lines.Add(line);
        }

        return _lines[index];
    }

    private static void ConfigureLine(LineRenderer line)
    {
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sharedMaterial = GetDashedLineMaterial();
    }

    private static Material GetDashedLineMaterial()
    {
        if (_dashedLineMaterial != null)
        {
            return _dashedLineMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        _dashedLineMaterial = new Material(shader)
        {
            name = "Runtime Power Connection Dashes",
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = CreateDashTexture()
        };
        return _dashedLineMaterial;
    }

    private static Texture2D CreateDashTexture()
    {
        Texture2D texture = new Texture2D(8, 1, TextureFormat.RGBA32, false)
        {
            name = "Runtime Power Dash Pattern",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int x = 0; x < texture.width; x++)
        {
            texture.SetPixel(x, 0, x < texture.width / 2 ? Color.white : Color.clear);
        }

        texture.Apply(false, true);
        return texture;
    }
}

// 职责：按源材质和脉冲配置共享发光材质；保留外部PropertyBlock及选择高亮的所有权。
// 入口：IndustrialPartMotion创建/释放绑定，Tick()每帧更新唯一材质一次。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal sealed class IndustrialEmissionBinding : IDisposable
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly Dictionary<PulseKey, Pulse> Pulses = new Dictionary<PulseKey, Pulse>();
    private static readonly Dictionary<Renderer, IndustrialEmissionBinding> Bindings = new Dictionary<Renderer, IndustrialEmissionBinding>();
    private static readonly Dictionary<Renderer, Material[]> Selections = new Dictionary<Renderer, Material[]>();
    private readonly Renderer renderer;
    private readonly Material[] original;
    private readonly Material[] animated;
    private readonly Pulse[] leases;
    private MaterialPropertyBlock properties;
    private bool disposed;
    internal IndustrialEmissionBinding(Renderer target, Color color, float frequency, float amplitude)
    {
        renderer = target;
        if (target == null)
        {
            return;
        }

        // 已有实例属性由其原系统拥有；继续读改写，不能清除选择、仓储状态灯等覆盖。
        if (!UsesGpuDrawing() || target.HasPropertyBlock() || Bindings.ContainsKey(target))
        {
            properties = new MaterialPropertyBlock();
            return;
        }

        original = Selections.TryGetValue(target, out Material[] saved) ? saved : target.sharedMaterials;
        animated = (Material[])original.Clone();
        leases = new Pulse[original.Length];
        for (int i = 0; i < original.Length; i++)
        {
            Material source = original[i];
            if (source == null || !source.HasProperty(EmissionColorId))
            {
                continue;
            }

            var key = new PulseKey(source, color, frequency, amplitude);
            if (!Pulses.TryGetValue(key, out Pulse pulse))
            {
                pulse = new Pulse(key, source);
                Pulses.Add(key, pulse);
            }

            pulse.References++;
            leases[i] = pulse;
            animated[i] = pulse.Material;
        }

        if (!Selections.ContainsKey(target))
        {
            target.sharedMaterials = animated;
        }

        Bindings.Add(target, this);
    }

    private static bool UsesGpuDrawing()
    {
        // 传统SRP的自动实例化优先保留PropertyBlock；去掉它反而可能使同Mesh转入更慢的SRP批处理。
        return GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset && asset.gpuResidentDrawerMode == GPUResidentDrawerMode.InstancedDrawing && BatchRendererGroup.BufferTarget == BatchBufferTarget.RawBuffer && SystemInfo.supportsComputeShaders;
    }

    internal void Apply(Color emission)
    {
        if (renderer == null)
        {
            return;
        }

        if (properties == null)
        {
            if (!renderer.HasPropertyBlock())
            {
                return;
            }

            properties = new MaterialPropertyBlock();
        }

        renderer.GetPropertyBlock(properties);
        properties.SetColor(EmissionColorId, emission);
        renderer.SetPropertyBlock(properties);
    }

    internal static void Tick(float unscaledTime)
    {
        foreach (Pulse pulse in Pulses.Values)
        {
            pulse.Update(unscaledTime);
        }
    }

    // 选择系统必须保存持久源材质，不能持有可能在禁用/重配时释放的临时材质。
    internal static Material[] CaptureSelection(Renderer target)
    {
        Material[] current = target.sharedMaterials;
        if (!Bindings.TryGetValue(target, out IndustrialEmissionBinding binding))
        {
            return current;
        }

        Material[] saved = Matches(current, binding.animated) ? (Material[])binding.original.Clone() : current;
        Selections[target] = saved;
        return saved;
    }

    internal static void RestoreSelection(Renderer target, Material[] saved)
    {
        Selections.Remove(target);
        target.sharedMaterials = Bindings.TryGetValue(target, out IndustrialEmissionBinding binding) && Matches(saved, binding.original) ? binding.animated : saved;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        if (original == null)
        {
            return;
        }

        if (renderer != null && Matches(renderer.sharedMaterials, animated))
        {
            renderer.sharedMaterials = original;
        }

        Bindings.Remove(renderer);
        if (renderer == null)
        {
            Selections.Remove(renderer);
        }

        foreach (Pulse pulse in leases)
        {
            if (pulse == null || --pulse.References > 0)
            {
                continue;
            }

            Pulses.Remove(pulse.Key);
            UnityEngine.Object.Destroy(pulse.Material);
        }
    }

    private static bool Matches(Material[] left, Material[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (int i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

    private readonly struct PulseKey : IEquatable<PulseKey>
    {
        private readonly EntityId sourceId;
        internal readonly Color Color;
        internal readonly float Frequency;
        internal readonly float Amplitude;
        internal PulseKey(Material source, Color color, float frequency, float amplitude)
        {
            sourceId = source.GetEntityId();
            Color = color;
            Frequency = frequency;
            Amplitude = amplitude;
        }

        public bool Equals(PulseKey other)
        {
            return sourceId == other.sourceId && Color.Equals(other.Color) && Frequency.Equals(other.Frequency) && Amplitude.Equals(other.Amplitude);
        }

        public override bool Equals(object other)
        {
            return other is PulseKey key && Equals(key);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(sourceId, Color, Frequency, Amplitude);
        }
    }

    private sealed class Pulse
    {
        internal readonly PulseKey Key;
        internal readonly Material Material;
        internal int References;
        internal Pulse(PulseKey key, Material source)
        {
            Key = key;
            Material = new Material(source)
            {
                name = source.name + " (shared industrial pulse)",
                hideFlags = HideFlags.HideAndDontSave
            };
            Update(Time.unscaledTime);
        }

        internal void Update(float unscaledTime)
        {
            float wave = Mathf.Sin(unscaledTime * Key.Frequency * Mathf.PI * 2f);
            Material.SetColor(EmissionColorId, Key.Color * Mathf.Max(0f, 1f + wave * Key.Amplitude));
        }
    }
}

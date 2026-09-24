using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Unity 6.3's graph-model API is internal. Keep the version-specific Editor bridge here.</summary>
public static class VfxGraphAuthoring
{
    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    public static Type Type(string name) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name)).First(t => t != null);
    private static PropertyInfo Property(object target, string name)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var property = type.GetProperty(name, Flags | BindingFlags.DeclaredOnly);
            if (property != null) return property;
        }
        throw new MissingMemberException(target.GetType().Name, name);
    }
    public static object Get(object target, string name) => Property(target, name).GetValue(target);
    public static void Set(object target, string name, object value)
    {
        // Space-aware slots store Position/Vector structs, not raw Vector3. Reflection
        // does not invoke their implicit conversion and VFX otherwise silently resets them.
        if (name == "value" && value is Vector3 vector)
        {
            object current = Get(target, name);
            if (current != null && current.GetType() != typeof(Vector3))
            {
                var field = current.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(f => f.FieldType == typeof(Vector3));
                if (field != null) { field.SetValue(current, vector); value = current; }
            }
        }
        Property(target, name).SetValue(target, value);
    }
    public static object Call(object target, string method, params object[] args)
    {
        var type = target as Type ?? target.GetType();
        var candidates = type.GetMethods(Flags).Where(m => !m.ContainsGenericParameters && m.Name == method && m.GetParameters().Length == args.Length);
        var match = candidates.First(m => m.GetParameters().Select((p, i) => args[i] == null || p.ParameterType.IsInstanceOfType(args[i])).All(x => x));
        return match.Invoke(target is Type ? null : target, args);
    }
    public static object[] Items(object target, string property) => ((IEnumerable)Get(target, property)).Cast<object>().ToArray();
    public static object Resource(string path) => Call(Type("UnityEditor.VFX.VisualEffectResource"), "GetResourceAtPath", path);
    public static object Graph(string path) => Call(Type("UnityEditor.VFX.VisualEffectResourceExtensions"), "GetOrCreateGraph", Resource(path));
    public static Object[] Contents(string path) => (Object[])Call(Resource(path), "GetContents");
    public static void Save(string path)
    {
        object resource = Resource(path);
        Set(resource, "cullingFlags", Enum.Parse(Get(resource, "cullingFlags").GetType(), "CullNone"));
        Call(Type("UnityEditor.VFX.VisualEffectResourceExtensions"), "WriteAssetWithSubAssets", Resource(path));
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
    public static object Create(string name) => ScriptableObject.CreateInstance(Type("UnityEditor.VFX." + name));
    public static void Add(object parent, object child) => Call(parent, "AddChild", child, -1, true);
    public static void Setting(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (var t = target.GetType(); t != null && field == null; t = t.BaseType) field = t.GetField(fieldName, Flags);
        if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
        if (field.FieldType.IsEnum && value is string text) value = Enum.Parse(field.FieldType, text);
        Call(target, "SetSettingValue", fieldName, value);
    }
    public static object Slot(object target, string name) => Items(target, "inputSlots").First(s => (string)Get(s, "name") == name);
    public static void Value(object target, string name, object value) => Set(Slot(target, name), "value", value);
    public static object Parameter(object graph, string name, object value)
    {
        object parameter = Items(graph, "children").FirstOrDefault(c => c.GetType().Name == "VFXParameter" && (string)Get(c, "exposedName") == name);
        if (parameter != null) return parameter;
        parameter = Create("VFXParameter");
        Call(parameter, "Init", value.GetType());
        Setting(parameter, "m_ExposedName", name); Setting(parameter, "m_Exposed", true);
        Set(parameter, "value", value); Add(graph, parameter);
        return parameter;
    }
    public static void Link(object parameter, object input) => Call(Items(parameter, "outputSlots")[0], "Link", input, true);
    public static object Attribute(object context, string name, object value, bool multiply = false)
    {
        object block = Create("Block.SetAttribute");
        Setting(block, "attribute", name);
        if (multiply) Setting(block, "Composition", "Multiply");
        Add(context, block);
        Set(Items(block, "inputSlots")[0], "value", value);
        return block;
    }
    public static void AddIntensity(string path)
    {
        object graph = Graph(path);
        if (Items(graph, "children").Any(c => c.GetType().Name == "VFXParameter" && (string)Get(c, "exposedName") == "Intensity")) return;
        object parameter = Parameter(graph, "Intensity", 1f);
        foreach (object context in Items(graph, "children").Where(c => c.GetType().GetProperty("contextType", Flags) != null && Get(c, "contextType").ToString() == "Output"))
        {
            object block = Attribute(context, "alpha", 1f, true);
            Link(parameter, Items(block, "inputSlots")[0]);
        }
        Save(path);
    }
    public static object Describe(string path)
    {
        object graph = Graph(path);
        return Items(graph, "children").Select(c => new
        {
            type = c.GetType().Name,
            slots = c.GetType().GetProperty("inputSlots", Flags) == null ? null : Items(c, "inputSlots").Select(s => new { name = Get(s, "name"), value = Get(s, "value") }).ToArray(),
            children = Items(c, "children").Select(b => new { type = b.GetType().Name, name = Get(b, "name"), slots = Items(b, "inputSlots").Select(s => new { name = Get(s, "name"), value = Get(s, "value") }).ToArray() }).ToArray()
        }).ToArray();
    }
}

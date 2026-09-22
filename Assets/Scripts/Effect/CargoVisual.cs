using UnityEngine;

public static class CargoVisual
{
    // Copy only static render geometry: stored items must not instantiate gameplay or nested drones.
    public static GameObject Create(string resourcePath, Transform parent, float size)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;
        GameObject prefab = Resources.Load<GameObject>(BuildManager.ConvertToResourcesPath(resourcePath));
        if (prefab == null) return null;
        var root = new GameObject("Cargo display: " + prefab.name);
        root.transform.SetParent(parent, false);
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        foreach (MeshFilter filter in prefab.GetComponentsInChildren<MeshFilter>(true))
        {
            MeshRenderer source = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null || source == null || !source.enabled || filter.GetComponentInParent<Bot>() != null) continue;
            var model = new GameObject(filter.name, typeof(MeshFilter), typeof(MeshRenderer));
            model.transform.SetParent(root.transform, false);
            Matrix4x4 local = prefab.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            model.transform.localPosition = local.GetColumn(3);
            model.transform.localRotation = local.rotation;
            model.transform.localScale = local.lossyScale;
            model.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var renderer = model.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = source.sharedMaterials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Bounds meshBounds = filter.sharedMesh.bounds;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                Vector3 point = local.MultiplyPoint3x4(meshBounds.center + Vector3.Scale(meshBounds.extents, sign));
                if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; } else bounds.Encapsulate(point);
            }
        }
        if (hasBounds)
        {
            Vector3 center = bounds.center;
            Vector3 localSize = bounds.size;
            float scale = size / Mathf.Max(0.01f, Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            foreach (Transform child in root.transform) child.localPosition -= center;
            root.transform.localScale = Vector3.one * scale;
        }
        return root;
    }
}

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the shared cockpit typography treatment to legacy UI.Text elements,
/// including elements instantiated by other systems after the scene loads.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class GlobalTextStyler : MonoBehaviour
{
    private static readonly Color OutlineColor = new Color(0.015f, 0.055f, 0.09f, 0.9f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        var host = new GameObject("Global Text Styler");
        DontDestroyOnLoad(host);
        host.AddComponent<GlobalTextStyler>();
    }

    private IEnumerator Start()
    {
        while (true)
        {
            ApplyToSceneTexts();
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }

    private static void ApplyToSceneTexts()
    {
        foreach (var text in Resources.FindObjectsOfTypeAll<Text>())
        {
            if (text == null || !text.gameObject.scene.IsValid())
            {
                continue;
            }

            var outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }

            outline.effectColor = OutlineColor;
            outline.effectDistance = new Vector2(1.25f, -1.25f);
            outline.useGraphicAlpha = true;
        }
    }
}

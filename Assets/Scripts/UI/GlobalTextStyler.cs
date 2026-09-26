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
    private static readonly Color ShadowColor = new Color(0.015f, 0.035f, 0.055f, 0.68f);

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
            if (outline != null)
            {
                outline.enabled = false;
            }

            // Outline derives from Shadow: GetComponent<Shadow>() can re-enable the very outline
            // disabled above, causing heavy text strokes and inconsistent typography.
            Shadow shadow = null;
            foreach (Shadow candidate in text.GetComponents<Shadow>())
                if (!(candidate is Outline)) { shadow = candidate; break; }
            if (shadow == null)
            {
                shadow = text.gameObject.AddComponent<Shadow>();
            }

            shadow.enabled = true;
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
        }
    }
}

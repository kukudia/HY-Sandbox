// 职责：在游玩HUD显示最近据点剩余驻军及本次探索清剿数量。
// 入口：Update()；引用由OutpostHudAuthoring保存，不在运行时生成界面。
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

public sealed class OutpostHud : MonoBehaviour
{
    [SerializeField]
    private Text _label;
    [SerializeField]
    private WorldSessionManager _session;
    private float nextUpdate;
    private void Update()
    {
        if (Time.unscaledTime < nextUpdate)
        {
            return;
        }

        nextUpdate = Time.unscaledTime + 0.2f;
        OutpostEncounterDirector director = _session != null ? _session.Outposts : null;
        _label.text = Format(director?.Nearest, director?.ClearedCount ?? 0);
    }

    public string Format(OutpostEncounterStatus? nearest, int clearedCount)
    {
        string summary = clearedCount > 0 ? "Outposts cleared this expedition: " + clearedCount : "";
        if (!nearest.HasValue)
        {
            return summary;
        }

        OutpostEncounterStatus status = nearest.Value;
        string distance = "  |  " + status.Distance.ToString("0", CultureInfo.InvariantCulture) + "m";
        string title = FitName(status.Name, distance) + distance;
        string detail = status.Cleared ? "CLEARED" : "Defenders " + status.Remaining + " / " + status.Total + "   |   Emplacements " + status.Emplacements;
        return title + "\n" + detail + (summary.Length > 0 ? "\n" + summary : "");
    }

    private string FitName(string name, string suffix)
    {
        name = (name ?? "Outpost").Replace('\n', ' ').Replace('\r', ' ');
        TextGenerationSettings settings = _label.GetGenerationSettings(Vector2.zero);
        TextGenerator generator = _label.cachedTextGeneratorForLayout;
        float width = Mathf.Max(0f, _label.rectTransform.rect.width - 8f) * _label.pixelsPerUnit;
        if (generator.GetPreferredWidth(name + suffix, settings) <= width)
        {
            return name;
        }

        // Fit actual glyph widths; grapheme boundaries preserve emoji and combined characters.
        int[] starts = StringInfo.ParseCombiningCharacters(name);
        int low = 0;
        int high = starts.Length;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            int end = middle < starts.Length ? starts[middle] : name.Length;
            if (generator.GetPreferredWidth(name.Substring(0, end) + "..." + suffix, settings) <= width)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        return name.Substring(0, low < starts.Length ? starts[low] : name.Length) + "...";
    }
}

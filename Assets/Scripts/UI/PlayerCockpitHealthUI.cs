using UnityEngine;
using UnityEngine.UI;

public class PlayerCockpitHealthUI : MonoBehaviour
{
    private const float BarWidth = 260f;
    private const float BarHeight = 18f;

    private Image fillImage;
    private Text valueText;
    private Durability cockpitDurability;

    private void Awake()
    {
        BuildHud();
    }

    private void Update()
    {
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            return;
        }

        RefreshCockpitReference();
        UpdateHealthDisplay();
    }

    private void BuildHud()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            return;
        }

        GameObject panelObject = CreateRectObject("CockpitHealthBar", root);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.anchoredPosition = new Vector2(28f, 28f);
        panelRect.sizeDelta = new Vector2(BarWidth + 20f, 58f);

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.type = Image.Type.Simple;
        panelImage.color = new Color(0.02f, 0.035f, 0.055f, 0.88f);
        panelImage.raycastTarget = false;

        Text label = CreateText("CockpitLabel", panelRect, "COCKPIT", 13, FontStyle.Bold);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.anchoredPosition = new Vector2(10f, -7f);
        labelRect.sizeDelta = new Vector2(-20f, 18f);
        label.alignment = TextAnchor.UpperLeft;
        label.color = new Color(0.68f, 0.85f, 0.96f, 1f);

        GameObject trackObject = CreateRectObject("CockpitHealthTrack", panelRect);
        RectTransform trackRect = trackObject.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0f);
        trackRect.anchorMax = new Vector2(0f, 0f);
        trackRect.pivot = new Vector2(0f, 0f);
        trackRect.anchoredPosition = new Vector2(10f, 10f);
        trackRect.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image trackImage = trackObject.AddComponent<Image>();
        trackImage.type = Image.Type.Simple;
        trackImage.color = new Color(0.08f, 0.1f, 0.13f, 1f);
        trackImage.raycastTarget = false;

        GameObject fillObject = CreateRectObject("Fill", trackRect);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fillObject.AddComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.fillAmount = 1f;
        fillImage.color = Color.green;
        fillImage.raycastTarget = false;

        valueText = CreateText("CockpitHealthValue", panelRect, "100 / 100", 12, FontStyle.Bold);
        RectTransform valueRect = valueText.rectTransform;
        valueRect.anchorMin = new Vector2(1f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.pivot = new Vector2(1f, 0f);
        valueRect.anchoredPosition = new Vector2(-10f, 10f);
        valueRect.sizeDelta = new Vector2(92f, BarHeight);
        valueText.alignment = TextAnchor.MiddleRight;
        valueText.color = Color.white;
    }

    private void RefreshCockpitReference()
    {
        if (cockpitDurability != null)
        {
            return;
        }

        Transform playerRoot = PlayManager.instance.blocksParent;
        if (playerRoot == null)
        {
            return;
        }

        Cockpit[] cockpits = playerRoot.GetComponentsInChildren<Cockpit>(true);
        foreach (Cockpit cockpit in cockpits)
        {
            if (cockpit != null && cockpit.faction == UnitFaction.Player)
            {
                cockpitDurability = cockpit.GetComponent<Durability>();
                break;
            }
        }
    }

    private void UpdateHealthDisplay()
    {
        if (fillImage == null || valueText == null)
        {
            return;
        }

        if (cockpitDurability == null || cockpitDurability.maxDurability <= 0f)
        {
            fillImage.fillAmount = 0f;
            valueText.text = "-- / --";
            return;
        }

        float ratio = Mathf.Clamp01(cockpitDurability.currentDurability / cockpitDurability.maxDurability);
        fillImage.fillAmount = ratio;
        fillImage.color = Color.Lerp(Color.red, Color.green, ratio);
        valueText.text = $"{cockpitDurability.currentDurability:0} / {cockpitDurability.maxDurability:0}";
    }

    private static GameObject CreateRectObject(string objectName, Transform parent)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child;
    }

    private static Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle style)
    {
        GameObject textObject = CreateRectObject(objectName, parent);
        Text textComponent = textObject.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = style;
        textComponent.raycastTarget = false;
        return textComponent;
    }
}

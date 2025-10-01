using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TooltipPanel : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;          // tooltip root (set inactive in inspector)
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text timeText;
    public TMP_Text spoonsText;
    public TMP_Text hungerText;
    public TMP_Text cashText;
    public Image iconImage;

    [Header("Settings")]
    public Vector2 offset = new Vector2(12f, -12f);

    RectTransform panelRect;

    void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);

            // Prevent raycast flicker (tooltip won't block hover detection)
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            panelRect = panel.GetComponent<RectTransform>();
        }
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 newPos = mousePos + offset;

        // Clamp tooltip to screen edges
        if (panelRect != null)
        {
            float width = panelRect.rect.width;
            float height = panelRect.rect.height;
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            // If too close to right edge, shift left
            if (newPos.x + width > screenWidth)
                newPos.x = mousePos.x - width - Mathf.Abs(offset.x);

            // If too close to bottom, shift up
            if (newPos.y - height < 0)
                newPos.y = height;
        }

        panel.transform.position = newPos;
    }

    public void Show(BehaviorData data)
    {
        if (panel == null || data == null) return;

        titleText.text = data.actionName;
        descText.text = data.description;

        // Time cost
        if (data.isToggle)
            timeText.text = "Time: Toggle (player controlled)";
        else if (data.durationMinutes > 0)
            timeText.text = FormatDuration(data.durationMinutes);
        else
            timeText.text = ""; // leave blank

        // Spoons
        if (data.spoonsCost < 0)
            spoonsText.text = "Spoons: ???";
        else if (data.spoonsCost > 0)
            spoonsText.text = $"Spoons: {data.spoonsCost}";
        else
            spoonsText.text = ""; // blank if zero

        // Hunger
        if (data.hungerImpact != 0)
            hungerText.text = FormatHunger(data.hungerImpact);
        else
            hungerText.text = "";

        // Cash
        if (data.cashCost > 0f)
            cashText.text = $"Cost: ${data.cashCost:0.00}";
        else
            cashText.text = ""; // blank if zero or negative

        if (iconImage != null) iconImage.sprite = data.icon;

        panel.SetActive(true);
    }


    public void Hide()
    {
        if (panel == null) return;
        panel.SetActive(false);
    }

    string FormatDuration(int minutes)
    {
        if (minutes <= 0) return "Time: <1m";
        int h = minutes / 60;
        int m = minutes % 60;
        if (h > 0) return $"Time: {h}h {m}m";
        return $"Time: {m}m";
    }

    string FormatHunger(int delta)
    {
        if (delta == 0) return "Hunger: None";
        if (delta > 0) return $"Hunger: +{delta}";
        return $"Hunger: {delta}"; // negative => reduces hunger
    }
}

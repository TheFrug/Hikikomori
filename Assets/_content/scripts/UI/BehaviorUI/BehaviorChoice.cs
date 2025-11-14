using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BehaviorChoice : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text timeText;
    public TMP_Text spoonsText;
    public TMP_Text hungerText;
    public TMP_Text cashText;
    public Image iconImage;

    [Header("Hook-ins")]
    public Button selectButton;
    public BehaviorManager behaviorManager;

    private BehaviorData data;

    public void Configure(BehaviorData behaviorData, BehaviorManager mgr)
    {
        data = behaviorData;
        behaviorManager = mgr;

        titleText.text = behaviorData.behaviorName;
        descText.text = behaviorData.behaviorDescription;

        if (behaviorData.isToggle)
            timeText.text = "Time: Toggle";
        else if (behaviorData.durationMinutes > 0)
            timeText.text = FormatDuration(behaviorData.durationMinutes);
        else
            timeText.text = "";

        spoonsText.text = behaviorData.hideSpoonsCost 
            ? "Spoons: ???" 
            : $"Spoons: {behaviorData.spoonsCost}";

        hungerText.text = behaviorData.hungerImpact != 0
            ? FormatHunger(behaviorData.hungerImpact)
            : "";

        cashText.text = behaviorData.cashCost > 0
            ? $"Cost: ${behaviorData.cashCost:0.00}"
            : "";

        if (iconImage != null)
            iconImage.sprite = behaviorData.icon;

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(OnSelected);
    }

    private void OnSelected()
    {
        if (behaviorManager == null || data == null)
        {
            Debug.LogError("BehaviorChoice missing BehaviorManager or BehaviorData.");
            return;
        }

        behaviorManager.QueueBehavior(data);

        // You can replace this later with an animation / UI fade-out
        Destroy(gameObject);
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
        return $"Hunger: {delta}";
    }
}

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
    [SerializeField] public GameObject spoonPanelPrefab; // assign via inspector; the panel prefab contains SpoonPanel component
    [SerializeField] public Transform panelAnchor; // where to spawn the panel (child container)

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

        // If Hiki is busy, let manager show tooltip
        if (behaviorManager.IsBusy())
        {
            behaviorManager.ShowBusyTooltip();
            return;
        }

        // Instantiate the spoon panel and configure it
        var panelGO = Instantiate(spoonPanelPrefab, panelAnchor != null ? panelAnchor : transform);
        var panel = panelGO.GetComponent<SpoonPanel>();
        panel.Setup(data, behaviorManager);

        // Optional: animate panel sliding out (tween or animator on panel prefab)
        // Do NOT Destroy this card here — panel will call back to the manager when done.
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

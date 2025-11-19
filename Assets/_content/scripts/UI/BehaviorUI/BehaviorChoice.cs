using System.Collections;
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

    [Header("One-shot UI (this lives on the choice)")]
    public Slider oneShotProgressBar;   // assign in inspector (on the choice card)
    public float defaultOneShotSeconds = 0.6f;

    private BehaviorData data;
    private SpoonPanel currentSpoonPanel;
    private Coroutine progressRoutine;

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

        // progress bar is hidden until Do-the-Thing triggers it
        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(false);
            oneShotProgressBar.value = 0f;
        }
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

        // If no spoons required -> start progress immediately on this choice (no SpoonPanel)
        if (data.spoonsCost <= 0)
        {
            StartProgressFromPanel(null);
            return;
        }

        // Otherwise spawn the SpoonPanel
        var panelGO = Instantiate(spoonPanelPrefab, panelAnchor != null ? panelAnchor : transform);
        var panel = panelGO.GetComponent<SpoonPanel>();
        if (panel == null)
        {
            Debug.LogError("SpoonPanel prefab missing SpoonPanel component.");
            Destroy(panelGO);
            return;
        }

        panel.Setup(data, behaviorManager, this);
        currentSpoonPanel = panel;
    }

    // Called by SpoonPanel when its Do-the-Thing button is pressed (or when there are 0 spoons required)
    // panel may be null (no panel if cost=0). Manager overloads accept panel so we forward panel too.
    public void StartProgressFromPanel(SpoonPanel panel)
    {
        // prevent double-start
        if (progressRoutine != null)
        {
            Debug.Log("[BehaviorChoice] Progress already running.");
            return;
        }

        bool isScene = data.isScene || (data.thought != null && data.thought.type == Thought.ThoughtType.Interactive);

        // decide time to fill progress bar
        float seconds = Mathf.Max(0.2f, defaultOneShotSeconds * (data.durationMinutes > 0 ? (data.durationMinutes / 30f) : 1f));

        // If we want scenes to also use the progress bar as a commitment/focus gate,
        // we still show it here and call manager when done (as you requested).
        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(true);
            progressRoutine = StartCoroutine(RunProgressThenStart(seconds, isScene, panel));
        }
        else
        {
            // No progress UI — trigger manager immediately
            if (isScene)
                behaviorManager.BeginSceneBehavior(data, panel);
            else
                behaviorManager.BeginOneShotBehavior(data, panel);
        }
    }

    private IEnumerator RunProgressThenStart(float seconds, bool isScene, SpoonPanel panel)
    {
        float elapsed = 0f;
        if (oneShotProgressBar != null) oneShotProgressBar.value = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            if (oneShotProgressBar != null)
                oneShotProgressBar.value = Mathf.Clamp01(elapsed / seconds);
            yield return null;
        }

        // done — notify manager and let it handle resources/thoughts + cleanup
        if (isScene)
            behaviorManager.BeginSceneBehavior(data, panel);
        else
            behaviorManager.BeginOneShotBehavior(data, panel);

        // cleanup local state: panel destruction will call back to NotifyPanelClosed via panel's Cancel/Destroy
        progressRoutine = null;

        // hide progress bar as a courtesy (manager / lifecycle may already destroy)
        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.value = 0f;
            oneShotProgressBar.gameObject.SetActive(false);
        }
    }

    // Called by SpoonPanel when it's cancelled/destroyed so the choice resets state
    public void NotifyPanelClosed()
    {
        // stop any running progress coroutines
        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(false);
            oneShotProgressBar.value = 0f;
        }

        currentSpoonPanel = null;
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

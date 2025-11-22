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
    [SerializeField] public GameObject spoonPanelPrefab;
    [SerializeField] public Transform panelAnchor;

    [Header("One-shot UI")]
    public Slider oneShotProgressBar;
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

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(false);
            oneShotProgressBar.value = 0f;
        }
    }

    private void OnSelected()
    {
        if (behaviorManager == null || data == null)
            return;

        if (behaviorManager.IsBusy())
        {
            behaviorManager.ShowBusyTooltip();
            return;
        }

        if (data.spoonsCost <= 0)
        {
            StartProgressFromPanel(null);
            return;
        }

        // If another choice has an open panel, close it
        if (behaviorManager.HasOpenPanel())
        {
            Destroy(behaviorManager.activeSpoonPanel.gameObject);
            behaviorManager.ClearPanel(behaviorManager.activeSpoonPanel);
        }

        // Spawn a new one and register it
        var panelGO = Instantiate(spoonPanelPrefab, panelAnchor != null ? panelAnchor : transform);
        var panel = panelGO.GetComponent<SpoonPanel>();

        if (panel == null)
        {
            Destroy(panelGO);
            return;
        }

        panel.Setup(data, behaviorManager, this);

        // NEW: BehaviorManager is in charge
        behaviorManager.RegisterPanel(panel);

        currentSpoonPanel = panel;
    }

    public void StartProgressFromPanel(SpoonPanel panel)
    {
        if (progressRoutine != null)
            return;

        bool isScene = data.isScene || (data.thought != null && data.thought.type == Thought.ThoughtType.Interactive);

        float seconds = Mathf.Max(
            0.2f,
            defaultOneShotSeconds * (data.durationMinutes > 0 ? (data.durationMinutes / 30f) : 1f)
        );

        if (oneShotProgressBar != null)
        {
            oneShotProgressBar.gameObject.SetActive(true);
            progressRoutine = StartCoroutine(RunProgressThenStart(seconds, isScene, panel));
        }
        else
        {
            if (isScene)
                behaviorManager.BeginSceneBehavior(data, panel);
            else
                behaviorManager.BeginOneShotBehavior(data, panel);
        }
    }

    private IEnumerator RunProgressThenStart(float seconds, bool isScene, SpoonPanel panel)
    {
        float elapsed = 0f;
        oneShotProgressBar.value = 0f;

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            oneShotProgressBar.value = Mathf.Clamp01(elapsed / seconds);
            yield return null;
        }

        if (isScene)
            behaviorManager.BeginSceneBehavior(data, panel);
        else
            behaviorManager.BeginOneShotBehavior(data, panel);

        progressRoutine = null;

        oneShotProgressBar.value = 0f;
        oneShotProgressBar.gameObject.SetActive(false);
    }

    public void NotifyPanelClosed()
    {
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

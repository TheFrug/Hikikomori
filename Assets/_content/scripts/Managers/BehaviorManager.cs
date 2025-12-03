using System.Collections;
using UnityEngine;
using ProjectHiki.UI;

public class BehaviorManager : MonoBehaviour
{
    private bool waitingForNextTick = false;

    [Header("References")]
    public ResourceManager resourceManager;
    public ClockManager clockManager;
    public TooltipPanel tooltipPanel;

    [Header("Default Behavior")]
    public BehaviorData defaultBehavior;

    [Header("Settings")]
    public float secondsPerGameMinute = 0.1f;

    private BehaviorData currentBehavior;
    private bool isBusy = false;
    private Coroutine behaviorRoutine;

    // The currently-open spoon panel (needed for cleanup timing)
    public SpoonPanel activeSpoonPanel;

    void Start()
    {
        if (clockManager != null)
            clockManager.OnTick += HandleClockTick;

        StartDefaultBehavior();
    }

    void OnDestroy()
    {
        if (clockManager != null)
            clockManager.OnTick -= HandleClockTick;
    }

    public bool IsBusy() => isBusy;

    public void ShowBusyTooltip()
    {
        tooltipPanel?.ShowBusyMessage("Hiki is busy!");
    }

    // -----------------------------------------------------------------
    // FIX #1 — WAIT FOR PANEL BEFORE STARTING BEHAVIOR
    // -----------------------------------------------------------------
    public void BeginSceneBehavior(BehaviorData data, SpoonPanel panel)
    {
        if (panel != null)
        {
            activeSpoonPanel = panel;
            StartCoroutine(WaitForPanelAndRunScene(data, panel));
        }
        else
        {
            BeginSceneBehavior(data);
        }
    }

    private IEnumerator WaitForPanelAndRunScene(BehaviorData data, SpoonPanel panel)
    {
        panel.ClosePanel();

        while (activeSpoonPanel != null)
            yield return null;

        BeginSceneBehavior(data);
    }

    public void BeginOneShotBehavior(BehaviorData data, SpoonPanel panel)
    {
        if (panel != null)
        {
            activeSpoonPanel = panel;
            StartCoroutine(WaitForPanelAndRunOneShot(data, panel));
        }
        else
        {
            BeginOneShotBehavior(data);
        }
    }

    private IEnumerator WaitForPanelAndRunOneShot(BehaviorData data, SpoonPanel panel)
    {
        panel.ClosePanel();

        while (activeSpoonPanel != null)
            yield return null;

        BeginOneShotBehavior(data);
    }

    // -----------------------------------------------------------------
    // PANEL STATE HELPERS
    // -----------------------------------------------------------------
    public bool HasOpenPanel()
    {
        return activeSpoonPanel != null;
    }

    public void RegisterPanel(SpoonPanel panel)
    {
        if (panel == null) return;
        activeSpoonPanel = panel;
    }

    public void ClearPanel(SpoonPanel panel)
    {
        if (panel == null) return;
        if (activeSpoonPanel == panel)
            activeSpoonPanel = null;
    }

    // -----------------------------------------------------------------
    // SCENE BEHAVIOR
    // -----------------------------------------------------------------
    public void BeginSceneBehavior(BehaviorData data)
    {
        if (data == null)
        {
            Debug.LogError("BeginSceneBehavior called with null data");
            return;
        }

        if (isBusy)
        {
            ShowBusyTooltip();
            return;
        }

        isBusy = true;
        currentBehavior = data;

        Debug.Log($"[BehaviorManager] BeginSceneBehavior: {data.behaviorName}");

        if (data.thought != null && ThoughtBubbleView.Instance != null)
            ThoughtBubbleView.Instance.SpawnThought(data.thought);
    }

    // -----------------------------------------------------------------
    // ONE-SHOT BEHAVIOR
    // -----------------------------------------------------------------
    public void BeginOneShotBehavior(BehaviorData data)
    {
        if (data == null)
        {
            Debug.LogError("BeginOneShotBehavior called with null data");
            return;
        }

        if (isBusy)
        {
            ShowBusyTooltip();
            return;
        }

        isBusy = true;
        currentBehavior = data;

        Debug.Log($"[BehaviorManager] BeginOneShotBehavior: {data.behaviorName}");

        if (data.thought != null && ThoughtBubbleView.Instance != null)
            ThoughtBubbleView.Instance.SpawnThought(data.thought);

        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }

    // -----------------------------------------------------------------
    // DEFAULT BEHAVIOR LOOP
    // -----------------------------------------------------------------
    private void StartDefaultBehavior()
    {
        if (defaultBehavior == null)
        {
            Debug.LogWarning("No default behavior assigned!");
            return;
        }

        Debug.Log($"Falling back to default behavior: {defaultBehavior.behaviorName}");

        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        currentBehavior = defaultBehavior;
        isBusy = false;

        behaviorRoutine = StartCoroutine(RunBehavior(defaultBehavior));
    }

    // -----------------------------------------------------------------
    // MAIN BEHAVIOR COROUTINE
    // -----------------------------------------------------------------
    private IEnumerator RunBehavior(BehaviorData data)
    {
        if (resourceManager == null)
        {
            Debug.LogError("BehaviorManager missing ResourceManager!");
            yield break;
        }

        float secondsPerGameMinute = clockManager.realSecondsPerGameTick /
                                     clockManager.minutesPerTick;

        // Infinite default behavior
        if (data.isDefault)
        {
            while (currentBehavior == data)
            {
                float elapsed = 0f;

                while (elapsed < 1f)
                {
                    if (clockManager.CurrentState != ClockManager.ClockState.Paused)
                        elapsed += Time.deltaTime *
                                    clockManager.TimeScaleMultiplier / secondsPerGameMinute;

                    yield return null;
                }

                resourceManager.ModifyResources(
                    data.spoonsCost,
                    data.hungerImpact,
                    data.cashCost
                );
            }

            yield break;
        }

        // One-shot timed behavior
        int totalMinutes = Mathf.Max(1, data.durationMinutes);
        float elapsedMinutes = 0f;

        Debug.Log($"Running behavior '{data.behaviorName}' for {totalMinutes} minutes");

        while (elapsedMinutes < totalMinutes)
        {
            if (clockManager.CurrentState != ClockManager.ClockState.Paused)
                elapsedMinutes += Time.deltaTime *
                                  clockManager.TimeScaleMultiplier / secondsPerGameMinute;

            yield return null;
        }

        resourceManager.ModifyResources(
            data.spoonsCost,
            data.hungerImpact,
            data.cashCost
        );

        isBusy = false;
        StartDefaultBehavior();
    }

    private void HandleClockTick()
    {
        // no-op but required
    }
}

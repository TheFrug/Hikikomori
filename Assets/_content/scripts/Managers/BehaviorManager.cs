using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectHiki.UI; // <-- for ThoughtBubbleView

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

    // Removed ThoughtManager — replaced by ThoughtBubbleView.Instance

    private BehaviorData currentBehavior;
    private bool isBusy = false;
    private Coroutine behaviorRoutine;

    private Queue<BehaviorData> behaviorQueue = new Queue<BehaviorData>();


    // ------------------------------
    // Unity Lifecycle
    // ------------------------------
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


    // ------------------------------
    // Public API
    // ------------------------------
    public void StartBehavior(BehaviorData data)
    {
        if (isBusy)
        {
            ShowBusyTooltip();
            return;
        }

        if (data == null)
        {
            Debug.LogWarning("StartBehavior called with NULL data!");
            return;
        }

        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        currentBehavior = data;
        isBusy = true;

        Debug.Log($"Starting behavior: {data.behaviorName}");

        // --- Thought Integration: use ThoughtBubbleView ---
        if (data.thought != null && ThoughtBubbleView.Instance != null)
        {
            // Thought asset already knows whether it's automatic or interactive.
            ThoughtBubbleView.Instance.SpawnThought(data.thought);
        }

        // Run duration + resources
        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }

    public bool IsBusy() => isBusy;

    public void ShowBusyTooltip()
    {
        tooltipPanel?.ShowBusyMessage("Hiki is busy!");
    }

    // -------------------------------------------------
    // Legacy compatibility for SpoonPanel / BehaviorPanel
    // -------------------------------------------------

    public void BeginSceneBehavior(BehaviorData data, SpoonPanel panel)
    {
        // Optional: hide the panel now that the player committed
        if (panel != null)
            Destroy(panel.gameObject);

        // Call your existing (one-param) method
        BeginSceneBehavior(data);
    }

    public void BeginOneShotBehavior(BehaviorData data, SpoonPanel panel)
    {
        // Optional: hide the panel now that the player committed
        if (panel != null)
            Destroy(panel.gameObject);

        // Call your existing (one-param) method
        BeginOneShotBehavior(data);
    }

    // ------------------------------
    // Actual 1-argument implementations
    // ------------------------------

    public void BeginSceneBehavior(BehaviorData data)
    {
        // Scenes = interactive Thought → BehaviorManager does NOT run duration
        // The ThoughtBubbleView handles the whole interactive sequence.
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

        // Launch interactive Thought
        if (data.thought != null && ThoughtBubbleView.Instance != null)
            ThoughtBubbleView.Instance.SpawnThought(data.thought);

        // When thought ends, ThoughtBubbleView must call something like:
        // BehaviorManager.CompleteSceneBehavior()
    }

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

        // One-shot thoughts (automatic)
        if (data.thought != null && ThoughtBubbleView.Instance != null)
            ThoughtBubbleView.Instance.SpawnThought(data.thought);

        // Run the timed one-shot behavior
        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }


    // Only needed if your BehaviorPanel still calls QueueBehavior.
    // If your new system will never queue, you can delete the calls in UI instead.
    public void QueueBehavior(BehaviorData data)
    {
        StartBehavior(data);
    }



    // ------------------------------
    // Default Behavior Loop
    // ------------------------------
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


    // ------------------------------
    // Main Routine
    // ------------------------------
    private IEnumerator RunBehavior(BehaviorData data)
    {
        if (resourceManager == null)
        {
            Debug.LogError("BehaviorManager missing ResourceManager!");
            yield break;
        }

        float secondsPerGameMinute = clockManager.realSecondsPerGameTick /
                                     clockManager.minutesPerTick;

        // --------------------------------------
        // Infinite-loop default behavior
        // --------------------------------------
        if (data.isDefault)
        {
            while (currentBehavior == data)
            {
                float elapsed = 0f;

                while (elapsed < 1f)
                {
                    if (clockManager.CurrentState != ClockManager.ClockState.Paused)
                        elapsed += Time.deltaTime *
                                   clockManager.TimeScaleMultiplier /
                                   secondsPerGameMinute;

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

        // --------------------------------------
        // Timed one-shot behavior
        // --------------------------------------
        int totalMinutes = Mathf.Max(1, data.durationMinutes);
        float elapsedMinutes = 0f;

        Debug.Log($"Running behavior '{data.behaviorName}' for {totalMinutes} minutes");

        // Wait for the duration
        while (elapsedMinutes < totalMinutes)
        {
            if (clockManager.CurrentState != ClockManager.ClockState.Paused)
                elapsedMinutes += Time.deltaTime *
                                  clockManager.TimeScaleMultiplier /
                                  secondsPerGameMinute;

            yield return null;
        }

        // Spend resources
        resourceManager.ModifyResources(
            data.spoonsCost,
            data.hungerImpact,
            data.cashCost
        );

        // End and return to default
        isBusy = false;
        StartDefaultBehavior();
    }


    // ------------------------------
    // Clock Tick
    // ------------------------------
    private void HandleClockTick()
    {
        // No waiting logic in your posted version,
        // but leaving the method to avoid null subscription warnings.
    }
}

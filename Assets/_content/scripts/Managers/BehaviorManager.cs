using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Yarn.Unity;

public class BehaviorManager : MonoBehaviour
{
    private bool waitingForNextTick = false;

    [Header("References")]
    public ResourceManager resourceManager;
    public ClockManager clockManager;
    public TooltipPanel tooltipPanel; // drag in from inspector

    [Header("Default Behavior")]
    public BehaviorData defaultBehavior;

    [Header("Settings")]
    public float secondsPerGameMinute = 0.1f;

    [Header("Yarn Integration")]
    public DialogueRunner dialogueRunner;

    private BehaviorData currentBehavior; // Keeps track of what is happening
    private bool isBusy = false; // Determines if current behavior is interruptible
    private Coroutine behaviorRoutine;

    private Queue<BehaviorData> behaviorQueue = new Queue<BehaviorData>();
    private HashSet<BehaviorData.BehaviorYarnTrigger> triggeredMidpoints = new HashSet<BehaviorData.BehaviorYarnTrigger>();


    void Start()
    {
        if (clockManager != null){
            clockManager.OnTick += HandleClockTick;
        }
        StartDefaultBehavior();
    }
    void OnDestroy()
    {
        if (clockManager != null)
            clockManager.OnTick -= HandleClockTick;
    }

    /* TryStartBehavior()
    // --- Public API for Buttons ---
    public void TryStartBehavior(BehaviorData data) // Never called
    {
        Debug.Log("Running method: TryStartBehavior(" + data + ")");
        if (isBusy)
        {
            if (tooltipPanel != null)
            {
                Debug.Log("Showing Message: Hiki is busy!");
                tooltipPanel.ShowBusyMessage("Hiki is busy!");
            }
            return;
        }

        if (data == null)
        {
            Debug.LogWarning("No behavior data provided!");
            return;
        }

        StartBehavior(data);
    }
    */

    // --- Main Behavior Flow ---
    public void StartBehavior(BehaviorData data)
    {
        if (isBusy)
        {
            tooltipPanel?.ShowBusyMessage("Hiki is busy!");
            return;
        }

        if (data == null)
        {
            Debug.LogWarning("No behavior data provided!");
            return;
        }

        // Stop current behavior (including infinite default)
        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        currentBehavior = data;
        isBusy = true;

        Debug.Log($"Starting behavior: {data.behaviorName}");
        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }

    private void StartDefaultBehavior()
    {
        if (defaultBehavior == null)
        {
            Debug.LogWarning("No default behavior assigned!");
            return;
        }

        Debug.Log($"Defaulting to: {defaultBehavior.behaviorName}");

        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        isBusy = false;
        currentBehavior = defaultBehavior;
        behaviorRoutine = StartCoroutine(RunBehavior(defaultBehavior));
    }

    private IEnumerator RunBehavior(BehaviorData data) //Called ONCE by StartDefaultBehavior(), NEVER called by StartBehavior()
    {
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not assigned to BehaviorManager!");
            yield break;
        }

        float secondsPerGameMinute = clockManager.realSecondsPerGameTick / clockManager.minutesPerTick;

        if (data.isDefault)
        {
            Debug.Log($"Running default behavior indefinitely: {data.behaviorName}");

            while (currentBehavior == data)
            {
                float elapsed = 0f;

                // Wait until one in-game minute passes, respecting clock state
                while (elapsed < 1f)
                {
                    if (clockManager.CurrentState == ClockManager.ClockState.Paused)
                    {
                        yield return null; // paused → do nothing
                        continue;
                    }

                    elapsed += Time.deltaTime * clockManager.TimeScaleMultiplier / secondsPerGameMinute;
                    yield return null;
                }

                // Apply per-minute resource updates
                resourceManager.ModifyResources(
                    data.spoonsCost,
                    data.hungerImpact,
                    data.cashCost
                );
            }
        }
        else
        {
            int totalMinutes = Mathf.Max(1, data.durationMinutes);
            secondsPerGameMinute = clockManager.realSecondsPerGameTick / clockManager.minutesPerTick;

            // --- Gradual Update Setup ---
            int totalHungerChange = Mathf.RoundToInt(data.hungerImpact);
            int totalCashChange = Mathf.RoundToInt(data.cashCost);
            int hungerSteps = Mathf.Abs(totalHungerChange);
            int cashSteps = Mathf.Abs(totalCashChange);
            int totalSteps = Mathf.Max(hungerSteps, cashSteps, 1);

            float minutesPerStep = (float)totalMinutes / totalSteps;

            Debug.Log($"Running behavior '{data.behaviorName}' for {totalMinutes} min with {totalSteps} steps (~{minutesPerStep:F2} min per step)");

            TriggerYarnFor(data, BehaviorData.BehaviorYarnTrigger.TriggerTime.OnStart);
            float elapsedMinutes = 0f;
            int step = 0;

            while (elapsedMinutes < totalMinutes)
            {
                // Respect pause state
                if (clockManager.CurrentState == ClockManager.ClockState.Paused)
                {
                    yield return null;
                    continue;
                }

                // Advance elapsed time based on clock speed
                elapsedMinutes += Time.deltaTime * clockManager.TimeScaleMultiplier / secondsPerGameMinute;

                // When enough in-game minutes have passed, apply one tick
                if (elapsedMinutes >= (step + 1) * minutesPerStep)
                {
                    step++;

                    int hungerDelta = 0;
                    int cashDelta = 0;

                    if (step <= hungerSteps)
                        hungerDelta = (totalHungerChange > 0) ? +1 : -1;

                    if (step <= cashSteps)
                        cashDelta = (totalCashChange > 0) ? +1 : -1;

                    resourceManager.ModifyResources(0, hungerDelta, cashDelta);
                }

                CheckForMidpointTriggers(data, elapsedMinutes, totalMinutes);

                yield return null;
            }
            TriggerYarnFor(data, BehaviorData.BehaviorYarnTrigger.TriggerTime.OnEnd);
            // Spend spoons at the end
            resourceManager.ModifyResources(data.spoonsCost, 0, 0);
            Debug.Log($"Finished behavior: {data.behaviorName}");
            isBusy = false;
            currentBehavior = null;
            StartDefaultBehavior();
        }
    }

    public void QueueBehavior(BehaviorData data)
    {
        Debug.Log($"QueueBehavior(): isBusy={isBusy}, behaviour={data?.behaviorName}");
        if (data == null)
        {
            Debug.LogWarning("Tried to queue a null behavior!");
            return;
        }

        // If Hiki is idle, start immediately
        if (!isBusy)
        {
            Debug.Log($"No active behavior — starting {data.behaviorName} immediately.");
            StartBehavior(data);
            return;
        }

        // Only one queued behavior allowed
        if (behaviorQueue.Count > 0)
        {
            Debug.Log($"Behavior queue already has one task ({behaviorQueue.Peek().behaviorName}). Ignoring new queue request.");
            tooltipPanel?.ShowBusyMessage("Hiki already has something planned!");
            return;
        }

        // Add to queue
        behaviorQueue.Enqueue(data);
        waitingForNextTick = true;
        Debug.Log($"Queued behavior: {data.behaviorName}. It will start at the next tick.");
    }


    // --- Yarn Integration ---
    private void TriggerYarnFor(BehaviorData data, BehaviorData.BehaviorYarnTrigger.TriggerTime triggerType)
    {
        if (data.yarnTrigger == null || dialogueRunner == null)
            return;

        foreach (var trigger in data.yarnTrigger)
        {
            if (trigger.triggerTime == triggerType && !string.IsNullOrEmpty(trigger.yarnNodeName))
            {
                Debug.Log($"Triggering Yarn node '{trigger.yarnNodeName}' for {triggerType} of {data.behaviorName}");
                StartCoroutine(RunYarnNode(trigger.yarnNodeName));
            }
        }
    }

    private void CheckForMidpointTriggers(BehaviorData data, float elapsedMinutes, float totalMinutes)
    {
        if (data.yarnTrigger == null)
            return;

        foreach (var trigger in data.yarnTrigger)
        {
            if (trigger.triggerTime == BehaviorData.BehaviorYarnTrigger.TriggerTime.OnMidpoint)
            {
                float targetMinute = trigger.triggerMinute > 0 ? trigger.triggerMinute : totalMinutes / 2f;
                if (elapsedMinutes >= targetMinute && !triggeredMidpoints.Contains(trigger))
                {
                    triggeredMidpoints.Add(trigger);
                    StartCoroutine(RunYarnNode(trigger.yarnNodeName));
                }
            }
        }
    }

    private IEnumerator RunYarnNode(string nodeName)
    {
        if (clockManager != null)
            clockManager.PauseClock();

        dialogueRunner.StartDialogue(nodeName);

        // Wait until Yarn finishes
        while (dialogueRunner.IsDialogueRunning)
            yield return null;

        if (clockManager != null)
            clockManager.PlayClock();
    }

    // --- Clock Tick System ---
    private void HandleClockTick()
    {
        //Debug.Log($"HandleClockTick(): waitingForNextTick={waitingForNextTick}, queueCount={behaviorQueue.Count}, isBusy={isBusy}, currentBehavior={(currentBehavior==null? "null": currentBehavior.behaviorName)}");

        // Only trigger queued behaviors on tick boundaries
        if (waitingForNextTick && behaviorQueue.Count > 0 && !isBusy)
        {
            var nextBehavior = behaviorQueue.Dequeue();
            waitingForNextTick = false;

            Debug.Log($"Clock tick triggered queued behavior: {nextBehavior.behaviorName}");
            StartBehavior(nextBehavior);
        }
        // If no queued task and Hiki is idle, return to default
        else if (!isBusy && behaviorQueue.Count == 0 && currentBehavior != defaultBehavior)
        {
            Debug.Log("Clock tick found no queued behavior. Returning to default idle.");
            StartDefaultBehavior();
        }
    }
}

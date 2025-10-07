using System.Collections;
using UnityEngine;
using TMPro;

public class BehaviorManager : MonoBehaviour
{
    [Header("References")]
    public ResourceManager resourceManager;
    public ClockManager clockManager;
    public TooltipPanel tooltipPanel; // drag in from inspector

    [Header("Default Behavior")]
    public BehaviorData defaultBehavior;

    [Header("Settings")]
    public float secondsPerGameMinute = 0.1f;

    private BehaviorData currentBehavior;
    private bool isBusy = false;
    private Coroutine behaviorRoutine;

    void Start()
    {
        StartDefaultBehavior();
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

                yield return null;
            }

            // Spend spoons at the end
            resourceManager.ModifyResources(data.spoonsCost, 0, 0);
            Debug.Log($"Finished behavior: {data.behaviorName}");
            isBusy = false;
            currentBehavior = null;
            StartDefaultBehavior();
        }
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
}

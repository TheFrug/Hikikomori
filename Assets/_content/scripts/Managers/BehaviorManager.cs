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

    // --- Public API for Buttons ---
    public void TryStartBehavior(BehaviorData data)
    {
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

    // --- Main Behavior Flow ---
    private void StartBehavior(BehaviorData data)
    {
        // Stop current behavior (including infinite default)
        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        currentBehavior = data;
        isBusy = true;

        Debug.Log($"Starting behavior: {data.behaviorName}");

        if (data.isDefault)
            Debug.LogWarning("Starting a default behavior manually—usually not needed.");

        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }

    private IEnumerator RunBehavior(BehaviorData data)
    {
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not assigned to BehaviorManager!");
            yield break;
        }

        if (data.isDefault)
        {
            Debug.Log($"Running default behavior indefinitely: {data.behaviorName}");

            while (isBusy && currentBehavior == data)
            {
                resourceManager.ModifyResources(
                    data.spoonsCost * secondsPerGameMinute,
                    data.hungerImpact * secondsPerGameMinute,
                    data.cashCost * secondsPerGameMinute
                );

                yield return new WaitForSeconds(secondsPerGameMinute);
            }
        }
        else
        {
            int totalMinutes = Mathf.Max(1, data.durationMinutes);
            float interval = secondsPerGameMinute;

            // Calculate deltas
            float spoonDeltaPerMinute = (float)data.spoonsCost / totalMinutes;
            float hungerDeltaPerMinute = (float)data.hungerImpact / totalMinutes;
            float cashDeltaPerMinute = (float)data.cashCost / totalMinutes;

            for (int minute = 0; minute < totalMinutes; minute++)
            {
                resourceManager.ModifyResources(spoonDeltaPerMinute, hungerDeltaPerMinute, cashDeltaPerMinute);
                yield return new WaitForSeconds(interval);
            }

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

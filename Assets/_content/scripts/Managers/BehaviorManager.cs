using System.Collections;
using UnityEngine;
using ProjectHiki.UI;

public class BehaviorManager : MonoBehaviour
{
    // Removed: waitingForNextTick, clockManager, defaultBehavior

    [Header("References")]
    public ResourceManager resourceManager;
    public TooltipPanel tooltipPanel;

    [Header("Settings")]
    public float secondsPerGameMinute = 0.1f;

    private BehaviorData currentBehavior;
    private bool isBusy = false;
    private Coroutine behaviorRoutine;

    // The currently-open spoon panel (needed for cleanup timing)
    public SpoonSlotPanel activeSpoonPanel;

    void Start()
    {
        // Removed clock subscription
        // Removed StartDefaultBehavior()
    }

    void OnDestroy()
    {
        // Removed clock unsubscribe
    }

    public bool IsBusy() => isBusy;

    public void ShowBusyTooltip()
    {
        tooltipPanel?.ShowBusyMessage("Hiki is busy!");
    }

    // -----------------------------------------------------------------
    // WAIT FOR PANEL BEFORE STARTING BEHAVIOR
    // -----------------------------------------------------------------
    public void BeginSceneBehavior(BehaviorData data, SpoonSlotPanel panel)
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

    private IEnumerator WaitForPanelAndRunScene(BehaviorData data, SpoonSlotPanel panel)
    {
        panel.CloseAfterConfirmedBehavior();

        while (activeSpoonPanel != null)
            yield return null;

        BeginSceneBehavior(data);
    }

    public void BeginOneShotBehavior(BehaviorData data, SpoonSlotPanel panel)
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

    private IEnumerator WaitForPanelAndRunOneShot(BehaviorData data, SpoonSlotPanel panel)
    {
        panel.CloseAfterConfirmedBehavior();

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

    public void RegisterPanel(SpoonSlotPanel panel)
    {
        if (panel == null) return;
        activeSpoonPanel = panel;
    }

    public void ClearPanel(SpoonSlotPanel panel)
    {
        if (panel == null) return;
        if (activeSpoonPanel == panel)
            activeSpoonPanel = null;
    }

    // -----------------------------------------------------------------
    // REMOVED DUPLICATED METHODS — keeping the real ones below
    // -----------------------------------------------------------------

    public void BeginSceneBehavior(BehaviorData data)
    {
        if (isBusy)
        {
            ShowBusyTooltip();
            return;
        }

        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        currentBehavior = data;
        isBusy = true;
        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }

    public void BeginOneShotBehavior(BehaviorData data)
    {
        if (isBusy)
        {
            ShowBusyTooltip();
            return;
        }

        if (behaviorRoutine != null)
            StopCoroutine(behaviorRoutine);

        currentBehavior = data;
        isBusy = true;
        behaviorRoutine = StartCoroutine(RunBehavior(data));
    }

    // -----------------------------------------------------------------
    // MAIN BEHAVIOR COROUTINE — now ONLY handles one-shot timed behaviors
    // -----------------------------------------------------------------
    private IEnumerator RunBehavior(BehaviorData data)
    {
        if (resourceManager == null)
        {
            Debug.LogError("BehaviorManager missing ResourceManager!");
            yield break;
        }

        // Removed all clock calculations

        int totalMinutes = Mathf.Max(1, data.durationMinutes);
        float elapsedMinutes = 0f;

        Debug.Log($"Running behavior '{data.behaviorName}' for {totalMinutes} minutes");

        while (elapsedMinutes < totalMinutes)
        {
            elapsedMinutes += Time.deltaTime / secondsPerGameMinute;
            yield return null;
        }

        resourceManager.ModifyResources(
            data.spoonsCost,
            data.hungerImpact,
            data.cashCost
        );

        // Close spoon panel if tied to this behavior
        if (activeSpoonPanel != null)
        {
            var panelToClose = activeSpoonPanel;
            activeSpoonPanel = null;
            panelToClose.CloseAfterConfirmedBehavior();
        }

        isBusy = false;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ProjectHiki.UI;
using System;

public class BehaviorManager : MonoBehaviour
{
    [Header("References")]
    public ResourceManager resourceManager;
    public ClockManager clockManager;   // No longer used, but kept if other systems reference it
    public TooltipPanel tooltipPanel;

    private bool behaviorRunning = false;
    private BehaviorChoice pendingChoice;     // Stored until interactive thought finishes

    public static event Action OnBehaviorStarted;

    [SerializeField]
    private BlackoutOverlayController blackout;

    // ---------- NEW: persistent used-today tracking ----------
    // Use a HashSet keyed by a stable identifier for the behavior. Using behaviorName for now,
    // but consider adding a dedicated ID field on BehaviorData in future.
    private HashSet<string> usedTodayBehaviors = new HashSet<string>();

    public bool IsBehaviorUsedToday(BehaviorData data)
    {
        if (data == null) return false;
        return usedTodayBehaviors.Contains(data.behaviorName);
    }

    public void MarkBehaviorUsed(BehaviorData data)
    {
        if (data == null) return;
        if (!data.repeatable)
            usedTodayBehaviors.Add(data.behaviorName);
    }

    // Call this when your game's day advances
    public void ResetDailyBehaviors()
    {
        usedTodayBehaviors.Clear();
    }

    // -----------------------------------------------------------------
    // BUSY TOOLTIP
    // -----------------------------------------------------------------
    public void ShowBusyTooltip()
    {
        tooltipPanel?.ShowBusyMessage("Hiki is busy!");
    }

    // new: generic message helper so callers can display a specific message
    public void ShowTooltip(string message)
    {
        tooltipPanel?.ShowBusyMessage(message);
    }

    // -----------------------------------------------------------------
    // PUBLIC UNIFIED ENTRY POINT
    // -----------------------------------------------------------------
    // DEPRECATED
    public void RunBehavior(BehaviorChoice choice)
    {
        if (choice == null || choice.BehaviorData == null)
        {
            Debug.LogError("RunBehavior called with null BehaviorChoice or invalid BehaviorData");
            return;
        }

        if (behaviorRunning)
        {
            ShowBusyTooltip();
            return;
        }

        // STEP 1: APPLY BASE COSTS
        ApplyBaseCosts(choice);

        // STEP 2: SPAWN THOUGHT
        var thought = choice.BehaviorData.thought;
        if (thought == null)
        {
            Debug.LogWarning($"Behavior {choice.BehaviorData.behaviorName} has no ThoughtData. Finishing immediately.");
            FinishBehavior(choice);
            return;
        }

        behaviorRunning = true;

        if (thought.type == ThoughtData.ThoughtType.Automatic)
        {
            ThoughtBubbleManager_New.Instance?.StartThought(thought);
            FinishBehavior(choice);
        }
        else
        {
            // Interactive Thought: wait for event
            blackout?.FadeIn();

            pendingChoice = choice;
            ThoughtBubbleManager_New.BubbleFinished.AddListener(OnInteractiveThoughtFinished);

            ThoughtBubbleManager_New.Instance?.StartThought(thought);
        }
    }

    // -----------------------------------------------------------------
    // BASE RESOURCE CHANGES (pre-thought)
    // -----------------------------------------------------------------
    private void ApplyBaseCosts(BehaviorChoice choice)
    {
        resourceManager.ModifyResources(choice.BehaviorData);
    }

    // -----------------------------------------------------------------
    // EVENT CALLBACK FOR INTERACTIVE THOUGHT
    // -----------------------------------------------------------------
    private void OnInteractiveThoughtFinished()
    {
        ThoughtBubbleManager_New.BubbleFinished.RemoveListener(OnInteractiveThoughtFinished);

        blackout?.FadeOut();

        if (pendingChoice != null)
        {
            FinishBehavior(pendingChoice);
            pendingChoice = null;
        }
    }

    // -----------------------------------------------------------------
    // FINALIZE BEHAVIOR (post-thought)
    // -----------------------------------------------------------------
    private void FinishBehavior(BehaviorChoice choice)
    {
        var data = choice.BehaviorData;

        // Centralized persistence
        if (!data.repeatable)
            MarkBehaviorUsed(data);

        // Apply any dialogue-driven resource changes (handled externally)
        resourceManager.ApplyPendingDialogueChanges();

        // Update UI
        resourceManager.UpdateAllUI();

        behaviorRunning = false;
    }

    // TEMP METHODS FOR COMPILATION
    public void ClearPanel(SpoonPanel panel)
    {
        // OPTION B: simple removal (no pooled cleanup yet)
    }
}

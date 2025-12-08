using System.Collections;
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

    // -----------------------------------------------------------------
    // BUSY TOOLTIP
    // -----------------------------------------------------------------
    public void ShowBusyTooltip()
    {
        tooltipPanel?.ShowBusyMessage("Hiki is busy!");
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

        // STEP 1: VALIDATION
        if (!Validate(choice))
            return;

        // STEP 2: APPLY BASE COSTS
        ApplyBaseCosts(choice);

        // STEP 3: SPAWN THOUGHT
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
            pendingChoice = choice;
            ThoughtBubbleManager_New.BubbleFinished.AddListener(OnInteractiveThoughtFinished);

            ThoughtBubbleManager_New.Instance?.StartThought(thought);
        }
    }

    // -----------------------------------------------------------------
    // VALIDATION
    // -----------------------------------------------------------------
    private bool Validate(BehaviorChoice choice)
    {
        var data = choice.BehaviorData;

        // Check spoon cost (SpoonDrawer usually ensures enough spoons are placed)
        if (!resourceManager.HasEnoughSpoons(data.spoonsCost))
        {
            tooltipPanel?.ShowBusyMessage("Not enough spoons.");
            return false;
        }

        // Stress check
        if (resourceManager.CurrentStress >= resourceManager.MaxStress)
        {
            tooltipPanel?.ShowBusyMessage("Too stressed to continue.");
            return false;
        }

        // Cooldowns / once-per-day lockouts
        if (!data.repeatable && choice.usedToday)
        {
            print("You can't do that again today.");
            return false;
        }

        return true;
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

        // Mark non-repeatable behavior used
        if (!data.repeatable)
            choice.usedToday = true;

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

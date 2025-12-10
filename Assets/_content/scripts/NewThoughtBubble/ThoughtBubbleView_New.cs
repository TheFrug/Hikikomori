using System.Threading;
using Yarn;
using Yarn.Unity;
using UnityEngine;

#nullable enable

public class ThoughtBubbleView_New : DialoguePresenterBase
{
    private bool yarnFinished = false;

    // Notify all registered action-markup handlers (e.g. LineAdvancer)
    // that the line is fully visible.
    private void NotifyLineDisplayComplete()
    {
        // ActionMarkupHandlers is provided by DialoguePresenterBase
        var handlers = this.ActionMarkupHandlers;
        if (handlers != null)
        {
            for (int i = 0; i < handlers.Count; i++)
            {
                try
                {
                    handlers[i].OnLineDisplayComplete();
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"ActionMarkupHandler threw in OnLineDisplayComplete: {ex}");
                }
            }
        }
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        yarnFinished = true;

        var mgr = ThoughtBubbleManager_New.Instance;
        if (mgr != null)
        {
            // If the manager thinks this was interactive, we still run EndInteractiveSession
            // (that will schedule the wait-for-visuals). If not interactive, notify manager
            // the runner finished so manager can wait for visuals too.
            if (mgr.IsInteractiveSession)
                mgr.EndInteractiveSession();
            else
                mgr.NotifyDialogueRunnerFinished_WaitForVisuals();
        }

        TryClearIfDone();

        return YarnTask.CompletedTask;
    }

    private void TryClearIfDone()
    {
        if (yarnFinished && ThoughtBubbleManager_New.Instance._active.Count == 0)
        {
            ThoughtBubbleManager_New.Instance.ClearAll();
            yarnFinished = false;
        }
    }

    // ----- LINES -----
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = line.CharacterName ?? string.Empty;
        string processedText = line.TextWithoutCharacterName.Text ?? line.RawText ?? string.Empty;

        var mgr = ThoughtBubbleManager_New.Instance;
        if (mgr == null)
        {
            // Nothing to show; complete the task so the runner can continue.
            return;
        }

        // Show the bubble visually
        mgr.ShowBubble(speaker, processedText);

        // Tell LineAdvancer and any other handlers that the line is fully visible.
        // This allows LineAdvancer to enter the "Waiting" state so user input advances
        // to the next line (instead of just "hurry up").
        NotifyLineDisplayComplete();

        // INTERACTIVE MODE — wait until the player advances (or hurry-up)
        if (mgr.IsInteractiveSession)
        {
            // Wait until DialogueRunner requests next line (user pressed advance)
            // or requests hurry-up (treat as advance here to keep behavior responsive).
            while (!token.IsNextLineRequested && !token.IsHurryUpRequested)
                await YarnTask.Yield();

            // returning from this method completes the presenter's task — the runner will proceed
            return;
        }

        // AUTOMATIC MODE — short yield to avoid tight loop; manager controls floating/duration.
        float displayDuration = 0.1f;
        float start = Time.time;

        while (!token.IsNextLineRequested
            && !token.IsHurryUpRequested
            && Time.time < start + displayDuration)
        {
            await YarnTask.Yield();
        }

        // returning completes the task and the runner moves on
    }

    // ----- OPTIONS -----
    public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
    {
        var mgr = ThoughtBubbleManager_New.Instance;
        if (mgr == null)
        {
            // If we can't present options, return the "no selection" marker
            // (Yarn docs: return YarnAsync.NoOptionSelected) — but returning null
            // is acceptable if your runner accepts it. Use null here for your current flow.
            return null;
        }

        bool selectionMade = false;
        int selectedIndex = -1;

        mgr.PresentOptionsAsBubbles(options, idx =>
        {
            selectionMade = true;
            selectedIndex = idx;
        });

        // Notify handlers that options are now visible (so LineAdvancer can handle input correctly).
        NotifyLineDisplayComplete();

        // Wait for selection or cancellation
        while (!selectionMade && !cancellationToken.IsCancellationRequested)
            await YarnTask.Yield();

        if (cancellationToken.IsCancellationRequested)
        {
            mgr.ClearAll();
            return null;
        }

        if (selectedIndex >= 0 && selectedIndex < options.Length)
            return options[selectedIndex];

        return null;
    }

    private async YarnTask WaitForNextThought(float delay, LineCancellationToken token)
    {
        float endTime = Time.time + delay;

        while (Time.time < endTime && !token.IsNextLineRequested && !token.IsHurryUpRequested)
            await YarnTask.Yield();
    }

    private void OnEnable()
    {
        ThoughtBubbleManager_New.BubbleFinished.AddListener(TryClearIfDone);
    }

    private void OnDisable()
    {
        ThoughtBubbleManager_New.BubbleFinished.RemoveListener(TryClearIfDone);
    }
}

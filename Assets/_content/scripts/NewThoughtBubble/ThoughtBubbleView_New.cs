using System.Threading;
using Yarn;
using Yarn.Unity;
using UnityEngine;

#nullable enable

public class ThoughtBubbleView_New : DialoguePresenterBase
{

    private bool yarnFinished = false;

    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        yarnFinished = true;
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

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = line.CharacterName ?? string.Empty;
        string processedText = line.TextWithoutCharacterName.Text ?? line.RawText ?? string.Empty;

        var mgr = ThoughtBubbleManager_New.Instance;
        if (mgr == null)
            return;

        mgr.ShowBubble(speaker, processedText);
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
    {
        // Automatic-only for now
        return YarnTask.FromResult<DialogueOption?>(null);
    }

    private async YarnTask WaitForNextThought(float delay, LineCancellationToken token)
    {
        float endTime = Time.time + delay;

        while (Time.time < endTime && !token.IsNextLineRequested)
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

using System.Threading;
using Yarn;
using Yarn.Unity;
using UnityEngine;

#nullable enable

public class ThoughtBubbleView_New : DialoguePresenterBase
{
    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        ThoughtBubbleManager_New.Instance?.ClearAll();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        string speaker = line.CharacterName ?? string.Empty;
        string processedText = line.TextWithoutCharacterName.Text ?? line.RawText ?? string.Empty;

        var mgr = ThoughtBubbleManager_New.Instance;
        if (mgr == null)
            return;

        mgr.ShowBubble(speaker, processedText);

        await WaitForNextThought(mgr.CurrentSpawnDelay, token);
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

}

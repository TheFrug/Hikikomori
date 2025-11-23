using System.Threading;
using System.Threading.Tasks;
using Yarn;
using Yarn.Unity;

#nullable enable

public class ThoughtBubbleView_New : DialoguePresenterBase
{
    public override YarnTask OnDialogueStartedAsync()
    {
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        ThoughtBubbleManager_New.Instance.ClearAll();
        return YarnTask.CompletedTask;
    }

    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        // Use Yarn's processed text so markup processors run.
        string speaker = line.CharacterName ?? string.Empty;
        string processedText = line.TextWithoutCharacterName.Text ?? line.RawText ?? string.Empty;

        ThoughtBubbleManager_New.Instance.ShowBubble(speaker, processedText);

        await WaitForBubbleDone(token);
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
    {
        // Automatic-only for now: do not handle options
        return YarnTask.FromResult<DialogueOption?>(null);
    }

    private async YarnTask WaitForBubbleDone(LineCancellationToken token)
    {
        bool finished = false;

        // If Yarn requests next line (skip), bail out.
        // Register manager callback which will set finished = true when a bubble finishes.
        var mgr = ThoughtBubbleManager_New.Instance;
        if (mgr == null)
        {
            // Nothing to wait on
            return;
        }

        System.Action onFinish = () => finished = true;
        mgr.OnBubbleFinished = onFinish;

        // Poll until manager signals finished OR Yarn asks to skip to next line
        while (!finished && !token.IsNextLineRequested)
        {
            await YarnTask.Yield();
        }

        // Clear callback to avoid accidental reuse
        if (mgr != null && mgr.OnBubbleFinished == onFinish)
            mgr.OnBubbleFinished = null;
    }
}
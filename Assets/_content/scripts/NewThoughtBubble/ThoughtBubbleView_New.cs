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
        StartThoughtBubble(line);
        await WaitForBubbleDone(token);
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
    {
        // Automatic-only for now: do not handle options
        return YarnTask.FromResult<DialogueOption?>(null);
    }

    private void StartThoughtBubble(LocalizedLine line)
    {
        string speaker = line.CharacterName ?? string.Empty;
        string rawText = line.RawText ?? string.Empty;

        ThoughtBubbleManager_New.Instance.ShowBubble(speaker, rawText);
    }

    private async YarnTask WaitForBubbleDone(LineCancellationToken token)
    {
        bool finished = false;

        // Yarn cancellation or skip
        void OnTokenSkip()
        {
            finished = true;
        }

        // Register with Yarn's line cancellation by polling IsNextLineRequested
        // Manager will set OnBubbleFinished when a bubble exits screen
        ThoughtBubbleManager_New.Instance.OnBubbleFinished = () =>
        {
            finished = true;
        };

        // Poll until manager signals finished OR Yarn asks to skip to next line
        while (!finished && !token.IsNextLineRequested)
        {
            await YarnTask.Yield();
        }

        // Clear callback to avoid accidental reuse
        ThoughtBubbleManager_New.Instance.OnBubbleFinished = null;
    }
}

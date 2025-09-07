#nullable enable
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Yarn.Unity;

public class CustomDialoguePresenter : DialoguePresenterBase
{
    [Header("UI References")]
    public TextMeshProUGUI nameplateText;
    public TextMeshProUGUI dialogueText;
    public Image dialogueBox;

    [Header("Character Database (optional)")]
    public CharacterDatabase database;

    // Called when dialogue starts
    public override YarnTask OnDialogueStartedAsync()
    {
        // Show or prepare UI here if you want.
        return YarnTask.CompletedTask;
    }

    // Called when dialogue ends
    public override YarnTask OnDialogueCompleteAsync()
    {
        // Hide/cleanup UI if you want.
        return YarnTask.CompletedTask;
    }

    // Called when a line should be shown
    public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
            Debug.Log($"[CustomLinePresenter] Got line: Character={line.CharacterName}, Text={line.TextWithoutCharacterName.Text}");
            
        // Look up style by speaker name
        CharacterProfile profile = database?.GetProfile(line.CharacterName);

        if (profile != null) {
            nameplateText.text = profile.characterName;

            Debug.Log($"Line from {line.CharacterName} (text={line.TextWithoutCharacterName.Text})");

            nameplateText.color = profile.nameplateColor;
            dialogueBox.color = profile.dialogueBoxColor;
            dialogueText.color = profile.fontColor;
            if (profile.font != null) {
                dialogueText.font = profile.font;
            }
        } else {
            // Fallback styling
            nameplateText.text = line.CharacterName;
            dialogueText.color = Color.white;
        }

        // Put the text into the UI
        dialogueText.text = line.TextWithoutCharacterName.Text; // plain string

        // Wait until player advances
        await YarnTask.WaitUntilCanceled(token.NextLineToken);
    }

    // Called when options should be shown
    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
    {
        // We’re not handling options here; return null
        return YarnTask.FromResult<DialogueOption?>(null);
    }
}

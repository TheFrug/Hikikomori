using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

public class CustomDialoguePresenter : DialoguePresenterBase
{
    [Header("UI References")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image backgroundImage; // optional - assign your background Image here

    [Header("Character Colors")]
    public Color defaultColor = Color.white;

    private readonly Dictionary<string, Color> characterColors = new Dictionary<string, Color>
    {
        { "Goblin", Color.green },
        { "Volition", Color.magenta },
        { "Lady Kindly", Color.yellow }
    };

    // Flag set when Continue button is pressed
    private bool continueClicked = false;

    public override YarnTask OnDialogueStartedAsync()
    {
        // make sure the presenter is visible when dialogue starts
        gameObject.SetActive(true);
        TryFixCanvasGroupVisibility();
        if (characterNameText != null) { characterNameText.text = ""; }
        if (dialogueText != null) { dialogueText.text = ""; }
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        gameObject.SetActive(false);
        return YarnTask.CompletedTask;
    }

    public override YarnTask RunLineAsync(LocalizedLine dialogueLine, LineCancellationToken cancellationToken)
    {
        Debug.Log($"[CustomDialoguePresenter] Got line: {dialogueLine.CharacterName}: {dialogueLine.TextWithoutCharacterName.Text}");

        // Basic sanity checks
        if (characterNameText == null) Debug.LogError("[CustomDialoguePresenter] characterNameText is NOT assigned!");
        if (dialogueText == null) Debug.LogError("[CustomDialoguePresenter] dialogueText is NOT assigned!");
        if (characterNameText == null || dialogueText == null)
        {
            // Can't draw anything; return immediately so Yarn doesn't hang.
            return YarnTask.CompletedTask;
        }

        // Ensure the presenter and its canvas group are visible
        gameObject.SetActive(true);
        TryFixCanvasGroupVisibility();

        // Ensure TMP components are enabled and full-alpha
        characterNameText.enabled = true;
        dialogueText.enabled = true;

        // Force alpha to 1 on both text components
        var cn = characterNameText.color;
        characterNameText.color = new Color(cn.r, cn.g, cn.b, 1f);
        var ct = dialogueText.color;
        dialogueText.color = new Color(ct.r, ct.g, ct.b, 1f);

        // Put the text in
        characterNameText.text = dialogueLine.CharacterName;
        dialogueText.text = dialogueLine.TextWithoutCharacterName.Text;

        // Apply color mapping
        if (characterColors.TryGetValue(dialogueLine.CharacterName, out var color))
            dialogueText.color = new Color(color.r, color.g, color.b, 1f);
        else
            dialogueText.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 1f);

        // Make sure background image is enabled if assigned
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            // If background image has zero alpha, force full alpha for debug
            var bgCol = backgroundImage.color;
            backgroundImage.color = new Color(bgCol.r, bgCol.g, bgCol.b, 1f);
        }

        // Debug important layout and visibility properties — paste these logs if still invisible
        LogUIState();

        // Reset continue flag for this line
        continueClicked = false;

        // Wait for continue button, keys, or Yarn requesting next-line.
        return YarnTask.WaitUntil(
            () =>
                continueClicked
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || cancellationToken.IsNextLineRequested,
            cancellationToken.NextLineToken
        );
    }

    // Hook this function up in the Continue button's OnClick() (drag the Line Presenter object, choose CustomDialoguePresenter -> OnContinueClicked)
    public void OnContinueClicked()
    {
        continueClicked = true;
    }

    public override YarnTask<DialogueOption> RunOptionsAsync(DialogueOption[] options, CancellationToken cancellationToken)
    {
        Debug.LogWarning("Auto-selecting first option — RunOptionsAsync not fully implemented.");
        return YarnTask.FromResult(options[0]);
    }

    // ---- Helpers ----

    private void TryFixCanvasGroupVisibility()
    {
        // If there's a CanvasGroup on this or parent, ensure it's visible/interactive.
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = GetComponentInParent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        else
        {
            Debug.Log("[CustomDialoguePresenter] No CanvasGroup found on this object or parents.");
        }

        // Also ensure the Canvas itself is enabled
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = true;
            Debug.Log($"[CustomDialoguePresenter] Found Canvas (renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}).");
        }
        else
        {
            Debug.Log("[CustomDialoguePresenter] No Canvas found in parents.");
        }
    }

    private void LogUIState()
    {
        Debug.Log($"[CustomDialoguePresenter] activeInHierarchy={gameObject.activeInHierarchy}");
        Debug.Log($"[CustomDialoguePresenter] characterNameText.enabled={characterNameText.enabled}, color={characterNameText.color}, font={(characterNameText.font != null ? characterNameText.font.name : "NULL")}");
        Debug.Log($"[CustomDialoguePresenter] dialogueText.enabled={dialogueText.enabled}, color={dialogueText.color}, font={(dialogueText.font != null ? dialogueText.font.name : "NULL")}");
        var rtName = characterNameText.rectTransform;
        var rtText = dialogueText.rectTransform;
        Debug.Log($"[CustomDialoguePresenter] nameRT anchoredPos={rtName.anchoredPosition}, size={rtName.sizeDelta}, lossyScale={rtName.lossyScale}");
        Debug.Log($"[CustomDialoguePresenter] textRT anchoredPos={rtText.anchoredPosition}, size={rtText.sizeDelta}, lossyScale={rtText.lossyScale}");
        if (backgroundImage != null)
        {
            Debug.Log($"[CustomDialoguePresenter] backgroundImage.enabled={backgroundImage.enabled}, color={backgroundImage.color}");
        }
    }
}

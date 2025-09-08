/*
Yarn Spinner is licensed to you under the terms found in the file LICENSE.md.
*/

using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity.Attributes;
using TMPro;
using Yarn.Unity;
using System;

#nullable enable

namespace Yarn.Unity
{
    [HelpURL("https://docs.yarnspinner.dev/using-yarnspinner-with-unity/components/dialogue-view/line-view")]
    public sealed class CustomDialoguePresenter : DialoguePresenterBase
    {
        [Space]
        [MustNotBeNull]
        public CanvasGroup? canvasGroup;

        [MustNotBeNull]
        public TMP_Text? lineText;

        [Group("Character")]
        [Label("Shows Name")]
        public bool showCharacterNameInLineView = true;

        [Group("Character")]
        [ShowIf(nameof(showCharacterNameInLineView))]
        [Label("Name field")]
        [MustNotBeNullWhen(nameof(showCharacterNameInLineView), "A text field must be provided when Shows Name is set")]
        public TMP_Text? characterNameText;

        [Group("Character")]
        [ShowIf(nameof(showCharacterNameInLineView))]
        public GameObject? characterNameContainer = null;

        [Group("Fade")]
        [Label("Fade UI")]
        public bool useFadeEffect = true;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeUpDuration = 0.25f;

        [Group("Fade")]
        [ShowIf(nameof(useFadeEffect))]
        public float fadeDownDuration = 0.1f;

        [Group("Automatically Advance Dialogue")]
        public bool autoAdvance = false;

        [Group("Automatically Advance Dialogue")]
        [ShowIf(nameof(autoAdvance))]
        [Label("Delay before advancing")]
        public float autoAdvanceDelay = 1f;

        [Group("Typewriter")]
        public bool useTypewriterEffect = true;

        [Group("Typewriter")]
        [ShowIf(nameof(useTypewriterEffect))]
        [Label("Letters per second")]
        [Min(0)]
        public int typewriterEffectSpeed = 60;

        [Group("Typewriter")]
        [ShowIf(nameof(useTypewriterEffect))]
        [Label("Event Handler")]
        [SerializeField] private List<ActionMarkupHandler> eventHandlers = new List<ActionMarkupHandler>();

        private bool typewriterRunning = false;
        private CancellationTokenSource? localHurryCts = null;
        private bool continueClicked = false;

        // Unified input handler
        public void HandleAdvanceInput() {
            if (typewriterRunning) {
                localHurryCts?.Cancel();
            } else if (!continueClicked) {
                continueClicked = true;
            }
        }

        public override YarnTask OnDialogueCompleteAsync()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0;
            return YarnTask.CompletedTask;
        }

        public override YarnTask OnDialogueStartedAsync()
        {
            if (canvasGroup != null) canvasGroup.alpha = 0;
            return YarnTask.CompletedTask;
        }

        private void Awake()
        {
            if (useTypewriterEffect)
            {
                var pauser = new PauseEventProcessor();
                ActionMarkupHandlers.Insert(0, pauser);
            }

            if (characterNameContainer == null && characterNameText != null)
                characterNameContainer = characterNameText.gameObject;
        }

        private void Start()
        {
            ActionMarkupHandlers.AddRange(eventHandlers);
        }

        private void Update()
        {
            var spacePressed = Input.GetKeyDown(KeyCode.Space);
            var enterPressed = Input.GetKeyDown(KeyCode.Return);

#if ENABLE_INPUT_SYSTEM
            var ks = UnityEngine.InputSystem.Keyboard.current;
            if (ks != null)
            {
                if (!spacePressed) spacePressed = ks.spaceKey.wasPressedThisFrame;
                if (!enterPressed) enterPressed = ks.enterKey.wasPressedThisFrame || ks.numpadEnterKey.wasPressedThisFrame;
            }
#endif
            if (spacePressed || enterPressed)
            {
                HandleAdvanceInput();
            }
        }

        public void OnContinueClicked()
        {
            HandleAdvanceInput();
        }

        public override async YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            if (lineText == null)
            {
                Debug.LogError($"Line view does not have a text view. Skipping line {line.TextID} (\"{line.RawText}\")");
                return;
            }

            MarkupParseResult text;

            if (showCharacterNameInLineView)
            {
                if (characterNameText != null) characterNameText.text = line.CharacterName;
                text = line.TextWithoutCharacterName;

                if (line.Text.TryGetAttributeWithName("character", out var characterAttribute))
                    text.Attributes.Add(characterAttribute);
            }
            else
            {
                characterNameContainer?.SetActive(false);
                text = line.TextWithoutCharacterName;
            }

            lineText.text = text.Text;

            if (characterNameText != null)
            {
                var speaker = line.CharacterName ?? "";
                characterNameText.text = speaker;
                switch (speaker)
                {
                    case "Goblin":
                        characterNameText.color = lineText.color = Color.green;
                        break;
                    case "Kindly":
                        characterNameText.color = lineText.color = Color.yellow;
                        break;
                    case "Volition":
                        characterNameText.color = lineText.color = Color.magenta;
                        break;
                    default:
                        characterNameText.color = lineText.color = Color.white;
                        break;
                }
            }

            if (useTypewriterEffect)
            {
                lineText.maxVisibleCharacters = 0;
                foreach (var processor in ActionMarkupHandlers)
                    processor.OnPrepareForLine(text, lineText);
            }
            else
            {
                lineText.maxVisibleCharacters = text.Text.Length;
            }

            if (canvasGroup != null)
            {
                if (useFadeEffect) await Effects.FadeAlphaAsync(canvasGroup, 0, 1, fadeDownDuration, token.HurryUpToken);
                else canvasGroup.alpha = 1;
            }

            continueClicked = false;

            if (useTypewriterEffect)
            {
                localHurryCts = CancellationTokenSource.CreateLinkedTokenSource(token.HurryUpToken);
                typewriterRunning = true;

                var typewriter = new BasicTypewriter()
                {
                    ActionMarkupHandlers = this.ActionMarkupHandlers,
                    Text = this.lineText,
                    CharactersPerSecond = this.typewriterEffectSpeed,
                };

                try
                {
                    await typewriter.RunTypewriter(text, localHurryCts.Token);
                }
                finally
                {
                    typewriterRunning = false;
                    localHurryCts.Dispose();
                    localHurryCts = null;
                }
            }

            if (autoAdvance)
            {
                await YarnTask.Delay((int)(autoAdvanceDelay * 1000), token.NextLineToken).SuppressCancellationThrow();
            }
            else
            {
                await YarnTask.WaitUntil(() => continueClicked || token.NextLineToken.IsCancellationRequested, token.NextLineToken)
                    .SuppressCancellationThrow();
            }

            foreach (var processor in ActionMarkupHandlers)
                processor.OnLineWillDismiss();

            if (canvasGroup != null)
            {
                if (useFadeEffect) await Effects.FadeAlphaAsync(canvasGroup, 1, 0, fadeDownDuration, token.HurryUpToken).SuppressCancellationThrow();
                else canvasGroup.alpha = 0;
            }
        }

        public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            return YarnTask<DialogueOption?>.FromResult(null);
        }
    }
}

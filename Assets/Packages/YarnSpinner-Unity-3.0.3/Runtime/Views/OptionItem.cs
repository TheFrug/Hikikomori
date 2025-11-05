// OptionItem.cs
// Updated: disables Unity Selectable color tinting and enforces our own visuals
using UnityEngine;
using UnityEngine.EventSystems;
using Yarn.Unity.Attributes;

#if USE_TMP
using TMPro;
#else
    using TextMeshProUGUI = Yarn.Unity.TMPShim;
#endif

#nullable enable

namespace Yarn.Unity
{
    [System.Serializable]
    internal struct InternalAppearance
    {
        [SerializeField] internal Sprite sprite;
        [SerializeField] internal Color colour;
    }

    public sealed class OptionItem : UnityEngine.UI.Selectable, ISubmitHandler, IPointerClickHandler, IPointerEnterHandler
    {
        [MustNotBeNull, SerializeField] TextMeshProUGUI? text;
        [SerializeField] UnityEngine.UI.Image? selectionImage;

        [Group("Appearance"), SerializeField] InternalAppearance normal;
        [Group("Appearance"), SerializeField] InternalAppearance selected;
        [Group("Appearance"), SerializeField] InternalAppearance disabled;

        [Group("Appearance"), SerializeField] bool disabledStrikeThrough = true;

        // Set by options presenter
        public YarnTaskCompletionSource<DialogueOption?>? OnOptionSelected;
        public System.Threading.CancellationToken completionToken;

        private bool hasSubmittedOptionSelection = false;

        private DialogueOption? _option;
        public DialogueOption Option
        {
            get
            {
                if (_option == null)
                {
                    throw new System.NullReferenceException("Option has not been set on the option item");
                }
                return _option;
            }

            set
            {
                _option = value;

                hasSubmittedOptionSelection = false;

                // When we're given an Option, use its text and update our
                // interactibility.
                string line = value.Line.TextWithoutCharacterName.Text;
                if (disabledStrikeThrough && !value.IsAvailable)
                {
                    line = $"<s>{value.Line.TextWithoutCharacterName.Text}</s>";
                }

                if (text == null)
                {
                    Debug.LogWarning($"The {nameof(text)} is null, is it not connected in the inspector?", this);
                    return;
                }

                // assign text and interactability
                text.text = line;
                interactable = value.IsAvailable;

                // enforce visuals immediately (don't rely on Unity transitions)
                ApplyStyle(normal);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            // IMPORTANT: disable Unity's Selectable color tint transitions so
            // Unity doesn't modify Image/Text color on hover/press/etc.
            this.transition = Transition.None;

            // Ensure default visuals reflect inspector values (they may be changed by Prefab defaults)
            if (text != null)
            {
                text.color = normal.colour;
            }

            if (selectionImage != null)
            {
                selectionImage.color = normal.colour;
                if (normal.sprite != null)
                {
                    selectionImage.sprite = normal.sprite;
                    selectionImage.gameObject.SetActive(true);
                }
                else
                {
                    // if there's no sprite, we can keep selectionImage active or inactive
                    // but don't leave it unexpectedly hidden by other systems
                    selectionImage.gameObject.SetActive(false);
                }
            }
        }

        private void ApplyStyle(InternalAppearance style)
        {
            // Decide base colour/sprite depending on availability
            Color newColour = style.colour;
            Sprite newSprite = style.sprite;

            if (_option != null && !_option.IsAvailable)
            {
                newColour = disabled.colour;
                newSprite = disabled.sprite;
            }

            // Safety: ensure alpha component is preserved if inspector accidentally set 0
            // (This avoids accidental invisible text if the user forgot alpha)
            if (newColour.a <= 0f)
                newColour.a = 1f;

            if (text == null)
            {
                Debug.LogWarning($"The {nameof(text)} is null, is it not connected in the inspector?", this);
                return;
            }

            text.color = newColour;

            if (selectionImage != null)
            {
                // Apply the sprite & tint. If sprite is null we still set color and
                // optionally hide/show the image according to whether sprite is present.
                selectionImage.color = newColour;

                if (newSprite != null)
                {
                    selectionImage.sprite = newSprite;
                    if (!selectionImage.gameObject.activeSelf)
                        selectionImage.gameObject.SetActive(true);
                }
                else
                {
                    // If you prefer the empty selectionImage to remain active (e.g. colored rectangle),
                    // comment out the next line. By default we deactivate when there's no sprite.
                    if (selectionImage.gameObject.activeSelf)
                        selectionImage.gameObject.SetActive(false);
                }
            }
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);

            // Use the 'selected' style we own (Unity won't auto-tint because transition = None)
            ApplyStyle(selected);
        }

        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);

            // Revert to the normal style
            ApplyStyle(normal);
        }

        new public bool IsHighlighted
        {
            get
            {
                return EventSystem.current != null && EventSystem.current.currentSelectedGameObject == this.gameObject;
            }
        }

        // If we receive a submit or click event, invoke our "we just selected this option" handler.
        public void OnSubmit(BaseEventData eventData)
        {
            InvokeOptionSelected();
        }

        public void InvokeOptionSelected()
        {
            // ensure interactive state
            if (!IsInteractable())
            {
                return;
            }

            if (hasSubmittedOptionSelection == false && !completionToken.IsCancellationRequested)
            {
                hasSubmittedOptionSelection = true;
                OnOptionSelected?.TrySetResult(this.Option);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            InvokeOptionSelected();
        }

        // If we mouse-over, select this element so keyboard/controller focus follows mouse.
        public override void OnPointerEnter(PointerEventData eventData)
        {
            // Keep the Select call so navigation works; because transition == None,
            // Unity won't change colors for us.
            base.Select();
        }
    }
}

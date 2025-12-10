using System.Collections;
using TMPro;
using UnityEngine;

public class AdvancePromptController : MonoBehaviour
{
    [SerializeField] private CanvasGroup group = null!;
    [SerializeField] private TMP_Text promptText = null!;
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine fadeRoutine;

    // queued requests if Show/Hide is called while component isn't yet active & enabled
    private bool queuedShow = false;
    private string queuedMessage = string.Empty;
    private bool queuedImmediateShow = false;

    private bool queuedHide = false;
    private bool queuedImmediateHide = false;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            // keep GameObject inactive initially for cleanliness
            //gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // If a Show() call was queued while the object was inactive, perform it now.
        if (queuedShow)
        {
            queuedShow = false;
            Show(queuedMessage, queuedImmediateShow);
            // Show() will clear queuedHide if it starts.
        }
        else if (queuedHide)
        {
            // If Hide was queued earlier, perform it now
            queuedHide = false;
            Hide(queuedImmediateHide);
        }
    }

    /// <summary>
    /// Show the prompt. Safe to call even if this GameObject is currently inactive.
    /// </summary>
    public void Show(string message, bool immediate = false)
    {
        if (promptText != null) promptText.text = message ?? string.Empty;
        if (group == null) return;

        // Ensure GameObject is active before attempting coroutines.
        if (!gameObject.activeInHierarchy)
        {
            // Activate now — OnEnable will re-invoke Show if component wasn't enabled yet.
            gameObject.SetActive(true);

            // If component isn't active/enabled *this frame*, queue request and return.
            if (!isActiveAndEnabled)
            {
                queuedShow = true;
                queuedMessage = message ?? string.Empty;
                queuedImmediateShow = immediate;
                return;
            }
        }

        // At this point GameObject is active and component enabled (or Show was called when active).
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        // Clear any pending hide request, since show overrides hide
        queuedHide = false;

        if (immediate)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
            // ensure active (already active, but keep for clarity)
            gameObject.SetActive(true);
        }
        else
        {
            // Ensure the object is active before starting coroutine
            gameObject.SetActive(true);
            fadeRoutine = StartCoroutine(FadeTo(1f));
        }
    }

    /// <summary>
    /// Hide the prompt. Safe to call even if this GameObject is currently inactive.
    /// </summary>
    public void Hide(bool immediate = false)
    {
        if (group == null)
        {
            // If group missing, still try to disable safely
            if (gameObject.activeInHierarchy && isActiveAndEnabled)
                gameObject.SetActive(false);
            else
            {
                // queue a hide if not active yet
                queuedHide = true;
                queuedImmediateHide = immediate;
            }
            return;
        }

        // If object currently inactive, we can queue the hide so that when it becomes enabled it hides.
        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
        {
            queuedHide = true;
            queuedImmediateHide = immediate;
            // Also clear any queued show, since hide overrides
            queuedShow = false;
            return;
        }

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (immediate)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            // DO NOT SetActive(false) for immediate hide
            return;
        }
        else
        {
            fadeRoutine = StartCoroutine(FadeTo(0f));
        }
    }

    private IEnumerator FadeTo(float target)
    {
        float start = group.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        group.alpha = target;

        if (Mathf.Approximately(target, 0f))
        {
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
        }
        else
        {
            group.blocksRaycasts = true;
            group.interactable = true;
        }
    }
}

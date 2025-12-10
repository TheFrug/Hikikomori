using System.Collections;
using TMPro;
using UnityEngine;

public class AdvancePromptController : MonoBehaviour
{
    [SerializeField] private CanvasGroup group = null!;
    [SerializeField] private TMP_Text promptText = null!;
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
        }
    }

    public void Show(string message, bool immediate = false)
    {
        if (promptText != null) promptText.text = message ?? string.Empty;
        if (group == null) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (immediate)
        {
            group.alpha = 1f;
            group.blocksRaycasts = true;
            group.interactable = true;
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
            fadeRoutine = StartCoroutine(FadeTo(1f));
        }
    }

    public void Hide(bool immediate = false)
    {
        if (group == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        if (immediate)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
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

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlackoutOverlayController : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
        group.alpha = 0;
    }

    public void FadeIn()
    {
        gameObject.SetActive(true);
        group.blocksRaycasts = true;
        group.interactable = true;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(1f));
    }

    public void FadeOut()
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(0f));
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
            // finished fading out
            group.blocksRaycasts = false;
            group.interactable = false;
            gameObject.SetActive(false);
        }
    }
}
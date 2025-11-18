using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpoonPanel : MonoBehaviour
{
    public static SpoonPanel ActivePanel;   // GLOBAL ACCESS

    [Header("Config")]
    public BehaviorData behaviorData;
    public BehaviorManager behaviorManager;
    public RectTransform slotContainer;
    public GameObject slotPrefab;
    public Button cancelButton;
    public Slider progressBar;
    public float oneShotBaseSeconds = 0.6f;

    [HideInInspector] public List<SpoonSlot> slots = new List<SpoonSlot>();

    private int requiredSpoons = 0;
    private bool behaviorTriggered = false;

    void OnEnable()
    {
        ActivePanel = this;
    }

    void OnDisable()
    {
        if (ActivePanel == this)
            ActivePanel = null;
    }

    void Start()
    {
        if (behaviorData != null && behaviorManager != null)
            Setup(behaviorData, behaviorManager);
    }

    public void Setup(BehaviorData data, BehaviorManager mgr)
    {
        behaviorData = data;
        behaviorManager = mgr;

        requiredSpoons = Mathf.Max(0, data.spoonsCost);
        behaviorTriggered = false;

        foreach (Transform t in slotContainer)
            Destroy(t.gameObject);

        slots.Clear();

        // Only ONE slot
        var go = Instantiate(slotPrefab, slotContainer);
        var slot = go.GetComponent<SpoonSlot>();
        slot.Initialize(this);
        slots.Add(slot);

        cancelButton.onClick.RemoveAllListeners();
        cancelButton.onClick.AddListener(CancelPanel);

        if (progressBar != null)
            progressBar.gameObject.SetActive(false);
    }

    public int CurrentFilledSpoons()
    {
        int count = 0;
        foreach (var s in slots)
            count += s.spoonCount;

        return count;
    }

    public void OnSlotChanged()
    {
        if (behaviorTriggered)
            return;

        int filled = CurrentFilledSpoons();

        if (filled >= requiredSpoons)
        {
            behaviorTriggered = true;
            StartBehaviorRun();
        }
    }

    private void StartBehaviorRun()
    {
        if (behaviorManager == null)
        {
            Debug.LogError("SpoonPanel: BehaviorManager missing!");
            return;
        }

        bool isScene = behaviorData.isScene ||
                       (behaviorData.thought != null &&
                        behaviorData.thought.type == Thought.ThoughtType.Interactive);

        if (isScene)
        {
            behaviorManager.BeginSceneBehavior(behaviorData, this);
        }
        else
        {
            StartCoroutine(RunOneShot());
        }
    }

    private System.Collections.IEnumerator RunOneShot()
    {
        float seconds = oneShotBaseSeconds;

        if (behaviorData.durationMinutes > 0)
            seconds = Mathf.Max(0.2f, oneShotBaseSeconds * (behaviorData.durationMinutes / 30f));

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            if (progressBar != null)
                progressBar.value = Mathf.Clamp01(elapsed / seconds);

            yield return null;
        }

        behaviorManager.BeginOneShotBehavior(behaviorData, this);
    }

    public void CancelPanel()
    {
        foreach (var s in slots)
            s.ForceReturnSpoon();

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        foreach (var s in slots)
            s.ForceReturnSpoon();
    }
}

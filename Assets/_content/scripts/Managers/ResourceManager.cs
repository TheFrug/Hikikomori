// ResourceManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using UnityEngine.Events;

[System.Serializable]
public class HopeLevelData
{
    [Tooltip("How many hope points required to reach this level from previous.")]
    public int hopeLevelUpThreshold = 5;

    [Tooltip("Range (inclusive) for max spoons to set at this level")]
    public Vector2Int spoonRange = new Vector2Int(3, 5);

    [Tooltip("Optional: behavior id/key to unlock on this level (game should implement unlocking).")]
    public string unlockBehavior = "";

    [Tooltip("Optional: ThoughtData to spawn when this level is achieved.")]
    public ThoughtData thoughtToSpawn = null;

    [Tooltip("Optional UnityEvent to run designer-defined effects in inspector.")]
    public UnityEvent onLevelUp;
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Stress")]
    public Slider stressBar;
    public TMP_Text stressText;
    public int maxStress = 100;
    [SerializeField]
    private int currentStress = 0;

    [Header("Hope")]
    public Slider hopeBar;
    public TMP_Text hopeText;
    public TMP_Text hopeLevelText;

    [Header("Hope Levels (inspector)")]
    public List<HopeLevelData> hopeLevels = new List<HopeLevelData>();

    // runtime hope values
    [SerializeField]
    private int currentHope = 0;
    private int hopeLevel = 0;

    [Header("Spoons")]
    public SpoonDrawer spoonDrawer;
    public int maxSpoons = 4;
    public int currentSpoons;

    [Header("Shutdown / Doomscroll settings")]
    public bool isShutdownMode = false;
    public float doomscrollTickSeconds = 1.0f;
    public int doomscrollStressRecoveryPerTick = 1;
    public int doomscrollHopeDrainPerTick = 1;

    private bool uiInitialized = false;
    private bool suppressDrawerRefresh = false;

    // animation coroutines
    private Coroutine stressAnimRoutine;
    private Coroutine hopeAnimRoutine;

    public static event System.Action<int> OnHopeLevelUp;
    public static event System.Action<int, HopeLevelData> OnHopeLevelUpUI;

    // visual config
    [Header("Animation")]
    [Tooltip("Duration in seconds for stress changes")]
    public float stressAnimDuration = 0.7f;
    [Tooltip("Duration in seconds for hope changes per segment")]
    public float hopeSegmentDuration = 0.6f;
    [Tooltip("Duration of the hope level-up pulse effect")]
    public float levelUpPulseDuration = 0.45f;
    [Tooltip("Pulse scale multiplier for level-up text")]
    public float levelUpPulseScale = 1.35f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        currentStress = 0;
        currentHope = 0;
        hopeLevel = 0;

        SetupBars();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeSceneUI());
    }

    private IEnumerator InitializeSceneUI()
    {
        yield return null; // wait a frame for UI objects to exist

        // Rebind any missing UI references
        if (stressBar == null || hopeBar == null)
        {
            var sliders = FindObjectsOfType<Slider>();
            foreach (var s in sliders)
            {
                var name = s.name.ToLower();
                if (name.Contains("stress") && stressBar == null) stressBar = s;
                else if (name.Contains("hope") && hopeBar == null) hopeBar = s;
            }
        }

        var texts = FindObjectsOfType<TMP_Text>();
        foreach (var t in texts)
        {
            var n = t.name.ToLower();
            if (n.Contains("stress") && stressText == null) stressText = t;
            else if (n.Contains("hope") && hopeText == null) hopeText = t;
        }

        if (spoonDrawer == null)
            spoonDrawer = FindObjectOfType<SpoonDrawer>();

        if (spoonDrawer == null)
        {
            Debug.LogWarning("ResourceManager: No SpoonDrawer found in this scene.");
        }

        uiInitialized = true;

        LoadDailySpoons();
        UpdateUI();
    }

    private void Update()
    {
        DebugControls();
        // Check stress threshold (simple logging + state)
        //CheckStressThreshold();
    }

    // --- Setup & Update UI ---
    void SetupBars()
    {
        if (stressBar != null)
        {
            stressBar.maxValue = maxStress;
            stressBar.value = currentStress;
        }

        if (hopeBar != null)
        {
            hopeBar.maxValue = GetCurrentHopeThreshold();
            hopeBar.value = Mathf.Clamp(currentHope, 0, GetCurrentHopeThreshold());
        }
    }

    void UpdateUI()
    {
        if (!uiInitialized) return;

        if (stressBar != null)
        {
            // slider value may be driven by animation coroutine; only set text here
            if (stressText) stressText.text = $"{currentStress}/{maxStress}";
        }

        if (hopeBar != null)
        {
            // hopeBar.value usually handled by animation coroutine; ensure max matches current threshold
            hopeBar.maxValue = GetCurrentHopeThreshold();
            if (hopeText) hopeText.text = $"{Mathf.Clamp(currentHope, 0, GetCurrentHopeThreshold())}/{GetCurrentHopeThreshold()}";
            if (hopeLevelText) hopeLevelText.text = $"{hopeLevel}";
        }

        // Stress fill color - new simplified ranges
        if (stressBar?.fillRect != null)
        {
            Image stressFill = stressBar.fillRect.GetComponent<Image>();
            if (stressFill)
            {
                float pct = (float)currentStress / maxStress;
                // 0-0.33: yellow, 0.34-0.66: orange, 0.67-0.89: red, 0.90-1.0: dark red
                if (pct >= 0.90f) stressFill.color = new Color(0.5f, 0f, 0f); // dark red
                else if (pct >= 0.67f) stressFill.color = Color.red;
                else if (pct >= 0.34f) stressFill.color = new Color(1f, 0.55f, 0f); // orange
                else stressFill.color = Color.yellow;
            }
        }
    }

    // --- Public API additions for BehaviorManager compatibility ---
    public bool HasEnoughSpoons(int cost)
    {
        return currentSpoons >= Mathf.Max(0, cost);
    }

    public int CurrentStress => currentStress;
    public int MaxStress => maxStress;

    // ModifyResources called by BehaviorManager
    public void ModifyResources(BehaviorData data)
    {
        if (data == null) return;

        Debug.Log($"ModifyResources called: spoonsCost={data.spoonsCost}, stressImpact={data.stressImpact}, hopeImpact={data.hopeImpact}");

        if (data.spoonsCost != 0)
            ModifySpoons(-data.spoonsCost);

        if (data.stressImpact != 0)
            ModifyStress(data.stressImpact);

        if (data.hopeImpact != 0)
            ModifyHope(data.hopeImpact);

        UpdateUI();
    }

    public void ApplyPendingDialogueChanges()
    {
        // placeholder
    }

    public void UpdateAllUI()
    {
        UpdateUI();
        if (uiInitialized && spoonDrawer != null)
            spoonDrawer.RefreshDrawer(currentSpoons);
    }

    // --- Core Logic / Public APIs ---
    // SPOONS
    public void ModifySpoons(int delta)
    {
        int previous = currentSpoons;
        currentSpoons = Mathf.Clamp(currentSpoons + delta, 0, maxSpoons);

        if (!suppressDrawerRefresh && uiInitialized && spoonDrawer != null && previous != currentSpoons)
            spoonDrawer.RefreshDrawer(currentSpoons);

        UpdateUI();
    }

    // STRESS
    public void ModifyStress(int delta)
    {
        if (isShutdownMode) return;

        int old = currentStress;
        currentStress = Mathf.Clamp(currentStress + delta, 0, maxStress);

        // stop existing stress anim
        if (stressAnimRoutine != null)
            StopCoroutine(stressAnimRoutine);

        if (stressBar != null)
            stressAnimRoutine = StartCoroutine(AnimateNumericSlider(stressBar, stressText, old, currentStress, maxStress, stressAnimDuration));
        else
            UpdateUI();

        if (currentStress >= maxStress && !isShutdownMode)
            StartShutdownMode();
    }

    // HOPE
    public void ModifyHope(int deltaXP)
    {
        if (hopeLevels == null || hopeLevels.Count == 0)
        {
            // fallback: just increment currentHope and animate a simple slider
            int old = currentHope;
            currentHope = Mathf.Clamp(currentHope + deltaXP, 0, 999);
            if (hopeAnimRoutine != null) StopCoroutine(hopeAnimRoutine);
            if (hopeBar != null)
                hopeAnimRoutine = StartCoroutine(AnimateNumericSlider(hopeBar, hopeText, old, currentHope, Mathf.Max(1, Mathf.RoundToInt(hopeBar.maxValue)), hopeSegmentDuration));
            else
                UpdateUI();
            return;
        }

        // Use cumulative totals to avoid mismatch between per-level and total representations.
        int oldTotal = GetCumulativeThresholdBefore(hopeLevel) + currentHope;
        int newTotal = Mathf.Max(0, oldTotal + deltaXP);

        // stop any running hope animation and start a fresh one
        if (hopeAnimRoutine != null) StopCoroutine(hopeAnimRoutine);
        hopeAnimRoutine = StartCoroutine(AnimateHopeTo(oldTotal, newTotal));
    }

    // Helper: sum thresholds for all levels strictly before `levelIndex`
    // returns 0 if levelIndex <= 0
    private int GetCumulativeThresholdBefore(int levelIndex)
    {
        int sum = 0;
        if (hopeLevels == null) return 0;
        int clamped = Mathf.Clamp(levelIndex, 0, hopeLevels.Count);
        for (int i = 0; i < clamped; i++)
            sum += hopeLevels[i].hopeLevelUpThreshold;
        return sum;
    }

    private int GetLevelForTotal(int total)
    {
        if (hopeLevels == null || hopeLevels.Count == 0) return 0;
        int cum = 0;
        for (int i = 0; i < hopeLevels.Count; i++)
        {
            int thr = hopeLevels[i].hopeLevelUpThreshold;
            if (total < cum + thr)
                return i;
            cum += thr;
        }
        // beyond configured levels -> return last level index (we'll treat its threshold as large)
        return Mathf.Max(0, hopeLevels.Count - 1);
    }

    // New robust animator (replaces previous AnimateHopeTo)
    private IEnumerator AnimateHopeTo(int startTotal, int targetTotal)
    {
        // clamp
        targetTotal = Mathf.Max(startTotal, targetTotal);

        int processed = Mathf.Max(0, startTotal);

        while (processed < targetTotal)
        {
            // determine which level the current processed total lives in
            int levelIndex = GetLevelForTotal(processed);
            int cumBefore = GetCumulativeThresholdBefore(levelIndex);
            int threshold = (levelIndex < hopeLevels.Count) ? hopeLevels[levelIndex].hopeLevelUpThreshold : int.MaxValue;

            // compute progress inside this level
            int progressInLevel = Mathf.Clamp(processed - cumBefore, 0, threshold);

            // compute target absolute for this segment (either fill to this level's threshold or go to final target)
            int absoluteSegEnd = Mathf.Min(targetTotal, cumBefore + threshold);
            int segTargetInLevel = Mathf.Clamp(absoluteSegEnd - cumBefore, 0, threshold);

            // prepare animator to go from progressInLevel -> segTargetInLevel
            float elapsed = 0f;
            float duration = hopeSegmentDuration;
            float startVal = progressInLevel;
            float endVal = segTargetInLevel;

            // ensure slider max corresponds to this threshold
            if (hopeBar != null) hopeBar.maxValue = Mathf.Max(1, threshold);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(startVal, endVal, t);

                if (hopeBar != null) hopeBar.value = eased;
                if (hopeText != null) hopeText.text = $"{Mathf.RoundToInt(eased)}/{threshold}";

                yield return null;
            }

            // finalize this segment
            processed = cumBefore + segTargetInLevel;
            if (hopeBar != null) hopeBar.value = segTargetInLevel;
            if (hopeText != null) hopeText.text = $"{segTargetInLevel}/{threshold}";

            // If we filled this level (segTarget == threshold) and there's still more to process, commit the level-up now.
            if (segTargetInLevel >= threshold && targetTotal >= cumBefore + threshold)
            {
                // apply level up effects for 'levelIndex'
                var levelData = hopeLevels[levelIndex];

                // increment authoritative level
                hopeLevel = Mathf.Clamp(levelIndex + 1, 0, hopeLevels.Count); // clamp for safety

                // update max spoons and drawer
                maxSpoons = Random.Range(levelData.spoonRange.x, levelData.spoonRange.y + 1);
                if (uiInitialized && spoonDrawer != null)
                    spoonDrawer.RefreshDrawer(currentSpoons);

                levelData.onLevelUp?.Invoke();

                if (!string.IsNullOrEmpty(levelData.unlockBehavior))
                    Debug.Log($"Unlocked behavior: {levelData.unlockBehavior}");

                if (levelData.thoughtToSpawn != null)
                    ThoughtBubbleManager_New.Instance?.StartThought(levelData.thoughtToSpawn);

                OnHopeLevelUp?.Invoke(hopeLevel);
                OnHopeLevelUpUI?.Invoke(hopeLevel, levelData);

                // Visual: reset bar to 0 for next level
                int nextThreshold = GetCurrentHopeThreshold();
                if (hopeBar != null)
                {
                    hopeBar.maxValue = Mathf.Max(1, nextThreshold);
                    hopeBar.value = 0f;
                }
                if (hopeText != null) hopeText.text = $"0/{nextThreshold}";
                if (hopeLevelText != null) StartCoroutine(PulseLevelText());

                Debug.Log($"Hope leveled up to {hopeLevel}");

                // small yield so the reset takes effect visually before continuing
                yield return null;

                // continue loop — processed already reflects absolute points; we'll animate the remainder in the next iteration
                continue;
            }

            // otherwise we've animated to final target inside this level and will exit loop
        }

        // finished; compute final per-level currentHope and ensure authoritative state matches
        int finalCumBefore = GetCumulativeThresholdBefore(hopeLevel);
        currentHope = Mathf.Clamp(processed - finalCumBefore, 0, GetCurrentHopeThreshold());

        if (hopeBar != null)
        {
            hopeBar.maxValue = Mathf.Max(1, GetCurrentHopeThreshold());
            hopeBar.value = currentHope;
        }
        if (hopeText != null)
            hopeText.text = $"{currentHope}/{GetCurrentHopeThreshold()}";
        if (hopeLevelText != null)
            hopeLevelText.text = $"{hopeLevel}";

        UpdateUI();

        hopeAnimRoutine = null;
        yield break;
    }


    // Pulse the level text scale for feedback
    private IEnumerator PulseLevelText()
    {
        if (hopeLevelText == null) yield break;

        var rt = hopeLevelText.rectTransform;
        Vector3 original = rt.localScale;
        Vector3 target = original * levelUpPulseScale;

        float half = levelUpPulseDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float v = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / half));
            rt.localScale = Vector3.Lerp(original, target, v);
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float v = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / half));
            rt.localScale = Vector3.Lerp(target, original, v);
            yield return null;
        }

        rt.localScale = original;
    }

    private int GetCurrentHopeThreshold()
    {
        if (hopeLevels != null && hopeLevels.Count > hopeLevel)
            return Mathf.Max(1, hopeLevels[hopeLevel].hopeLevelUpThreshold);
        // fallback default
        return Mathf.Max(1, 5);
    }

    // generic numeric slider animator (used for stress; simple float tween)
    private IEnumerator AnimateNumericSlider(Slider slider, TMP_Text text, int startValue, int endValue, int maxValue, float duration)
    {
        float elapsed = 0f;
        float s = startValue;
        float e = endValue;

        // Ensure slider max matches
        if (slider != null) slider.maxValue = maxValue;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float v = Mathf.SmoothStep(s, e, t);
            if (slider != null) slider.value = v;
            if (text != null) text.text = $"{Mathf.RoundToInt(v)}/{maxValue}";
            yield return null;
        }

        if (slider != null) slider.value = endValue;
        if (text != null) text.text = $"{endValue}/{maxValue}";
    }

    // DAILY SLOTS
    public void LoadDailySpoons()
    {
        int baseSpoons = Mathf.RoundToInt(maxSpoons);
        int randomVariance = Random.Range(-3, 2);
        //currentSpoons = Mathf.Clamp(baseSpoons + randomVariance, 1, maxSpoons);
        currentSpoons = maxSpoons;

        if (uiInitialized && spoonDrawer != null)
            spoonDrawer.RefreshDrawer(currentSpoons);

        UpdateUI();
    }

    // --- Shutdown / Doomscroll Mode ---
    private void StartShutdownMode()
    {
        if (isShutdownMode) return;
        isShutdownMode = true;
        Debug.Log("Entering Shutdown/Doomscroll mode due to max stress.");
        StartCoroutine(DoomscrollCoroutine());
    }

    private IEnumerator DoomscrollCoroutine()
    {
        while (currentStress > 0)
        {
            yield return new WaitForSeconds(doomscrollTickSeconds);

            currentStress = Mathf.Max(0, currentStress - doomscrollStressRecoveryPerTick);
            currentHope = Mathf.Max(0, currentHope - doomscrollHopeDrainPerTick);

            UpdateUI();
        }

        isShutdownMode = false;
        Debug.Log("Shutdown/Doomscroll ended; stress is zero.");
        UpdateUI();
    }

    public void BeginCommit()
    {
        suppressDrawerRefresh = true;
    }

    public void EndCommit()
    {
        suppressDrawerRefresh = false;
    }

    // --- Debug Keys ---
    void DebugControls()
    {
        // keep your earlier test mapping: 3/-3 and 4/+4
        if (Input.GetKeyDown(KeyCode.Alpha3)) ModifyStress(-10);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ModifyStress(10);

        if (Input.GetKeyDown(KeyCode.Alpha3)) ModifyHope(-1);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ModifyHope(1);

        if (Input.GetKeyDown(KeyCode.Alpha5)) ModifySpoons(-1);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ModifySpoons(1);
    }

    // --- Helpers ---
    public int GetCurrentSpoons() => currentSpoons;
    public int GetCurrentStress() => currentStress;
    public int GetCurrentHope() => currentHope;
    public int GetHopeLevel() => hopeLevel;
}

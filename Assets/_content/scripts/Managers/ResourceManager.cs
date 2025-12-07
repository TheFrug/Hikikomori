using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Yarn.Unity;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Stress")]
    public Slider stressBar;
    public TMP_Text stressText;
    public int maxStress = 100;
    private int currentStress;
    [Tooltip("Soft threshold where you might show UI warnings, independent of maxStress.")]
    private int stressThreshold = 40;
    private bool stressedOut = false;

    [Header("Hope")]
    public Slider hopeBar;
    public TMP_Text hopeText;
    public TMP_Text hopeLevelText;
    private int hopeLevelUpThreshold = 5;
    private int currentHope;
    private int hopeLevel;

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

    private Coroutine stressAnimRoutine;
    private Coroutine hopeAnimRoutine;

    public static event System.Action<int> OnHopeLevelUp;
    [Header("Hope Levels")]
    public List<HopeLevelData> hopeLevels = new List<HopeLevelData>();

    // --- Lifecycle ---
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
                if (name.Contains("stress")) stressBar = s;
                else if (name.Contains("hope")) hopeBar = s;
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
        CheckStressThreshold();
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
            hopeBar.maxValue = hopeLevelUpThreshold;
            hopeBar.value = currentHope;
        }
    }

    void UpdateUI()
    {
        if (!uiInitialized) return;

        if (stressBar != null)
        {
            stressBar.value = currentStress;
            if (stressText) stressText.text = $"{currentStress}/{maxStress}";
        }

        if (hopeBar != null)
        {
            hopeBar.maxValue = hopeLevelUpThreshold;
            hopeBar.value = currentHope;
            if (hopeText) hopeText.text = $"{currentHope}/{hopeLevelUpThreshold}";
            if (hopeLevelText) hopeLevelText.text = $"{hopeLevel}";
        }

        if (stressBar?.fillRect != null)
        {
            Image stressFill = stressBar.fillRect.GetComponent<Image>();
            if (stressFill)
            {
                float stressPct = (float)currentStress / maxStress;
                if (stressPct >= 1f) stressFill.color = Color.black;
                else if (stressPct >= 0.8f) stressFill.color = Color.red;
                else if (stressPct >= 0.6f) stressFill.color = new Color(1f, 0.5f, 0f);
                else if (stressPct >= 0.4f) stressFill.color = Color.yellow;
                else stressFill.color = Color.green;
            }
        }
    }

    // --- Public API additions for BehaviorManager compatibility ---

    // Return whether there are enough spoons to pay this cost
    public bool HasEnoughSpoons(int cost)
    {
        return currentSpoons >= Mathf.Max(0, cost);
    }

    // Expose current stress and max stress in the exact names expected
    public int CurrentStress => currentStress;
    public int MaxStress => maxStress;

    // Apply behavior's base resource changes (safely handles null)
    public void ModifyResources(BehaviorData data)
    {
        Debug.Log($"ModifyResources called: spoonsCost={data.spoonsCost}, stressImpact={data.stressImpact}, hopeImpact={data.hopeImpact}");
        if (data == null) return;

        // Consume spoons (cost is positive in data, we subtract)
        if (data.spoonsCost != 0)
            ModifySpoons(-data.spoonsCost);

        // Apply stress/hope deltas (assumed fields; change names if your BehaviorData differs)
        if (data.stressImpact != 0)
            ModifyStress(data.stressImpact);

        if (data.hopeImpact != 0)
            ModifyHope(data.hopeImpact);

        UpdateUI();
    }

    // Called by BehaviorManager after interactive dialogues finish; placeholder for Yarn-driven changes
    public void ApplyPendingDialogueChanges()
    {
        // If you collect dialogue-driven resource changes in a queue, process them here.
        // For now this is a safe no-op (keeps BehaviorManager happy).
    }

    // BehaviorManager expects this to trigger UI refresh hooks
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

        // Only refresh drawer when the canonical count actually changed and refresh is not suppressed.
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

        // Stop any existing animation
        if (stressAnimRoutine != null)
            StopCoroutine(stressAnimRoutine);

        if (stressBar != null)
        {
            stressAnimRoutine = StartCoroutine(
                AnimateSlider(stressBar, stressText, old, currentStress, maxStress)
            );
        }
        else
        {
            UpdateUI();
        }

        if (currentStress >= maxStress && !isShutdownMode)
            StartShutdownMode();
    }

    // HOPE
    public void ModifyHope(int deltaXP)
    {
        int old = currentHope;
        currentHope += deltaXP; // DO NOT clamp here!

        if (hopeAnimRoutine != null)
            StopCoroutine(hopeAnimRoutine);

        if (hopeBar != null)
        {
            hopeAnimRoutine = StartCoroutine(
                AnimateSlider(hopeBar, hopeText, old, currentHope, hopeLevels[Mathf.Min(hopeLevel, hopeLevels.Count - 1)].hopeLevelUpThreshold)
            );
        }
        else
        {
            UpdateUI();
        }

        CheckHopeThreshold();
    }

    public void CheckHopeThreshold()
    {
        // Only proceed if we have another level
        while (hopeLevels.Count > hopeLevel && currentHope >= hopeLevels[hopeLevel].hopeLevelUpThreshold)
        {
            var levelData = hopeLevels[hopeLevel];

            // Subtract threshold from currentHope (handles overflow)
            currentHope -= levelData.hopeLevelUpThreshold;

            // Update maxSpoons based on this level
            maxSpoons = Random.Range(levelData.spoonRange.x, levelData.spoonRange.y + 1);
            if (uiInitialized && spoonDrawer != null)
                spoonDrawer.RefreshDrawer(currentSpoons);

            // Trigger any inspector UnityEvent
            levelData.onLevelUp?.Invoke();

            // Unlock behavior if set
            if (!string.IsNullOrEmpty(levelData.unlockBehavior))
            {
                Debug.Log($"Unlocked behavior: {levelData.unlockBehavior}");
            }

            // Spawn thought if set
            if (levelData.thoughtToSpawn != null)
            {
                ThoughtBubbleManager_New.Instance?.StartThought(levelData.thoughtToSpawn);
            }

            hopeLevel++;
            OnHopeLevelUp?.Invoke(hopeLevel);

            Debug.Log($"Hope leveled up to {hopeLevel}");
        }

        UpdateUI();
    }


    public int GetCurrentSpoons() => currentSpoons;

    // DAILY SLOTS
    public void LoadDailySpoons()
    {
        int baseSpoons = Mathf.RoundToInt(maxSpoons);
        int randomVariance = Random.Range(-3, 2);
        currentSpoons = Mathf.Clamp(baseSpoons + randomVariance, 1, maxSpoons);

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

    // Simple threshold logging
    void CheckStressThreshold()
    {
        if ((currentStress >= stressThreshold) && (!stressedOut))
        {
            Debug.Log($"Hiki is stressed: {currentStress}/{maxStress} >= threshold {stressThreshold}.");
            stressedOut = true;
        }

        if (stressedOut && (currentStress < stressThreshold))
        {
            Debug.Log("Hiki is no longer in the stress warning region.");
            stressedOut = false;
        }
    }

    private IEnumerator AnimateSlider(
    Slider slider,
    TMP_Text text,
    int startValue,
    int endValue,
    int maxValue,
    float duration = 2.0f)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // SmoothStep = fast start, slows at end
            float v = Mathf.SmoothStep(startValue, endValue, t);
            int iv = Mathf.RoundToInt(v);

            slider.value = iv;
            if (text != null)
                text.text = $"{iv}/{maxValue}";

            yield return null;
        }

        // Snap final values
        slider.value = endValue;
        if (text != null)
            text.text = $"{endValue}/{maxValue}";
    }

    public void BeginCommit()
    {
        suppressDrawerRefresh = true;
    }

    public void EndCommit()
    {
        suppressDrawerRefresh = false;
        // No automatic refresh — the drawer state after dragging is the final state.
    }

    // --- Debug Keys ---
    void DebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3)) ModifyStress(-10);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ModifyStress(10);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ModifyHope(-1);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ModifyHope(1);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ModifySpoons(-1);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ModifySpoons(1);
    }

    // --- Optional helpers for other scripts ---
    public int GetCurrentStress() => currentStress;
    public int GetCurrentHope() => currentHope;
    public int GetHopeLevel() => hopeLevel;
}

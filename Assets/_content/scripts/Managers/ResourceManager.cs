using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Hunger")]
    public Slider hungerBar;
    public TMP_Text hungerText;
    public int maxHunger = 100;
    private int currentHunger;

    [Header("Stress")]
    public Slider stressBar;
    public TMP_Text stressText;
    public int maxStress = 100;
    private int currentStress;
    private int stressThreshold = 40;
    private bool stressedOut = false;

    [Header("Spoons")]
    public SpoonDrawer spoonDrawer;
    public int maxSpoons = 10;
    public int currentSpoons;

    [Header("Cash")]
    public int cash = 20;
    public int cashNeededForRent = 100;

    private bool uiInitialized = false;

    // --- Lifecycle ---
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        currentHunger = 70;
        currentStress = 0;
        SetupBars();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(InitializeSceneUI());
    }

    private IEnumerator InitializeSceneUI()
    {
        yield return null; // Wait one frame to allow UI to fully load

        // Rebind any missing UI references
        if (hungerBar == null || stressBar == null)
        {
            var sliders = FindObjectsOfType<Slider>();
            foreach (var s in sliders)
            {
                if (s.name.ToLower().Contains("hunger")) hungerBar = s;
                else if (s.name.ToLower().Contains("stress")) stressBar = s;
            }
        }

        if (spoonDrawer == null)
            spoonDrawer = FindObjectOfType<SpoonDrawer>();

        if (spoonDrawer == null)
        {
            Debug.LogWarning("No SpoonDrawer found in this scene.");
            yield break;
        }

        uiInitialized = true;

        LoadDailySpoons();
        UpdateUI();
    }

    private void Update()
    {
        DebugControls();
        checkStress();
    }

    // --- Setup & Update UI ---
    void SetupBars()
    {
        if (hungerBar != null)
        {
            hungerBar.maxValue = maxHunger;
            hungerBar.value = currentHunger;
        }

        if (stressBar != null)
        {
            stressBar.maxValue = maxStress;
            stressBar.value = currentStress;
        }
    }

    void UpdateUI()
    {
        if (!uiInitialized) return;

        if (hungerBar != null)
        {
            hungerBar.value = currentHunger;
            if (hungerText) hungerText.text = $"{currentHunger}/{maxHunger}";
        }

        if (stressBar != null)
        {
            stressBar.value = currentStress;
            if (stressText) stressText.text = $"{currentStress}/{maxStress}";
        }

        // Stress threshold tinting
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

    // --- Core Logic ---
    public void LoadDailySpoons()
    {
        float hungerPercent = (float)currentHunger / maxHunger;
        int baseSpoons = Mathf.RoundToInt(hungerPercent * maxSpoons);
        int randomVariance = Random.Range(-3, 2);
        currentSpoons = Mathf.Clamp(baseSpoons + randomVariance, 1, maxSpoons);

        Debug.Log($"Daily spoons set to {currentSpoons} (Hunger: {currentHunger}/{maxHunger})");

        // Only refresh if UI for this scene exists
        if (uiInitialized && spoonDrawer != null)
            spoonDrawer.RefreshDrawer(currentSpoons);
    }

    public int GetCurrentSpoons()
    {
        return currentSpoons;
    }


    public void ModifyResources(float spoonDelta, float hungerDelta, float cashDelta)
    {
        if (spoonDelta != 0)
        {
            currentSpoons = Mathf.RoundToInt(currentSpoons - spoonDelta);
            currentSpoons = Mathf.Clamp(currentSpoons, 0, maxSpoons);
        }

        if (hungerDelta != 0)
        {
            currentHunger = Mathf.RoundToInt(currentHunger - hungerDelta);
            currentHunger = Mathf.Clamp(currentHunger, 0, maxHunger);
        }

        if (cashDelta != 0)
            cash -= Mathf.RoundToInt(cashDelta);

        UpdateUI();
    }

    void checkStress()
    {
        if ((currentStress >= stressThreshold) && (!stressedOut))
        {
            Debug.Log($"Hiki is stressed out! ({currentStress}/{maxStress}) exceeds threshold {stressThreshold}.");
            stressedOut = true;
        }

        if (stressedOut && (currentStress < stressThreshold))
        {
            Debug.Log("Hiki is no longer stressed out.");
            stressedOut = false;
        }
    }

    // --- Debug Keys ---
    void DebugControls()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) currentHunger = Mathf.Max(0, currentHunger - 10);
        if (Input.GetKeyDown(KeyCode.Alpha2)) currentHunger = Mathf.Min(maxHunger, currentHunger + 10);
        if (Input.GetKeyDown(KeyCode.Alpha3)) currentStress = Mathf.Max(0, currentStress - 10);
        if (Input.GetKeyDown(KeyCode.Alpha4)) currentStress = Mathf.Min(maxStress, currentStress + 10);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ModifyResources(1, 0, 0);
        if (Input.GetKeyDown(KeyCode.Alpha6)) ModifyResources(-1, 0, 0);
    }
}

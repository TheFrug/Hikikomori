using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Unity;
using TMPro;

public class FamilyManager : MonoBehaviour
{
    public static FamilyManager Instance { get; private set; }

    [System.Serializable]
    public class PartInfo
    {
        public string key;
        public string realName;
        public string partType;
        public string partDescription;
        public bool nameRevealed;
        public float bond;

        [Header("Visuals")]
        public Color baseColor = Color.gray;
        public Color textColor = Color.gray;
        public TMP_FontAsset font;
        public AnimationCurve bondToSaturation = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 0-1 curve mapping

        [Header("Bond mapping")]
        [Tooltip("Maximum bond value used to normalize 'bond' into 0..1 for colour saturation. Adjust to your game's scale.")]
        public float bondMax = 10f;
    }

    [Header("Global Bond Thresholds")]
    public float bondToStartColor = 20f;
    public float bondToRevealFont = 35f;
    public float bondToRevealName = 50f;
    public TMP_FontAsset defaultFont; // shared font when not revealed
    public Color defaultTextColor;

    private Color unknownSpeakerColor = Color.gray;

    [Header("parts")]
    public List<PartInfo> parts = new List<PartInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject);
    }

    // --- Core Logic ---
    public Color GetBubbleColor(string key, bool useDisplayKey = false)
    {
        if (useDisplayKey && key == "???")
        {
            Debug.LogWarning("[FamilyManager] GetBubbleColor received '???' (useDisplayKey). Returning neutral color.");
            return unknownSpeakerColor;
        }

        // If caller passed the ambiguous display name "???", don't try to match parts by display name.
        if (key == "???")
        {
            Debug.Log("[FamilyManager] GetBubbleColor: received ambiguous display '???'. Returning neutral color.");
            return unknownSpeakerColor;
        }

        // existing resolution: try key, then realName (but NOT GetDisplayName fallback)
        var part = parts.Find(p => p.key == key);
        if (part == null) part = parts.Find(p => p.realName == key);

        if (part == null)
        {
            Debug.Log($"[FamilyManager] GetBubbleColor: part not found for key/display='{key}'");
            return Color.white;
        }

        float normalizedBond = 0f;
        if (part.bondMax > 0f)
            normalizedBond = Mathf.Clamp01(part.bond / part.bondMax);

        float saturationValue = part.bondToSaturation.Evaluate(normalizedBond);

        Color.RGBToHSV(part.baseColor, out float h, out float s, out float v);
        s = Mathf.Clamp01(saturationValue);

        return Color.HSVToRGB(h, s, v);
    }

    public TMP_FontAsset GetFontAsset(string key)
    {
        // Find by internal key
        var part = parts.Find(p => p.key == key);

        // Fallbacks
        if (part == null) part = parts.Find(p => p.realName == key);
        if (part == null) part = parts.Find(p => GetDisplayName(p.key) == key);

        if (part == null)
            return defaultFont;

        // Gate font behind bond
        if (part.bond < bondToRevealFont)
            return defaultFont;

        return part.font != null ? part.font : defaultFont;
    }

    public Color GetTextColor(string key)
    {
        var part = parts.Find(p => p.key == key);

        // Fallbacks
        if (part == null) part = parts.Find(p => p.realName == key);
        if (part == null) part = parts.Find(p => GetDisplayName(p.key) == key);

        if (part == null)
            return defaultTextColor;

        // Gate text color behind the same threshold as font
        if (part.bond < bondToRevealFont)
            return defaultTextColor;

        return part.textColor;
    }

    public bool IsNameRevealed(string key)
    {
        var part = parts.Find(p => p.key == key);
        if (part == null)
            return false;

        // Automatic gating: name reveals at a threshold OR manually revealed
        if (part.bond >= bondToRevealName)
            return true;

        return part.nameRevealed;
    }

    public string GetDisplayName(string key)
    {
        var part = parts.Find(p => p.key == key);
        if (part == null)
            return "???"; // display text only, do NOT touch the key

        // Only hide the name text; keep the key for color logic
        return IsNameRevealed(key) ? part.realName : "???";
    }

    public void RevealName(string key)
    {
        var part = parts.Find(p => p.key == key);
        if (part != null)
            part.nameRevealed = true;
    }

    public void AddBond(string key, float amount)
    {
        var part = parts.Find(p => p.key == key);
        if (part == null)
        {
            Debug.LogWarning($"No family member found with key '{key}' to add bond.");
            return;
        }

        part.bond += amount;
        Debug.Log($"Bond with {key} increased by {amount}. New value: {part.bond}");

        // TODO: [FX] Trigger particle animation of this family member's color
        // (e.g., ParticleSystem.Play() or spawn a prefab at Dialogue UI position)
    }

    // --- Yarn Setup ---
    private void Start() {
        var runner = FindObjectOfType<DialogueRunner>();
        if (runner == null) return;
        Debug.Log("FamilyManager Start: Found runner = " + runner);
    }
    private void OnEnable()
    {
        DialogueRunner runner = FindObjectOfType<DialogueRunner>();
        if (runner != null) RegisterFunctions(runner);
    }

    private void RegisterFunctions(DialogueRunner runner)
    {
        runner.AddFunction<string, string>("GetPartDisplayName", key => GetDisplayName(key));
        runner.AddFunction<string, bool>("IsNameRevealed", key => IsNameRevealed(key));
    }
}
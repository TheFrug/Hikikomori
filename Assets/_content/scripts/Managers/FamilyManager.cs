using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Unity;
using TMPro;
using ProjectHiki.UI;

public class FamilyManager : MonoBehaviour
{
    public static FamilyManager Instance { get; private set; }

    [System.Serializable]
    public class PartInfo
    {
        public string key;
        public string realName;
        public bool nameRevealed;
        public float bond;

        [Header("Visuals")]
        public Color baseColor = Color.gray;
        public TMP_FontAsset font;
        public AnimationCurve bondToSaturation = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 0-1 curve mapping

        [Header("Bond mapping")]
        [Tooltip("Maximum bond value used to normalize 'bond' into 0..1 for colour saturation. Adjust to your game's scale.")]
        public float bondMax = 10f;
    }

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

    public Color GetBubbleColor(string key)
    {
        var part = parts.Find(p => p.key == key);
        if (part == null) return Color.white;

        // Normalize bond (whole number) into 0..1 based on part.bondMax
        float normalizedBond = 0f;
        if (part.bondMax > 0f)
            normalizedBond = Mathf.Clamp01(part.bond / part.bondMax);
        else
            normalizedBond = 0f;

        // Evaluate saturation modifier from the curve (curve expects 0..1 input)
        float saturationValue = part.bondToSaturation.Evaluate(normalizedBond);

        // Take the part's baseColor, convert to HSV, set saturation from curve,
        // then convert back to RGB. This keeps hue and value but changes saturation.
        Color.RGBToHSV(part.baseColor, out float h, out float s, out float v);

        s = Mathf.Clamp01(saturationValue);

        Color result = Color.HSVToRGB(h, s, v);
        return result;
    }

    public TMP_FontAsset GetFontAsset(string key)
    {
        var part = parts.Find(p => p.key == key);
        if (part != null && part.font != null)
            return part.font;
        return null;
    }

    public bool IsNameRevealed(string key)
    {
        var part = parts.Find(p => p.key == key);
        return part != null && part.nameRevealed;
    }

    public string GetDisplayName(string key)
    {
        Debug.Log("Getting Display name for" + key);
        var part = parts.Find(p => p.key == key);
        if (part == null)
            return "???";

        if (part.nameRevealed)
            return part.realName;

        return "???";
        //return GenerateGrawlix(key);
    }

    public void RevealName(string key)
    {
        var part = parts.Find(p => p.key == key);
        if (part != null)
            part.nameRevealed = true;
    }

    private string GenerateGrawlix(string seed)
    {
        string[] symbols = { "#", "@", "%", "&", "$", "!", "?" };
        StringBuilder sb = new StringBuilder();
        int length = Random.Range(4, 8);
        for (int i = 0; i < length; i++)
        {
            sb.Append(symbols[Random.Range(0, symbols.Length)]);
        }
        return sb.ToString();
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

        // Register functions that RETURN values:
        runner.AddFunction<string, string>("GetPartDisplayName", key => GetDisplayName(key));
        runner.AddFunction<string, bool>("IsNameRevealed", key => IsNameRevealed(key));
    }

    // --- Yarn Commands ---

    [YarnCommand("reveal_name")]
    public static void Yarn_RevealName(string key)
    {
        Instance?.RevealName(key);
    }

    [YarnCommand("add_bond")]
    public static void Yarn_AddBond(string key, float amount)
    {
        Instance?.AddBond(key, amount);
    }

    [YarnCommand("RandomThought")]
    public static void Yarn_RandomThought(string key, string text) {
        var style = Instance.GetBubbleColor(key);
        ThoughtBubbleView.Instance.SpawnThought(key, text);
    }
}

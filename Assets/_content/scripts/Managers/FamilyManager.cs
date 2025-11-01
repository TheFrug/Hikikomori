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
        public AnimationCurve bondToSaturation; // 0-1 curve mapping - Why does this exist?
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
        var intensity = part.bondToSaturation.Evaluate(part.bond); // THIS DOESN'T WORK - Assets\_content\scripts\Managers\FamilyManager.cs(45,30): error CS1061: 'FamilyManager.PartInfo' does not contain a definition for 'bondToSaturation' and no accessible extension method 'bondToSaturation' accepting a first argument of type 'FamilyManager.PartInfo' could be found (are you missing a using directive or an assembly reference?)
        var c = part.baseColor;
        c = Color.Lerp(Color.gray, c, intensity);
        return c;
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

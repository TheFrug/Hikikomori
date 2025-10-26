using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Yarn.Unity;

public class FamilyManager : MonoBehaviour
{
    public static FamilyManager Instance { get; private set; }

    [System.Serializable]
    public class partInfo
    {
        public string key;
        public string realName;
        public bool nameRevealed;
    }

    [Header("parts")]
    public List<partInfo> parts = new List<partInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsNameRevealed(string key)
    {
        var part = parts.Find(p => p.key == key);
        return part != null && part.nameRevealed;
    }

    public string GetDisplayName(string key)
    {
        var part = parts.Find(v => v.key == key);
        if (part == null)
            return "???";

        if (part.nameRevealed)
            return part.realName;

        return GenerateGrawlix(key);
    }

    public void RevealName(string key)
    {
        var part = parts.Find(v => v.key == key);
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

    void Start()
    {
        // Register Yarn functions/commands if a DialogueRunner is present
        DialogueRunner runner = FindObjectOfType<DialogueRunner>();
        if (runner != null)
        {
            // IsNameRevealed(key) -> bool
            // Uses generic AddFunction<string, bool>
            runner.AddFunction<string, bool>("IsNameRevealed", (string key) =>
            {
                return Instance != null && Instance.IsNameRevealed(key);
            });

            // GetVoiceDisplayName(key) -> string
            runner.AddFunction<string, string>("GetVoiceDisplayName", (string key) =>
            {
                return Instance != null ? Instance.GetDisplayName(key) : key;
            });

            // optional command to reveal a voice from yarn: <<RevealVoiceName "Goblin">>
            runner.AddCommandHandler<string>("RevealVoiceName", (string key) =>
            {
                Instance?.RevealName(key);
            });
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

public class BehaviorUnlockManager : MonoBehaviour
{
    public static BehaviorUnlockManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private List<BehaviorData> allBehaviorData = new();
    [SerializeField] private List<BehaviorIconUI> allIcons = new();

    private HashSet<string> unlockedBehaviors = new();
    private HashSet<string> unlockedIcons = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeBehaviorStates();
        InitializeIconStates();
    }

    // -----------------------------
    // BehaviorData Unlock Logic
    // -----------------------------
    private void InitializeBehaviorStates()
    {
        foreach (var data in allBehaviorData)
        {
            if (!data.startsLocked)
            {
                data.unlocked = true;
                continue;
            }

            data.unlocked = unlockedBehaviors.Contains(data.behaviorName);
        }
    }

    public void UnlockBehavior(BehaviorData data)
    {
        if (data == null) return;

        data.unlocked = true;
        unlockedBehaviors.Add(data.behaviorName);
    }

    public void UnlockBehavior(string id)
    {
        foreach (var data in allBehaviorData)
        {
            if (data.behaviorName == id)
            {
                UnlockBehavior(data);
                break;
            }
        }
    }

    public bool IsBehaviorUnlocked(BehaviorData data) => data != null && data.unlocked;

    // -----------------------------
    // BehaviorIconUI Unlock Logic
    // -----------------------------
    private void InitializeIconStates()
    {
        foreach (var icon in allIcons)
        {
            if (icon == null) continue;

            if (!icon.startsUnlocked)
                icon.unlocked = unlockedIcons.Contains(icon.iconID);
            else
                icon.unlocked = true;

            icon.ApplyUnlockState();
        }
    }

    public void UnlockIcon(string id)
    {
        foreach (var icon in allIcons)
        {
            if (icon.iconID == id)
            {
                icon.unlocked = true;
                icon.ApplyUnlockState();
                unlockedIcons.Add(id);
                break;
            }
        }
    }

    public bool IsIconUnlocked(string id) => unlockedIcons.Contains(id);
}

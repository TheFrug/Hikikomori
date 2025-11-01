using System.Collections.Generic;
using UnityEngine;

public class BehaviorUnlockManager : MonoBehaviour
{
    public static BehaviorUnlockManager Instance { get; private set; }

    private HashSet<string> unlockedBehaviors = new HashSet<string>();

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

    public bool IsUnlocked(string behaviorName) => unlockedBehaviors.Contains(behaviorName);
    public void Unlock(string behaviorName) => unlockedBehaviors.Add(behaviorName);
}

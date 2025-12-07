using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class HopeLevelData
{
    public int rank; // Level rank, 0, 1, 2, ...
    public int hopeLevelUpThreshold = 5; // New threshold for this level
    public Vector2Int spoonRange = new Vector2Int(1, 4); // min/max spoons unlocked
    public UnityEvent onLevelUp; // Any custom inspector event
    public string unlockBehavior; // Name of behavior to unlock
    public ThoughtData thoughtToSpawn; // Optional thought key for ThoughtBubbleView
}

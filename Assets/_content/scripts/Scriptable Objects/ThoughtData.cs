using UnityEngine;
using Yarn.Unity;

[CreateAssetMenu(menuName = "Hiki/Thought", fileName = "NewThought")]
public class Thought : ScriptableObject
{
    public enum ThoughtType
    {
        Automatic,
        Interactive
    }

    public enum ThoughtSpeed
    {
        Slow,
        Normal,
        Fast
    }
    
    [Header("General")]
    [Header("Optional Yarn Data")]
    public TextAsset yarnScript;
    public string yarnNodeName;
    public ThoughtType type = ThoughtType.Automatic;

    [Header("Speed Settings")]
    public ThoughtSpeed speed = ThoughtSpeed.Normal;

    [Tooltip("Optional: override auto bubble duration. Leave at 0 to use manager defaults.")]
    public float bubbleLifetimeOverride = 0f;

    [Tooltip("Optional: override vertical move speed of bubbles. Leave at 0 to use manager defaults.")]
    public float travelSpeedOverride = 0f;
}

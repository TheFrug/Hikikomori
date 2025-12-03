using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBehavior", menuName = "Hiki/Behavior", order = 1)]
public class BehaviorData : ScriptableObject
{
    [Header("Basic")]
    public string behaviorName = "New Action";
    [TextArea(2, 4)] public string behaviorDescription = "Short fun behavior Description.";
    public bool isDefault = false;
    public bool startsLocked;

    [Header("Time")]
    public int durationMinutes = 30;            // used for one-shot visual timing
    public bool isToggle = false;               // toggle = player-controlled (no fixed time)

    [Header("Costs / Impacts")]
    public int spoonsCost = 0;
    public bool hideSpoonsCost = false;
    public int hopeGain = 0;
    public int stresGain = 0;
    public int hungerImpact = 0; //deprecated
    [Tooltip("-1 = none")]
    public float cashCost = -1f; //deprecated

    [Header("Visuals")]
    public Sprite icon;

    [Header("Behavior Run Mode")]
    public bool isScene;         // true => interactive scene (uses Thought in interactive mode)
    public Thought thought;      // thought to play for this behavior (automatic/interactive)
}

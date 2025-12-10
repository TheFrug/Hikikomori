using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBehavior", menuName = "Hiki/Behavior", order = 1)]
public class BehaviorData : ScriptableObject
{
    [Header("Basic")]
    public string behaviorName = "New Action";
    [TextArea(2, 4)] public string behaviorDescription = "Short fun behavior Description.";
    public bool repeatable;
    public bool startsLocked = false; // Controls if player can use it right away
    public bool unlocked = false;

    [Header("Time")]
    public int durationMinutes = 30;            // used to calculate clock change

    [Header("Costs / Impacts")]
    public int spoonsCost = 0;
    public bool hideSpoonsCost = false;

    public int hopeImpact = 0;
    public int stressImpact = 0;

    [Header("Visuals")]
    public Sprite icon;

    [Header("Behavior Run Mode")]
    public ThoughtData thought;      // thought to play for this behavior (automatic/interactive)
}

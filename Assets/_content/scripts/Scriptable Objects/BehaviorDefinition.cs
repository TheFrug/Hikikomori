using UnityEngine;

[CreateAssetMenu(fileName = "NewBehavior", menuName = "Hiki/Behavior", order = 1)]
public class BehaviorData : ScriptableObject
{
    [Header("Basic")]
    public string actionName = "New Action";
    [TextArea(2,4)] public string description = "Short fun description.";

    [Header("Time")]
    public bool isToggle = false;               // toggle = player-controlled (no fixed time)
    public int durationMinutes = 30;            // used when isToggle == false

    [Header("Costs / Impacts")]
    public int spoonsCost = 0;                  // -1 => "???"
    public bool hideSpoonsCost = false;
    public int hungerImpact = 0;                // + means increases hunger; - reduces hunger
    [Tooltip("-1 = none")]
    public float cashCost = -1f;                // -1 => none

    [Header("Visuals")]
    public Sprite icon;
}

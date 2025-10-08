using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBehavior", menuName = "Hiki/Behavior", order = 1)]
public class BehaviorData : ScriptableObject
{
    [Header("Basic")]
    public string behaviorName = "New Action";
    [TextArea(2, 4)] public string behaviorDescription = "Short fun behavior Description.";
    public bool isDefault = false;

    [Header("Time")]
    public bool isToggle = false;               // toggle = player-controlled (no fixed time)
    public int durationMinutes = 30;            // used when isToggle == false

    [Header("Costs / Impacts")]
    public int spoonsCost = 0;
    public bool hideSpoonsCost = false;         // False -> ???
    public int hungerImpact = 0;                // + means increases hunger; - reduces hunger
    [Tooltip("-1 = none")]
    public float cashCost = -1f;                // -1 => none

    [Header("Visuals")]
    public Sprite icon;

    public List<BehaviorYarnTrigger> yarnTrigger;

    [System.Serializable]
    public class BehaviorYarnTrigger
    {
        public string yarnNodeName;
        public enum TriggerTime { OnStart, OnMidpoint, OnEnd }
        public TriggerTime triggerTime;
        public float triggerMinute; // optional override for custom minute marks
    }
}

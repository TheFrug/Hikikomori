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

    public ThoughtType type = ThoughtType.Automatic;

    [Header("Optional Yarn Data")]
    public TextAsset yarnScript;
    public string yarnNodeName;
}


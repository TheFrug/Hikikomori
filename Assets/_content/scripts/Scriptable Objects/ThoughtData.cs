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
    public string speakerKey = "Truth";
    [TextArea] public string previewText = "This is a passing thought.";
    public float lifetime = 3f;
    public float riseDistance = 80f;

    [Header("Optional Yarn Data")]
    public TextAsset yarnScript;
    public string yarnNodeName;
}
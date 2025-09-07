using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName = "Dialogue/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    [Tooltip("Exact speaker name used in Yarn (case-sensitive)")]
    public string characterName;

    public Color nameplateColor = Color.white;
    public Color dialogueBoxColor = new Color(0f, 0f, 0f, 0.6f);
    public Color fontColor = Color.white;
    public TMP_FontAsset font; // optional — leave null to use dialogueText's default
}

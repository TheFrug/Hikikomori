using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Character Database")]
public class CharacterDatabase : ScriptableObject
{
    public List<CharacterProfile> characters = new List<CharacterProfile>();

    public CharacterProfile GetProfile(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return characters.Find(c => c != null && c.characterName == name);
    }
}

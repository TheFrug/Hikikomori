using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BehaviorPanelManager : MonoBehaviour
{
    [SerializeField] private List<Button> optionButtons;

    private Dictionary<string, List<string>> roomOptions = new()
    {
        { "Bedroom", new List<string> { "Sleep", "Read", "Change Clothes", "Meditate", "Journal", "Check Phone" } },
        { "Kitchen", new List<string> { "Cook", "Eat", "Wash Dishes", "Snack", "Make Tea", "Clean Counter" } },
        { "Hallway", new List<string> { "Leave House", "Check Mail", "Stretch", "Water Plants", "Pace", "Look Outside" } }
    };

    private void Start()
    {
        // Populate with Bedroom options at game start
        ShowOptions("Bedroom");
    }

    public void ShowOptions(string roomName)
    {
        if (!roomOptions.ContainsKey(roomName)) return;

        var options = roomOptions[roomName];

        for (int i = 0; i < optionButtons.Count; i++)
        {
            if (i < options.Count)
            {
                optionButtons[i].gameObject.SetActive(true);
                TMP_Text label = optionButtons[i].GetComponentInChildren<TMP_Text>();
                label.text = options[i];

                // Clear old listeners before adding new
                optionButtons[i].onClick.RemoveAllListeners();
                string choice = options[i];
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(choice));
            }
            else
            {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionSelected(string choice)
    {
        Debug.Log("Player selected: " + choice);
        // TODO: hook into resource changes here
    }
}

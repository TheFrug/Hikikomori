using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;

public class YarnManager : MonoBehaviour
{
    public static YarnManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // FamilyManager Methods
    [YarnCommand("reveal_name")]
    public static void Yarn_RevealName(string key)
    {
        FamilyManager.Instance?.RevealName(key);
    }

    [YarnCommand("add_bond")]
    public static void Yarn_AddBond(string key, float amount)
    {
        FamilyManager.Instance?.AddBond(key, amount);
    }

    // ResourceManager Methods
    [YarnCommand("modify_spoons")]
    public static void Yarn_ModifySpoons(int delta)
    {
        ResourceManager.Instance?.ModifySpoons(delta);
    }

    [YarnCommand("modify_stress")]
    public static void Yarn_ModifyStress(int delta)
    {
        ResourceManager.Instance?.ModifyStress(delta);
    }

    [YarnCommand("modify_hope")]
    public static void Yarn_ModifyHope(int delta)
    {
        ResourceManager.Instance?.ModifyHope(delta);
    }

    [YarnCommand("reseed_daily_spoons")]
    public static void Yarn_ReseedDailySpoons()
    {
        ResourceManager.Instance?.LoadDailySpoons();
    }

    // -----------------------------
    // BehaviorUnlockManager Methods
    // -----------------------------

    // Unlock a behavior choice by ID
    [YarnCommand("unlock_behavior")]
    public static void Yarn_UnlockBehavior(string behaviorID)
    {
        BehaviorUnlockManager.Instance?.UnlockBehavior(behaviorID);
    }

    // Unlock a behavior icon by ID
    [YarnCommand("unlock_icon")]
    public static void Yarn_UnlockIcon(string iconID)
    {
        BehaviorUnlockManager.Instance?.UnlockIcon(iconID);
    }
}

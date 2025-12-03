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
}

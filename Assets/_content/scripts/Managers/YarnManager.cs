using UnityEngine;
using Yarn.Unity;

/// <summary>
/// All Yarn commands for Project Hiki live here.
/// This class forwards Yarn calls to the appropriate managers
/// (ResourceManager, BehaviorManager, Thought systems, etc.)
/// </summary>
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

    // ---------------------------
    // RESOURCE COMMANDS
    // ---------------------------

    [YarnCommand("modify_spoons")]
    public void CMD_ModifySpoons(int delta)
    {
        ResourceManager.Instance?.ModifySpoons(delta);
    }

    [YarnCommand("modify_stress")]
    public void CMD_ModifyStress(int delta)
    {
        ResourceManager.Instance?.ModifyStress(delta);
    }

    [YarnCommand("modify_hope")]
    public void CMD_ModifyHope(int delta)
    {
        ResourceManager.Instance?.ModifyHope(delta);
    }

    [YarnCommand("reseed_daily_spoons")]
    public void CMD_ReseedDailySpoons()
    {
        ResourceManager.Instance?.LoadDailySpoons();
    }

    // ---------------------------
    // SHUTDOWN / INPUT LOCK
    // ---------------------------
    /*
    [YarnCommand("set_behavior_block")]
    public void CMD_SetBehaviorBlock(bool block)
    {
        BehaviorManager.BlockBehaviorSelection = block;
    }
    */

}

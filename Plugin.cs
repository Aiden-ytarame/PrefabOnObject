using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace PrefabOnAbsolutePos;

[BepInPlugin(Guid, Name, Version)]
[BepInProcess("Project Arrhythmia.exe")]

public class Plugin : BasePlugin
{
    public static Plugin Inst;
    
    Harmony _harmony;
    const string Guid = "me.ytarame.AbsolutePosPrefab";
    const string Name = "AbsolutePosPrefab";
    const string Version = "1.0.0";


    public override void Load()
    {
        Inst = this;
        _harmony = new Harmony(Guid);
        _harmony.PatchAll();

        // Plugin startup logic
        Log.LogInfo($"Plugin {Guid} is loaded!");
    }
    
}

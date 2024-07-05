using System;
using HarmonyLib;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PrefabOnAbsolutePos;

[HarmonyPatch(typeof(ObjectManager))]
public class ObjectManager_Patch
{
    public static bool QuickSpawn;
    public static bool ShouldSpawnOnSelec;
    public static bool ShouldFollowRot;
    public static bool ShouldSpawnOnCamera;
    
    
    [HarmonyPatch(nameof(ObjectManager.HandlePrefabOffsetKeyframes))]
    [HarmonyPrefix]
    static void PostSetup(ref ObjectManager __instance, int _i)
    {
        if ((!ShouldSpawnOnSelec && !ShouldSpawnOnCamera) || !QuickSpawn)
            return;

        string id = DataManager.inst.gameData.beatmapObjects[_i].prefabInstanceID;
            
        //so this gets the PrefabObject from the BeatmapGameObject, im unsure if this is needed,
        //as BeatmapGameObject contains an event array but doesnt have the GetEvent() function
        var obj = DataManager.inst.gameData.prefabObjects.Find(
            DelegateSupport.ConvertDelegate<Il2CppSystem.Predicate<DataManager.GameData.PrefabObject>>
                (new Predicate<DataManager.GameData.PrefabObject>(x => x.ID == id)));

        if (obj == null)
        {
            QuickSpawn = false;
            return;
        }

        if (ShouldSpawnOnCamera)
        {
            obj.GetEvent().SetVal(0, EventManager.inst.CamPos.x); //pos x
            obj.GetEvent().SetVal(1, EventManager.inst.CamPos.y); //pos y
            return;
        }
        
        var selectedObject = ObjectEditor.Inst.MainSelectedObject;
        if (ObjectManager.inst.beatmapGameObjects.TryGetValue(selectedObject.ID, out var goRef))
        {
            if (!goRef.VisualObject)
                return;
            
            obj.GetEvent().SetVal(0, goRef.VisualObject.position.x); //pos x
            obj.GetEvent().SetVal(1, goRef.VisualObject.position.y); //pos y
            
            if(ShouldFollowRot)
                obj.GetEvent(2).SetVal(0, goRef.VisualObject.rotation.eulerAngles.z); //rot
        }
        else
        {
            EditorManager.inst.DisplayNotification("Selected Object Invalid\n[remember to expand a prefab if you're selecting it]", 5, EditorManager.NotificationType.Error);
        }
        
        QuickSpawn = false;
    }

}

[HarmonyPatch(typeof(PrefabQuickSpawner))]
public class PrefabQuickSpawner_Patch
{
    [HarmonyPatch(nameof(PrefabQuickSpawner.CheckForPrefabSpawn))]
    [HarmonyPrefix]
    static void PostSetup(int _i)
    {
        //this runs when you quick spawn a prefab.
        if (ObjectManager_Patch.ShouldSpawnOnSelec)
        {
            ObjectManager_Patch.QuickSpawn = true;
        }
    }

}

[HarmonyPatch(typeof(EditorManager))]
public class QS_Editor_Patch
{
    [HarmonyPatch(nameof(EditorManager.Start))]
    [HarmonyPostfix]
    static void PostSetup(ref EditorManager __instance)
    {
        var settings = __instance.transform.parent.Find("Editor GUI/sizer/EditorDialogs/QuickSpawnDialog/Scroll View/Viewport/Content/settings");
        
        
        //GameObjects]
        var content = settings.Find("1/content-wrapper/content");
        var label = content.Find("keycode-label");
        var active = content.Find("keycode/active");

        //Label
        var labelObj = Object.Instantiate(label, settings);
        labelObj.GetComponentInChildren<TextMeshProUGUI>().text = "Spawn On Selection?";
        labelObj.SetSiblingIndex(1);
        
        //the Spawn toggle
        var activeObj = Object.Instantiate(active, settings);
        activeObj.GetComponent<Toggle>().Set(false);
        activeObj.GetComponent<Toggle>().onValueChanged.AddListener(new Action<bool>(x => {ObjectManager_Patch.ShouldSpawnOnSelec = x;}));
        activeObj.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "Spawn";
        
        //setting up the tooltip
        var tooltip = activeObj.GetComponent<TooltipTrigger>();
        tooltip.keyCodeTips.Clear();
        tooltip.title = "Spawn On Selection";
        tooltip.description = "if an quick spawned prefab should spawn on the position of the current selection \nDoes not work when the Camera toggle is ON";
        activeObj.SetSiblingIndex(2);
        
        
        //the Rotate toggle
        activeObj = Object.Instantiate(active, settings);
        activeObj.GetComponent<Toggle>().Set(false);
        activeObj.GetComponent<Toggle>().onValueChanged.AddListener(new Action<bool>(x => {ObjectManager_Patch.ShouldFollowRot = x;}));
        activeObj.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "Rotate";

        //setting up the tooltip
        tooltip = activeObj.GetComponent<TooltipTrigger>();
        tooltip.keyCodeTips.Clear();
        tooltip.title = "Follow Rotation";
        tooltip.description = "if an quick spawned prefab should copy the rotation of the current selection \nDoes not work when the Camera toggle is ON";
        activeObj.SetSiblingIndex(3);
        
        //the Spawn on Camera toggle
        activeObj = Object.Instantiate(active, settings);
        activeObj.GetComponent<Toggle>().Set(false);
        activeObj.GetComponent<Toggle>().onValueChanged.AddListener(new Action<bool>(x => {ObjectManager_Patch.ShouldSpawnOnCamera = x;}));
        activeObj.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "Camera";

        //setting up the tooltip
        tooltip = activeObj.GetComponent<TooltipTrigger>();
        tooltip.keyCodeTips.Clear();
        tooltip.title = "Spawn on Camera";
        tooltip.description = "if an quick spawned prefab should spawn on the center of the camera";
        activeObj.SetSiblingIndex(4);

        
        
    }
}

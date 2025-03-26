using System;
using HarmonyLib;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VGFunctions;
using Object = UnityEngine.Object;

namespace PrefabOnAbsolutePos;

[HarmonyPatch(typeof(ObjectManager))]
public class ObjectManagerPatch
{
    public static bool QuickSpawn;
    public static bool ShouldSpawnOnSelec;
    public static bool ShouldSpawnOnCamera;
    public static bool ShouldFollowRot;
    
    public static float Xoffset;
    public static float Yoffset;
    public static float Zoffset;
    
    private static readonly string ErrorMessage = "Selected Object Invalid\n[remember to expand a prefab if you're selecting it]";
    

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
            obj.GetEvent().SetVal(0, EventManager.inst.CamPos.x + Xoffset); //pos x
            obj.GetEvent().SetVal(1, EventManager.inst.CamPos.y + Yoffset); //pos y

            if (ShouldFollowRot)
            {
                obj.GetEvent(2).SetVal(0,  EventManager.inst.camRot + Zoffset); //rot
            }
            QuickSpawn = false;
            return;
        }
        
        var selectedObject = ObjectEditor.Inst.MainSelectedObject;
        if (ObjectManager.inst.beatmapGameObjects.TryGetValue(selectedObject.ID, out var goRef))
        {
            if (!goRef.VisualObject)
                return;
            
            obj.GetEvent().SetVal(0, goRef.VisualObject.position.x + Xoffset); //pos x
            obj.GetEvent().SetVal(1, goRef.VisualObject.position.y + Yoffset); //pos y
            
            if (ShouldFollowRot)
            {
                obj.GetEvent(2).SetVal(0, goRef.VisualObject.rotation.eulerAngles.z + Zoffset); //rot
            }
        }
        else
        {
            if (!EditorManager.inst.liveNotifTracking.ContainsKey(ErrorMessage))
            {
                EditorManager.inst.DisplayNotificationLoop(ErrorMessage, 3, EditorManager.NotificationType.Error);
            }
        }

        QuickSpawn = false;
    }
}

[HarmonyPatch(typeof(PrefabQuickSpawner))]
public class PrefabQuickSpawnerPatch
{
    [HarmonyPatch(nameof(PrefabQuickSpawner.CheckForPrefabSpawn))]
    [HarmonyPrefix]
    static void PostSetup()
    {
        //this runs when you quick spawn a prefab.
        if (ObjectManagerPatch.ShouldSpawnOnSelec)
        {
            ObjectManagerPatch.QuickSpawn = true;
        }
    }

}

[HarmonyPatch(typeof(EditorManager))]
public class QsEditorPatch
{
    [HarmonyPatch(nameof(EditorManager.Start))]
    [HarmonyPostfix]
    static void PostSetup(ref EditorManager __instance)
    {
        ObjectManagerPatch.QuickSpawn = false;
        ObjectManagerPatch.ShouldSpawnOnCamera = false;
        ObjectManagerPatch.ShouldFollowRot = false;
        ObjectManagerPatch.ShouldSpawnOnSelec = false;

        ObjectManagerPatch.Xoffset = 0;
        ObjectManagerPatch.Yoffset = 0;
        ObjectManagerPatch.Zoffset = 0;
          
        var settings =
            __instance.transform.parent.Find(
                "Editor GUI/sizer/EditorDialogs/QuickSpawnDialog/Scroll View/Viewport/Content/settings");
        var offset = __instance.transform.parent.Find(
            "Editor GUI/sizer/EditorDialogs/GameObjectDialog/data/left/Scroll View/Viewport/Content/parent/parent_more");

        var position =
            __instance.transform.parent.Find(
                "Editor GUI/sizer/EditorDialogs/GameObjectDialog/data/right/position/position");
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
        activeObj.GetComponent<Toggle>().onValueChanged.AddListener(new Action<bool>(x =>
        {
            ObjectManagerPatch.ShouldSpawnOnSelec = x;
        }));
        activeObj.Find("content/text").GetComponent<TextMeshProUGUI>().text = "Spawn";

        //setting up the tooltip
        var tooltip = activeObj.GetComponent<TooltipTrigger>();
        tooltip.keyCodeTips.Clear();
        tooltip.title = "Spawn On Selection";
        tooltip.description =
            "if an quick spawned prefab should spawn on the position of the current selection \nDoes not work when the Camera toggle is ON";
        activeObj.SetSiblingIndex(2);


        //the Rotate toggle
        activeObj = Object.Instantiate(active, settings);
        activeObj.GetComponent<Toggle>().Set(false);
        activeObj.GetComponent<Toggle>().onValueChanged
            .AddListener(new Action<bool>(x => { ObjectManagerPatch.ShouldFollowRot = x; }));
        activeObj.Find("content/text").GetComponent<TextMeshProUGUI>().text = "Rotate";

        //setting up the tooltip
        tooltip = activeObj.GetComponent<TooltipTrigger>();
        tooltip.keyCodeTips.Clear();
        tooltip.title = "Follow Rotation";
        tooltip.description =
            "if an quick spawned prefab should copy the rotation of the current selection \nDoes not work when the Camera toggle is ON";
        activeObj.SetSiblingIndex(3);

        //the Spawn on Camera toggle
        activeObj = Object.Instantiate(active, settings);
        activeObj.GetComponent<Toggle>().Set(false);
        activeObj.GetComponent<Toggle>().onValueChanged.AddListener(new Action<bool>(x =>
        {
            ObjectManagerPatch.ShouldSpawnOnCamera = x;
        }));
        activeObj.Find("content/text").GetComponent<TextMeshProUGUI>().text = "Camera";

        //setting up the tooltip
        tooltip = activeObj.GetComponent<TooltipTrigger>();
        tooltip.keyCodeTips.Clear();
        tooltip.title = "Spawn on Camera";
        tooltip.description = "if an quick spawned prefab should spawn on the center of the camera";
        activeObj.SetSiblingIndex(4);
        
        //spawn the parent offset tab
        activeObj = Object.Instantiate(offset, settings);
        activeObj.Find("pos_label").GetComponent<TextMeshProUGUI>().text = "Spawn Offset";
        activeObj.Find("sca_label")?.gameObject.SetActive(false);
        activeObj.Find("rot_label")?.gameObject.SetActive(false);
        
        Object.Destroy(activeObj.Find("pos_row").gameObject);
        Object.Destroy(activeObj.Find("sca_row").gameObject);
        
        Object.Instantiate(position.parent.Find("value_label"), activeObj).SetSiblingIndex(2);
        
        Transform posOffet = Object.Instantiate(position, activeObj);
        posOffet.SetSiblingIndex(3);
        
        CompLib.InitInputFieldFloat(posOffet.Find("x"),
            _getFunc: new Func<float>(() => ObjectManagerPatch.Xoffset),
            _setFunc:new Action<float>(x => { ObjectManagerPatch.Xoffset = x; }),
            _refreshUINeeded:new Func<bool>(() => true),
            _type: CompLib.InputFieldStyle.AddSub
        );
        CompLib.InitInputFieldFloat(posOffet.Find("y"),
            _getFunc: new Func<float>(() => ObjectManagerPatch.Yoffset),
            _setFunc:new Action<float>(x => { ObjectManagerPatch.Yoffset = x; }),
            _refreshUINeeded:new Func<bool>(() => true),
            _type: CompLib.InputFieldStyle.AddSub
        );

        Transform rotLabel = Object.Instantiate(position.parent.Find("value_label"), activeObj);
        rotLabel.SetSiblingIndex(4);
        rotLabel.GetChild(1).gameObject.SetActive(false);
        rotLabel.GetComponentInChildren<TextMeshProUGUI>().text = "Rotation";

        Transform rotRow = activeObj.Find("rot_row");
        
        CompLib.InitInputFieldFloat(rotRow.GetChild(0),
            _getFunc: new Func<float>(() => ObjectManagerPatch.Zoffset),
            _setFunc:new Action<float>(x => { ObjectManagerPatch.Zoffset = x; }),
            _refreshUINeeded:new Func<bool>(() => true),
            _type: CompLib.InputFieldStyle.AddSub
        );
        
        activeObj.SetSiblingIndex(5);
    }
}

using Barotrauma;
using Barotrauma.MoreLevelContent.Shared.Utils;
using MoreLevelContent.Shared.Data;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using Barotrauma.MoreLevelContent.Client.UI;

namespace MoreLevelContent.Shared.Generation
{
    public partial class MapDirector : Singleton<MapDirector>
    {
        public override void Setup()
        {
            // Map
            var map_ctr_load = typeof(Map).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(CampaignMode), typeof(XElement) }, null);
            var map_save = typeof(Map).GetMethod(nameof(Map.Save));
            var map_generate = typeof(Map).GetMethod("Generate", BindingFlags.Instance | BindingFlags.NonPublic);

            // Leveldata
            var leveldata_ctr_load = typeof(LevelData).GetConstructor(new Type[] { typeof(XElement), typeof(float?) });
            var leveldata_ctr_generate = typeof(LevelData).GetConstructor(new Type[] { typeof(LocationConnection) });
            var leveldata_save = typeof(LevelData).GetMethod(nameof(LevelData.Save));

            // Map data
            _ = Main.Harmony.Patch(map_ctr_load, postfix: new HarmonyMethod(GetType().GetMethod(nameof(OnMapLoad), BindingFlags.Static | BindingFlags.NonPublic)));
            
            // Level data
            _ = Main.Harmony.Patch(leveldata_ctr_load, postfix: new HarmonyMethod(GetType().GetMethod(nameof(OnLevelDataLoad), BindingFlags.Static | BindingFlags.NonPublic)));
            _ = Main.Harmony.Patch(leveldata_ctr_generate, postfix: new HarmonyMethod(GetType().GetMethod(nameof(OnLevelDataGenerate), BindingFlags.Static | BindingFlags.NonPublic)));
            _ = Main.Harmony.Patch(leveldata_save, postfix: new HarmonyMethod(GetType().GetMethod(nameof(OnLevelDataSave), BindingFlags.Static | BindingFlags.NonPublic)));

#if CLIENT
            MapUI.Instance.Setup();
#endif
        }

        private static void OnLevelDataGenerate(LevelData __instance, LocationConnection locationConnection)
        {
            LevelData_MLC levelData = new LevelData_MLC();
            var rand = new MTRandom(ToolBox.StringToInt(__instance.Seed));
            if (__instance.HasBeaconStation)
            {
                levelData.HasBeaconConstruction = rand.NextDouble() < locationConnection.Locations.Select(l => l.Type.BeaconStationChance).Max();
                if (levelData.HasBeaconConstruction) __instance.HasBeaconStation = false;
            }

            __instance.AddData(levelData);
        }

        private static void OnLevelDataLoad(LevelData __instance, XElement element)
        {
            LevelData_MLC data = new LevelData_MLC
            {
                HasBeaconConstruction = element.GetAttributeBool("mlc_hasbeaconconstruction", false)
            };
            __instance.AddData(data);
            // turn off regular beacons on construction missions
            if (data.HasBeaconConstruction) __instance.HasBeaconStation = false;
        }

        private static void OnLevelDataSave(LevelData __instance, XElement parentElement)
        {
            XElement levelData = (XElement)parentElement.LastNode;
            LevelData_MLC data = __instance.MLC();
            levelData.SetAttributeValue("mlc_hasbeaconconstruction", data.HasBeaconConstruction);
            // don't brick saves when you uninstall the mod
            if (data.HasBeaconConstruction) levelData.SetAttributeValue("hasbeaconstation", true);
        }

        private static void OnMapLoad(Map __instance)
        {
            Log.Error("OnMapLoad");
            // Update a save from before this update to include construction beacons
            if (!__instance.Connections.Any(c => c.LevelData.MLC().HasBeaconConstruction))
            {
                Log.Debug("Migrating old save...");
                for (int i = 0; i < __instance.Connections.Count; i++)
                {
                    if (!__instance.Connections[i].LevelData.HasBeaconStation) continue;
                    bool addBeacon = Rand.Range(0.0, 1.0f) < __instance.Connections[i].Locations.Select(l => l.Type.BeaconStationChance).Max();
                    __instance.Connections[i].LevelData.MLC().HasBeaconConstruction = addBeacon;
                    __instance.Connections[i].LevelData.HasBeaconStation = false;
                }
            }
        }

        private object OnMapSave(object self, Dictionary<string, object> args)
        {
            Log.Debug("OnMapSave");
            return null;
        }

        private object OnMapGenerate(object self, Dictionary<string, object> args)
        {
            Log.Debug("OnMapGenerate");
            return null;
        }

    }
}
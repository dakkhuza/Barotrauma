using Barotrauma;
using Barotrauma.MoreLevelContent.Config;
using Barotrauma.MoreLevelContent.Shared.Config;
using Barotrauma.MoreLevelContent.Shared.Utils;
using HarmonyLib;
using Microsoft.Xna.Framework;
using MoreLevelContent.Shared.Generation.Interfaces;
using MoreLevelContent.Shared.Store;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace MoreLevelContent.Shared.Generation
{
    public class MapDirector : Singleton<MapDirector>
    {
        public override void Setup()
        {
            // Map
            var map_load = typeof(Map).GetMethod(nameof(Map.Load));
            var map_save = typeof(Map).GetMethod(nameof(Map.Save));
            var map_generate = typeof(Map).GetMethod("Generate", BindingFlags.Instance | BindingFlags.NonPublic);

            // Leveldata
            var leveldata_ctr = typeof(LevelData).GetConstructor(new Type[] { typeof(XElement) });
            foreach (var item in typeof(LevelData).GetConstructors())
            {
                Log.Debug("============");
                foreach (var param in item.GetParameters())
                {
                    Log.Debug($"Param: {param.Name} Type:{param.ParameterType.Name}");
                }
                Log.Debug("------------");
            }

            // Map data
            Main.HookMethod("MLC::Map.Load", map_load, OnMapLoad, LuaCsHook.HookMethodType.Before);
            Main.HookMethod("MLC::Map.Save", map_save, OnMapSave, LuaCsHook.HookMethodType.Before);
            Main.HookMethod("MLC::Map.Generate", map_generate, OnMapGenerate, LuaCsHook.HookMethodType.Before);

            // Level data
            _ = Main.Harmony.Patch(leveldata_ctr, new HarmonyMethod(GetType().GetMethod(nameof(OnLevelDataLoad))));

        }

        /*
ConstructorInfo constructorInfoObj = myType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, types, null);
         */

        private static void OnLevelDataLoad()
        {
            Log.Debug("OnLevelDataLoad");
        }

        private object OnMapLoad(object self, Dictionary<string, object> args)
        {
            Log.Debug("OnMapLoad");
            return null;
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
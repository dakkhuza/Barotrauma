using Barotrauma;
using Barotrauma.Extensions;
using Barotrauma.Items.Components;
using Barotrauma.Networking;
using MoreLevelContent.Shared.Generation;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace MoreLevelContent.Shared
{
    partial class ILOMod : ACsMod
    {
        public static bool IsCampaign => GameMain.GameSession?.GameMode is MultiPlayerCampaign;
        public static bool IsRunning => GameMain.GameSession?.IsRunning ?? false;
        private LevelContentProducer generator;

        public ILOMod() => Init();

        public void Init()
        {
            var level_onCreateWrecks = typeof(Level).GetMethod("CreateWrecks", BindingFlags.NonPublic | BindingFlags.Instance);
            var level_onSpawnNPC = typeof(Level).GetMethod(nameof(Level.SpawnNPCs));
            generator = new LevelContentProducer();

            GameMain.LuaCs.Hook.HookMethod(
                "ilo_GenerateILO",
                level_onCreateWrecks,
                CreateWrecks,
                LuaCsHook.HookMethodType.After,
                this);

            GameMain.LuaCs.Hook.HookMethod(
                "ilo_SpawnNPC",
                level_onSpawnNPC,
                SpawnNPC,
                LuaCsHook.HookMethodType.Before,
                this
                );
        }

        public object CreateWrecks(object self, Dictionary<string, object> args)
        {
            if (!generator.Enabled) return null;
            generator.CreatePirateOutpost();
            return null;
        }

        public object SpawnNPC(object self, Dictionary<string, object> args)
        {
            if (!generator.Enabled) return null;
            generator.CreatePirates();
            return null;
        }

        public override void Stop() { }
    }
}

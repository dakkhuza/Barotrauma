using Barotrauma.MoreLevelContent.Shared.Utils;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using MoreLevelContent;
using HarmonyLib;
using MoreLevelContent.Shared.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Barotrauma.MoreLevelContent.Client.UI
{
    public class MapUI : Singleton<MapUI>
    {
        static FieldInfo zoomLevel;
        static FieldInfo tooltipField;
        public override void Setup()
        {
            var drawConnection = typeof(Map).GetMethod("DrawConnection", BindingFlags.NonPublic | BindingFlags.Instance);
            zoomLevel = typeof(Map).GetField("zoom", BindingFlags.Instance | BindingFlags.NonPublic);
            tooltipField = typeof(Map).GetField("tooltip", BindingFlags.Instance | BindingFlags.NonPublic);
            _ = Main.Harmony.Patch(drawConnection, null, new HarmonyMethod(GetType().GetMethod(nameof(OnDrawConnection), BindingFlags.NonPublic | BindingFlags.Static)));
        }

        private static void OnDrawConnection(SpriteBatch spriteBatch, LocationConnection connection, Rectangle viewArea, Vector2 viewOffset, Location currentDisplayLocation, Map __instance)
        {
            LevelData_MLC data = connection.LevelData.MLC();
            if (!data.HasBeaconConstruction) return; // exit if we don't have a construction station
            Vector2? connectionStart = null;
            Vector2? connectionEnd = null;
            Vector2 rectCenter = viewArea.Center.ToVector2();
            int startIndex = connection.CrackSegments.Count > 2 ? 1 : 0;
            int endIndex = connection.CrackSegments.Count > 2 ? connection.CrackSegments.Count - 1 : connection.CrackSegments.Count;
            float zoom = (float)zoomLevel.GetValue(__instance);
            for (int i = startIndex; i < endIndex; i++)
            {
                var segment = connection.CrackSegments[i];
                Vector2 start = rectCenter + (segment[0] + viewOffset) * zoom;
                Vector2 end = rectCenter + (segment[1] + viewOffset) * zoom;
                connectionEnd = end;
                if (!connectionStart.HasValue) { connectionStart = start; }
            }

            DrawIcon("BeaconConst", (int)(28 * zoom), RichString.Rich(TextManager.Get("mlc.beaconconsttooltip")));

            void DrawIcon(string iconStyle, int iconSize, RichString tooltipText)
            {
                Vector2 iconPos = (connectionStart.Value + connectionEnd.Value) / 2;
                Vector2 iconDiff = Vector2.Normalize(connectionEnd.Value - connectionStart.Value) * iconSize;

                iconPos += (iconDiff * -(1 - 1) / 2.0f) + iconDiff * 0;

                var style = GUIStyle.GetComponentStyle(iconStyle);
                bool mouseOn = Vector2.DistanceSquared(iconPos, PlayerInput.MousePosition) < iconSize * iconSize && IsPreferredTooltip(iconPos, __instance);
                Sprite iconSprite = style.GetDefaultSprite();
                iconSprite.Draw(spriteBatch, iconPos, (mouseOn ? style.HoverColor : style.Color) * 0.7f,
                    scale: iconSize / iconSprite.size.X);
                if (mouseOn)
                {
                    tooltipField.SetValue(__instance, (new Rectangle((iconPos - Vector2.One * iconSize / 2).ToPoint(), new Point(iconSize)), tooltipText));
                }
            }

            bool IsPreferredTooltip(Vector2 tooltipPos, Map map) => tooltipField.GetValue(map) == null || Vector2.DistanceSquared(tooltipPos, PlayerInput.MousePosition) < Vector2.DistanceSquared((tooltipField.GetValue(map) as (Rectangle targetArea, RichString tip)?).Value.targetArea.Center.ToVector2(), PlayerInput.MousePosition);
        }
    }
}

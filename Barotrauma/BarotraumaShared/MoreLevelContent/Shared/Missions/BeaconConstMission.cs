using System;
using System.Collections.Generic;
using Barotrauma;
using Microsoft.Xna.Framework;

namespace MoreLevelContent.Missions
{
    partial class BeaconConstMission : Mission
    {
        private readonly LocalizedString sonarLabel;
        public BeaconConstMission(MissionPrefab prefab, Location[] locations, Submarine sub) : base(prefab, locations, sub)
        {
            sonarLabel = TextManager.Get("beaconstationsonarlabel");
        }

        public override LocalizedString SonarLabel
        {
            get
            {
                return base.SonarLabel.IsNullOrEmpty() ? sonarLabel : base.SonarLabel;
            }
        }

        public override IEnumerable<Vector2> SonarPositions
        {
            get
            {
                if (level.BeaconStation == null)
                {
                    yield break;
                }
                yield return level.BeaconStation.WorldPosition;
            }
        }

        protected override void UpdateMissionSpecific(float deltaTime) => base.UpdateMissionSpecific(deltaTime);

        public override void End() => base.End();

        public override void AdjustLevelData(LevelData levelData) => base.AdjustLevelData(levelData);

    }
}
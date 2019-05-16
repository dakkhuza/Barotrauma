﻿using Barotrauma.Items.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Barotrauma.Extensions;

namespace Barotrauma
{
    class AIObjectivePumpWater : AIObjectiveLoop<Pump>
    {
        public override string DebugTag => "pump water";
        private IEnumerable<Pump> pumpList;

        public AIObjectivePumpWater(Character character, AIObjectiveManager objectiveManager, string option, float priorityModifier = 1) 
            : base(character, objectiveManager, priorityModifier, option) { }

        public override bool IsDuplicate(AIObjective otherObjective) => otherObjective is AIObjectivePumpWater && otherObjective.Option == Option;

        protected override void FindTargets()
        {
            if (Option == null) { return; }
            base.FindTargets();
            if (targets.None() && objectiveManager.CurrentOrder == this)
            {
                character.Speak(TextManager.Get("DialogNoPumps"), null, 3.0f, "nopumps", 30.0f);
            }
        }

        protected override bool Filter(Pump pump)
        {
            if (pump == null) { return false; }
            if (pump.Item.HasTag("ballast")) { return false; }
            if (pump.Item.Submarine == null) { return false; }
            if (pump.Item.CurrentHull == null) { return false; }
            if (pump.Item.Submarine.TeamID != character.TeamID) { return false; }
            if (pump.Item.CurrentHull.FireSources.Count > 0 || Character.CharacterList.Any(c => c.CurrentHull == pump.Item.CurrentHull && !HumanAIController.IsFriendly(c))) { return false; }
            if (pump.Item.ConditionPercentage <= 0) { return false; }
            if (character.Submarine != null && !character.Submarine.IsEntityFoundOnThisSub(pump.Item, true)) { return false; }
            if (Option == "stoppumping")
            {
                if (!pump.IsActive || MathUtils.NearlyEqual(pump.FlowPercentage, 0)) { return false; }
            }
            else
            {
                if (!pump.Item.InWater) { return false; }
                if (pump.IsActive && pump.FlowPercentage <= -99.9f) { return false; }
            }
            return true;
        }
        protected override IEnumerable<Pump> GetList()
        {
            if (pumpList == null)
            {
                pumpList = character.Submarine.GetItems(true).Select(i => i.GetComponent<Pump>()).Where(p => p != null);
            }
            return pumpList;
        }

        protected override AIObjective ObjectiveConstructor(Pump pump) => new AIObjectiveOperateItem(pump, character, objectiveManager, Option, false) { IsLoop = false };
        protected override float TargetEvaluation()
        {
            if (Option == "stoppumping")
            {
                return targets.Max(t => MathHelper.Lerp(0, 100, Math.Abs(t.FlowPercentage / 100)));
            }
            else
            {
                return targets.Max(t => MathHelper.Lerp(100, 0, Math.Abs(-t.FlowPercentage / 100)));
            }
        }
    }
}

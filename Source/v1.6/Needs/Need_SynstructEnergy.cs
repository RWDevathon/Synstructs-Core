using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArtificialBeings
{
    public class Need_SynstructEnergy : Need_Artificial
    {
        private float FallRateModifierPerStage
        {
            get
            {
                if (CurLevelPercentage < 0.1f)
                {
                    return 0.25f;
                }
                else if (CurLevelPercentage < 0.2f)
                {
                    return 0.5f;
                }
                return 1f;
            }
        }

        public Need_SynstructEnergy(Pawn pawn) : base(pawn)
        {
        }

        // IsFrozen is protected instead of public.
        public virtual bool IsStopped => IsFrozen;

        public override void SetInitialLevel()
        {
            CurLevelPercentage = 1.0f;
        }

        // Fall rate per day depends on the Energy Consumption statistic.
        public override float FallRatePerDay
        {
            get
            {
                return pawn.GetStatValue(ABF_StatDefOf.ABF_Stat_Synstruct_EnergyConsumption, cacheStaleAfterTicks: 400) * FallRateModifierPerStage;
            }
        }

        public override float MaxLevel
        {
            get
            {
                if (Current.ProgramState != ProgramState.Playing)
                {
                    return pawn.BodySize * 200f;
                }
                return pawn.GetStatValue(ABF_StatDefOf.ABF_Stat_Synstruct_MaxEnergy, cacheStaleAfterTicks: 400);
            }
        }

        public override int GUIChangeArrow
        {
            get
            {
                if (IsFrozen)
                {
                    return 0;
                }
                return -1;
            }
        }

        // Add little tick markers to the points where the need starts falling slower.
        public override void DrawOnGUI(Rect rect, int maxThresholdMarkers = int.MaxValue, float customMargin = -1, bool drawArrows = true, bool doTooltip = true, Rect? rectForTooltip = null, bool drawLabel = true)
        {
            if (threshPercents == null)
            {
                threshPercents = new List<float>();
            }
            threshPercents.Clear();
            threshPercents.Add(0.1f);
            threshPercents.Add(0.2f);
            base.DrawOnGUI(rect, maxThresholdMarkers, customMargin, drawArrows, doTooltip, rectForTooltip, drawLabel);
        }

        // Pawns should try to replenish the energy need via resevoirs before trying to find an item that fulfills this need.
        public override Job GetReplenishJob()
        {
            if (SC_Utils.GetReservoir(pawn) is Thing reservoir)
            {
                return new Job(ABF_JobDefOf.ABF_Job_Synstruct_ChargeSelf, new LocalTargetInfo(reservoir));
            }
            if (SC_Utils.GetAccessPoint(pawn) is Thing point)
            {
                return new Job(ABF_JobDefOf.ABF_Job_Synstruct_ChargeSelf, new LocalTargetInfo(point));
            }
            return base.GetReplenishJob();
        }

        // Pawns should try to replenish the energy need via siphoning from other pawns (that have sufficient reserves) before trying to find an item that fulfills this need.
        public override void TryReplenishNeedInCaravan(Caravan caravan)
        {
            // Assemble a list of potential siphoning targets, ordered by highest to least charge (as a percentage).
            List<Need_SynstructEnergy> siphonTargets = new List<Need_SynstructEnergy>();
            EnergyComparer comparer = new EnergyComparer();
            foreach (Pawn siphonTarget in caravan.PawnsListForReading)
            {
                if (pawn != siphonTarget && siphonTarget.def.GetModExtension<ABF_SynstructExtension>()?.mayHaveEnergySiphoned == true && siphonTarget.needs.TryGetNeed(def) is Need_SynstructEnergy siphonableNeed && siphonableNeed.CurLevelPercentage > CurLevelPercentage && !siphonableNeed.ShouldReplenishNow() && siphonableNeed.CurLevelPercentage > NeedExtension.criticalThreshold + 0.05f)
                {
                    siphonTargets.InsertIntoSortedList(siphonableNeed, comparer);
                }
            }
            
            // Iterate until full or until there are no more valid targets.
            while (AmountDesired > 0 && siphonTargets.Count > 0)
            {
                // If we have an available target, transfer some amount of energy - no more than 5% of the target's max capacity. 
                int targetInd = siphonTargets.Count - 1;
                Need_SynstructEnergy target = siphonTargets[targetInd];
                float amountToTransfer = Mathf.Min(AmountDesired, target.MaxLevel * 0.05f);
                CurLevel += amountToTransfer;
                target.CurLevel -= amountToTransfer;

                // If the target now has too little to spare now, remove it from the list.
                if (target.CurLevelPercentage <= NeedExtension.criticalThreshold + 0.05f || target.ShouldReplenishNow())
                {
                    siphonTargets.RemoveAt(targetInd);
                }
                // If it has enough to spare and there are other elements, force a resort of the target.
                else if (siphonTargets.Count > 1)
                {
                    siphonTargets.RemoveAt(targetInd);
                    siphonTargets.InsertIntoSortedList(target, comparer);
                }

                // If, after siphoning, the highest with energy to spare has less percentage than we do or there's none left to siphon from, then we've charged all we should.
                if (siphonTargets.Count == 0 || siphonTargets[siphonTargets.Count - 1].CurLevelPercentage < CurLevelPercentage)
                {
                    break;
                }
            }

            // In case insufficient charge was achieved, try to find an item that fulfills the need.
            if (ShouldReplenishNow())
            {
                base.TryReplenishNeedInCaravan(caravan);
            }
        }

        private class EnergyComparer : Comparer<Need_SynstructEnergy>
        {
            public override int Compare(Need_SynstructEnergy x, Need_SynstructEnergy y)
            {
                return x.CurLevelPercentage.CompareTo(y.CurLevelPercentage);
            }
        }
    }
}

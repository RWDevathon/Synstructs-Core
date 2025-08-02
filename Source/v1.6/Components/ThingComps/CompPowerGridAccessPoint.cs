using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace ArtificialBeings
{
    public class CompPowerGridAccessPoint : ThingComp
    {
        public CompPowerTrader compPowerTrader;

        public CompProperties_PowerGridAccessPoint Props
        {
            get
            {
                return props as CompProperties_PowerGridAccessPoint;
            }
        }

        public bool Usable => compPowerTrader?.PowerOn == true && EnergyAvailable > 0;

        public float EnergyAvailable => compPowerTrader?.PowerNet?.CurrentStoredEnergy() ?? 0f;

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }
            if (!Usable)
            {
                yield return new FloatMenuOption("CannotUseNoPower".Translate(), null);
            }
            // Check if the pawn can reach the building.
            else if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            }
            // Check if the pawn can charge.
            else if (!SC_Utils.CanCharge(selPawn))
            {
                yield return new FloatMenuOption("ABF_IncapableOfCharging".Translate(selPawn), null);
            }
            else
            {
                yield return new FloatMenuOption("ABF_ForceCharge".Translate(), delegate () {
                    Job job = new Job(ABF_JobDefOf.ABF_Job_Synstruct_ChargeSelf, new LocalTargetInfo(parent));
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        public override IEnumerable<FloatMenuOption> CompMultiSelectFloatMenuOptions(IEnumerable<Pawn> selPawns)
        {
            foreach (FloatMenuOption option in base.CompMultiSelectFloatMenuOptions(selPawns))
            {
                yield return option;
            }
            if (!Usable)
            {
                yield return new FloatMenuOption("CannotUseNoPower".Translate(), null);
                yield break;
            }

            List<Pawn> usableFor = new List<Pawn>();
            foreach (Pawn pawn in selPawns)
            {
                if (pawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly) && SC_Utils.CanCharge(pawn))
                {
                    usableFor.Add(pawn);
                }
            }
            if (!usableFor.NullOrEmpty())
            {
                yield return new FloatMenuOption("ABF_ForceCharge".Translate(), delegate () {
                    foreach (Pawn pawn in usableFor)
                    {
                        Job job = new Job(ABF_JobDefOf.ABF_Job_Synstruct_ChargeSelf, new LocalTargetInfo(parent));
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }
                });
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compPowerTrader = parent.GetComp<CompPowerTrader>();
            SC_MapComponent mapComponent = parent.Map.GetComponent<SC_MapComponent>();
            mapComponent.accessPoints.Add(parent);
        }

        public override void PostDeSpawn(Map map, DestroyMode destroyMode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, destroyMode);
            SC_MapComponent mapComponent = map.GetComponent<SC_MapComponent>();
            mapComponent.accessPoints.Remove(parent);
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            SC_MapComponent mapComponent = previousMap.GetComponent<SC_MapComponent>();
            mapComponent.accessPoints.Remove(parent);
        }

        public override void Notify_MapRemoved()
        {
            base.Notify_MapRemoved();
            SC_MapComponent mapComponent = parent.Map.GetComponent<SC_MapComponent>();
            mapComponent.reservoirs.Remove(parent);
        }
    }
}
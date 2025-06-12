﻿using Verse;
using Verse.AI;
using HarmonyLib;
using RimWorld;

namespace ArtificialBeings
{
    public class JobGiver_PatientGoToBed_Patch
    {
        // Override job for being a patient based on whether the pawn can charge and the target bed is charge-capable.
        [HarmonyPatch(typeof(JobGiver_PatientGoToBed), "TryGiveJob")]
        public class JobGiver_PatientGoToBed_TryGiveJob_Patch
        {
            [HarmonyPostfix]
            public static void Listener(Pawn pawn, ref Job __result)
            {
                if (__result == null || !(__result.targetA.Thing is ThingWithComps bed) || !SC_Utils.CanCharge(pawn))
                    return;

                if (bed.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading.Any(thing => thing.HasComp<CompPawnCharger>() && (thing.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)) ?? false)
                {
                    __result = JobMaker.MakeJob(ABF_JobDefOf.ABF_Job_Synstruct_ChargeSelf, __result.targetA.Thing);
                }
            }
        }
    }
}
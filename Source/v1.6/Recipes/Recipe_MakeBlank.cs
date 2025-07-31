using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ArtificialBeings
{
    public class Recipe_MakeBlank : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn) || !base.AvailableOnNow(pawn) || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            ABF_ArtificialState state = ABF_Utils.PawnStateFor(pawn);
            return state != ABF_ArtificialState.Unknown && state != ABF_ArtificialState.Blank;
        }

        public override bool CompletableEver(Pawn surgeryTarget)
        {
            if (!(surgeryTarget is Pawn pawn) || !base.AvailableOnNow(pawn) || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            ABF_ArtificialState state = ABF_Utils.PawnStateFor(pawn);
            return state != ABF_ArtificialState.Unknown && state != ABF_ArtificialState.Blank;
        }

        // Making a pawn blank has special considerations. The state-change in the comp handles most of it, and duplicating the blank pawn in beforehand will handle the rest.
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

            if (pawn.RaceProps.Humanlike)
            {
                SC_Utils.Duplicate(SC_Utils.GetBlank(), pawn, ABF_Utils.IsArtificialSapient(pawn));
            }
            pawn.GetComp<CompArtificialPawn>().State = ABF_ArtificialState.Blank;

            // Ensure the pawn is disabled as it is blank.
            pawn.health.AddHediff(ABF_HediffDefOf.ABF_Hediff_Artificial_Disabled);

            // Remove the incapacitated condition if it is present.
            if (pawn.health.hediffSet.GetFirstHediffOfDef(ABF_HediffDefOf.ABF_Hediff_Artificial_Incapacitated) is Hediff hediff)
            {
                pawn.health.RemoveHediff(hediff);
            }
        }
    }
}

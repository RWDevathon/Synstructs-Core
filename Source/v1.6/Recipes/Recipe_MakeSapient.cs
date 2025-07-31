using System.Collections.Generic;
using Verse;
using RimWorld;

namespace ArtificialBeings
{
    public class Recipe_MakeSapient : Recipe_Surgery
    {
        public override bool AvailableOnNow(Thing thing, BodyPartRecord part = null)
        {
            if (!(thing is Pawn pawn) || !base.AvailableOnNow(pawn) || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            return ABF_Utils.IsArtificialBlank(pawn) && pawn.def.GetModExtension<ABF_ArtificialPawnExtension>()?.canBeSapient == true;
        }

        public override bool CompletableEver(Pawn surgeryTarget)
        {
            if (!(surgeryTarget is Pawn pawn) || !base.AvailableOnNow(pawn) || !pawn.RaceProps.Humanlike)
            {
                return false;
            }

            return ABF_Utils.IsArtificialBlank(pawn) && pawn.def.GetModExtension<ABF_ArtificialPawnExtension>()?.canBeSapient == true;
        }

        // Sapients need to have their intelligence generated and appropriate story and skills populated.
        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            base.ApplyOnPawn(pawn, part, billDoer, ingredients, bill);

            pawn.GetComp<CompArtificialPawn>().State = ABF_ArtificialState.Sapient;

            // Create a pawn that has some age and appropriate features to duplicate into the blank to act as its new intelligence.
            ABF_SynstructExtension synstructExtension = pawn.def.GetModExtension<ABF_SynstructExtension>();
            PawnKindDef pawnKindDef = synstructExtension.playerSapientPawnKindDef ?? pawn.kindDef;

            if (synstructExtension.playerSapientPawnKindDef?.GetModExtension<ABF_ArtificialPawnKindExtension>() is ABF_ArtificialPawnKindExtension pawnKindExtension && pawnKindExtension.pawnState != ABF_ArtificialState.Sapient)
            {
                Log.Warning($"[ABF] Operation to make a pawn into a sapient encountered an issue: {pawnKindDef.LabelCap} is not a pawn kind def for sapients!");
            }

            if (pawnKindDef.race != pawn.def)
            {
                Log.Error($"[ABF] Operation to make a pawn with race {pawn.def.defName} into a drone was given race {pawnKindDef.race.defName} to copy from! Severe errors may occur.");
            }
            else
            {
                pawn.kindDef = pawnKindDef;
            }

            // Incapacitated the unit for a small amount of time.
            Hediff rebootHediff = pawn.health.hediffSet.GetFirstHediffOfDef(ABF_HediffDefOf.ABF_Hediff_Artificial_Incapacitated);
            if (rebootHediff == null)
            {
                rebootHediff = HediffMaker.MakeHediff(ABF_HediffDefOf.ABF_Hediff_Artificial_Incapacitated, pawn, null);
                pawn.health.AddHediff(rebootHediff);
            }
            rebootHediff.Severity = 1;

            PawnGenerationRequest request = new PawnGenerationRequest(pawnKindDef, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowAddictions: false, fixedBiologicalAge: 50, forceNoIdeo: billDoer.ideo == null, fixedIdeo: billDoer.Ideo, colonistRelationChanceFactor: 0, forceBaselinerChance: 1f);
            Pawn personality = PawnGenerator.GeneratePawn(request);
            SC_Utils.Duplicate(personality, pawn, false);

            // Create a pawn relation between the creator and creation.
            if (billDoer != null)
            {
                billDoer.relations?.AddDirectRelation(SC_PawnRelationDefOf.ABF_PawnRelation_Synstruct_Creation, pawn);
                pawn.relations?.AddDirectRelation(SC_PawnRelationDefOf.ABF_PawnRelation_Synstruct_Creator, billDoer);
            }

            // Allow the player to pick a few passions and a trait for the new synstruct, akin to child growth moments in Biotech.
            if (ModLister.BiotechInstalled)
            {
                ChoiceLetter_PersonalityShift choiceLetter = (ChoiceLetter_PersonalityShift)LetterMaker.MakeLetter(ABF_LetterDefOf.ABF_Letter_Synstruct_PersonalityShift);
                choiceLetter.ConfigureChoiceLetter(pawn, 3, 3, false, false);
                choiceLetter.Label = "ABF_PersonalityShiftNewboot".Translate();
                choiceLetter.disappearAtTick = Find.TickManager.TicksGame;
                choiceLetter.OpenLetter();
            }
        }
    }
}


using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ArtificialBeings
{
    // Slot upgrades occupy the slot they are in, and are mutually exclusive with all other Hediffs of the same class on that part.
    // This is enforced at the recipe level alone, so it's possible that there is more than one implant there anyway. Nothing we can do about that.
    public class Recipe_RemoveSlotUpgrade : Recipe_Surgery
    {
        public override IEnumerable<BodyPartRecord> GetPartsToApplyOn(Pawn pawn, RecipeDef recipe)
        {
            foreach (Hediff hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_Implant && !(hediff is Hediff_AddedPart) && hediff.Part != null)
                {
                    yield return hediff.Part;
                }
            }
        }

        public override void ApplyOnPawn(Pawn pawn, BodyPartRecord part, Pawn billDoer, List<Thing> ingredients, Bill bill)
        {
            if (billDoer != null)
            {
                if (CheckSurgeryFail(billDoer, pawn, ingredients, part, bill))
                {
                    return;
                }
                TaleRecorder.RecordTale(TaleDefOf.DidSurgery, billDoer, pawn);
                Hediff hediff = pawn.health.hediffSet.hediffs.FirstOrDefault((Hediff x) => x is Hediff_Implant && x.Part == part);
                if (hediff != null)
                {
                    if (hediff.def.spawnThingOnRemoved != null)
                    {
                        GenSpawn.Spawn(hediff.def.spawnThingOnRemoved, billDoer.Position, billDoer.Map);
                    }
                    pawn.health.RemoveHediff(hediff);
                }
            }
            if (IsViolationOnPawn(pawn, part, Faction.OfPlayer))
            {
                ReportViolation(pawn, billDoer, pawn.HomeFaction, -70);
            }
        }
    }
}


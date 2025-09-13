using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ArtificialBeings
{
    // A simple ThingComp designed to add the necessary trackers and gizmo's for synstruct animals to allow drafting.
    public class CompDraftableSynstructAnimal : ThingComp
    {
        private Pawn_DraftController drafter;

        private Pawn Pawn => parent as Pawn;

        private Pawn_DraftController Drafter
        {
            get
            {
                if (drafter == null)
                {
                    if (Pawn.drafter == null)
                    {
                        Pawn.drafter = new Pawn_DraftController(Pawn);
                    }
                    drafter = Pawn.drafter;
                }
                // If the drafter exists, the equipment tracker also needs to exist, but isn't referenced directly.
                if (Pawn.equipment == null)
                {
                    Pawn.equipment = new Pawn_EquipmentTracker(Pawn);
                }
                return drafter;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (Drafter == null)
            {
                Log.ErrorOnce("[ABF] A draftable animal failed to generate its draft controller.", 912831);
            }
            base.PostSpawnSetup(respawningAfterLoad);
        }

        // The pawn can lose its draft controller on despawn, and that's fine, so long as we don't continue to hold a reference to it.
        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            drafter = null;
        }

        // The extra selection overlay for drawing lines for paths only happens for Colonist pawns (ie. humanlikes).
        public override void PostDrawExtraSelectionOverlays()
        {
            if (Pawn.Faction == Faction.OfPlayer)
            {
                Pawn.pather.curPath?.DrawPath(Pawn);
                Pawn.jobs.DrawLinesBetweenTargets();
            }
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Pawn.Faction == Faction.OfPlayer)
            {
                if (Drafter.ShowDraftGizmo)
                {
                    Command_Toggle command_Toggle = new Command_Toggle
                    {
                        hotKey = KeyBindingDefOf.Command_ColonistDraft,
                        isActive = () => Drafter.Drafted,
                        toggleAction = delegate
                        {
                            Drafter.Drafted = !Drafter.Drafted;
                        },
                        defaultDesc = "CommandToggleDraftDesc".Translate(),
                        icon = TexCommand.Draft,
                        turnOnSound = SoundDefOf.DraftOn,
                        turnOffSound = SoundDefOf.DraftOff,
                        groupKeyIgnoreContent = 81729172,
                        defaultLabel = (Drafter.Drafted ? "CommandUndraftLabel" : "CommandDraftLabel").Translate()
                    };
                    if (Pawn.Downed)
                    {
                        command_Toggle.Disable("IsIncapped".Translate(Pawn.LabelShort, Pawn));
                    }
                    yield return command_Toggle;
                }
                foreach (Gizmo attackGizmo in PawnAttackGizmoUtility.GetAttackGizmos(Pawn))
                {
                    yield return attackGizmo;
                }
            }
        }
    }
}

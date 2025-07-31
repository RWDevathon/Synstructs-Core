using Verse.AI;
using Verse;
using System.Collections.Generic;
using RimWorld;

namespace ArtificialBeings
{
    public class Directive_Minuteman : Directive
    {
        public bool Deactivated
        {
            get
            {
                return pawn.CurJobDef == JobDefOf.Wait_AsleepDormancy;
            }
            set
            {
                if (value == Deactivated)
                {
                    return;
                }
                if (pawn.Spawned)
                {
                    if (Deactivated)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                    else
                    {
                        Job job = JobMaker.MakeJob(JobDefOf.Wait_AsleepDormancy, pawn.Position);
                        job.forceSleep = true;
                        pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                    }
                }
            }
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            Command_Toggle command_Toggle = new Command_Toggle
            {
                isActive = () => !Deactivated,
                toggleAction = delegate
                {
                    Deactivated = !Deactivated;
                },
                defaultDesc = "ABF_CommandToggleActivationDesc".Translate(),
                icon = def.Icon,
                turnOnSound = SoundDefOf.DraftOn,
                turnOffSound = SoundDefOf.DraftOff,
                groupKeyIgnoreContent = 20831048,
                defaultLabel = "ABF_CommandActiveLabel".Translate()
            };
            if (pawn.Downed)
            {
                command_Toggle.Disable("IsIncapped".Translate(pawn.LabelShort, pawn));
            }
            if (pawn.Deathresting)
            {
                command_Toggle.Disable("IsDeathresting".Translate(pawn.Named("PAWN")));
            }
            yield return command_Toggle;
        }

        public override IEnumerable<string> GetCompInspectStrings()
        {
            if (Deactivated)
            {
                yield return "ABF_MinutemanDormant".Translate();
            }
            yield break;
        }
    }
}

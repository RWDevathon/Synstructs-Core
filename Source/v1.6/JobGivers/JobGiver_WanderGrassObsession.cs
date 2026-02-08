using Verse;
using Verse.AI;

namespace ArtificialBeings
{
    // Give a job to wander near grass, and also plants I guess.
    public class JobGiver_WanderGrassObsession : JobGiver_Wander
    {
        public JobGiver_WanderGrassObsession()
        {
            wanderRadius = 2f;
            ticksBetweenWandersRange = new IntRange(240, 360);
        }

        protected override Job TryGiveJob(Pawn pawn)
        {
            Map map = pawn.Map;
            if (map.listerThings.ThingsInGroup(ThingRequestGroup.NonStumpPlant).Count == 0)
            {
                return null;
            }
            return base.TryGiveJob(pawn);
        }

        protected override IntVec3 GetWanderRoot(Pawn pawn)
        {
            Map map = pawn.Map;
            return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.NonStumpPlant), PathEndMode.Touch, TraverseParms.For(pawn)).Position;
        }
    }
}

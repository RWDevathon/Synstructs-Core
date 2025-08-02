using System.Collections.Generic;
using Verse;

namespace ArtificialBeings
{
    public class SC_MapComponent : MapComponent
    {
        public HashSet<CompRoomCharger> chargers = new HashSet<CompRoomCharger>();

        public HashSet<ThingWithComps> reservoirs = new HashSet<ThingWithComps>();

        public HashSet<ThingWithComps> accessPoints = new HashSet<ThingWithComps>();

        public SC_MapComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (map.IsHashIntervalTick(GenTicks.TickRareInterval))
            {
                SC_Utils.UpdateRoomChargers(chargers);
            }
            if (map.IsHashIntervalTick(GenTicks.TickLongInterval))
            {
                SC_Utils.UpdateAmicableDroneCount(map);
                SC_Utils.UpdatePlayerOrganicPawnCount(map);
                SC_Utils.UpdatePlayerBiomimeticCount(map);
            }
        }
    }
}

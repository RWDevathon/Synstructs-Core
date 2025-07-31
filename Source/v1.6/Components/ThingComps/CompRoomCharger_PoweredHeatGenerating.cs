using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ArtificialBeings
{
    public class CompRoomCharger_PoweredHeatGenerating : CompRoomCharger
    {
        protected CompPowerTrader compPowerTrader;

        protected float cachedPowerConsumption = 0f;

        protected float cachedHeatGeneration = 0f;

        protected float cachedPawnEnergyConsumption = 0f;

        protected int cachedPawnsInRoomCount = 0;

        protected int cachedActiveChargersInRoomCount = 0;

        private List<Pawn> chargingPawns;

        protected virtual float PowerConsumptionModifier
        {
            get
            {
                return Mathf.Max((0.125f * (cachedPawnsInRoomCount - cachedActiveChargersInRoomCount) / cachedActiveChargersInRoomCount) + 1f, 1f);
            }
        }

        public override bool Active
        {
            get
            {
                if (compPowerTrader?.PowerOn == true)
                {
                    return true;
                }
                return false;
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            compPowerTrader = parent.GetComp<CompPowerTrader>();
            base.PostSpawnSetup(respawningAfterLoad);
        }

        // Using the provided room and lists of chargers and pawns in the room, calculate the effects of charging. baselineOnly being true implies the device should be treated as inoperable.
        public override void RecalculateCaches(Room parentRoom, List<CompRoomCharger> roomChargers, List<Pawn> roomPawns, bool baselineOnly = false)
        {
            // Recalculating the cache should store the list of valid charging pawns for the purpose of the pulse.
            chargingPawns = roomPawns;
            if (baselineOnly || roomChargers.NullOrEmpty() || roomPawns.NullOrEmpty())
            {
                cachedPowerConsumption = Mathf.Max(compPowerTrader.Props.PowerConsumption, 0f);
                cachedHeatGeneration = 0f;
                cachedPawnEnergyConsumption = 0f;
                cachedPawnsInRoomCount = 0;
                cachedActiveChargersInRoomCount = 0;
                return;
            }

            cachedActiveChargersInRoomCount = 0;
            // Count only chargers which are active.
            for (int i = roomChargers.Count - 1; i >= 0; i--)
            {
                if (roomChargers[i].Active)
                {
                    cachedActiveChargersInRoomCount++;
                }
            }
            if (cachedActiveChargersInRoomCount == 0)
            {
                return;
            }

            cachedPawnsInRoomCount = roomPawns.Count;
            cachedPawnEnergyConsumption = 0;
            foreach (Pawn pawn in roomPawns)
            {
                cachedPawnEnergyConsumption += GetChargePerDayForPawn(pawn);
            }
            float powerConsumptionModifier = PowerConsumptionModifier;
            cachedPowerConsumption = cachedPawnEnergyConsumption * powerConsumptionModifier;
            cachedHeatGeneration = 0.005f * cachedPowerConsumption; // Generate one heat per second for every 200 Watts of power consumption.
        }

        public override void DoChargePulse()
        {
            // Do nothing if not spawned.
            if (!parent.Spawned)
            {
                return;
            }

            // Update the power comp so it remains synced, regardless of whether the device is operating.
            compPowerTrader.PowerOutput = -cachedPowerConsumption;

            // Does nothing if flicked off, unpowered, or there are no pawns to charge.
            if (!Active || chargingPawns.NullOrEmpty())
            {
                return;
            }

            Room currentRoom = parent.Map?.regionGrid.GetValidRegionAt_NoRebuild(parent.Position)?.Room;
            if (currentRoom == null || !currentRoom.ProperRoom)
            {
                return;
            }

            // The amount to replenish depends on the tick rate of the device. A longer tick interval means it is being called less, and should charge more to compensate.
            // The parent comp treats normal ticker types as Rare, so Rare is the shortest acceptable interval.
            int effectiveTicks = parent.def.tickerType == TickerType.Long ? GenTicks.TickLongInterval : GenTicks.TickRareInterval;

            // The heat pusher output is not modifiable. Simply push the cached heat (or cold) if necessary.
            if (cachedHeatGeneration != 0f)
            {
                GenTemperature.PushHeat(parent.PositionHeld, parent.MapHeld, cachedHeatGeneration * (GenTicks.SecondsPerTick * effectiveTicks));
            }

            foreach (Pawn pawn in chargingPawns)
            {
                if (pawn.needs?.TryGetNeed(ABF_NeedDefOf.ABF_Need_Synstruct_Energy) is Need_SynstructEnergy need)
                {
                    need.CurLevel += GetChargePerDayForPawn(pawn) / GenDate.TicksPerDay * effectiveTicks;
                }
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!chargingPawns.NullOrEmpty() && chargingPawns.Count > 1)
            {
                return "ABF_ReducedEfficiency".Translate(Props.maxChargeRatePerPawnPerDay, (1 / PowerConsumptionModifier).ToStringPercent("0.#"));
            }
            return base.CompInspectStringExtra();
        }

        // Depending on when this is called, it may differ between when used to recalculate caches versus doing charge pulses, due to chargers going offline or pawn energy consumption changing.
        public override float GetChargePerDayForPawn(Pawn pawn)
        {
            float toChargeForPawn = pawn.GetStatValue(ABF_StatDefOf.ABF_Stat_Synstruct_EnergyConsumption);
            // If the pawn has less than 90% of its need filled, this building should attempt to charge it up.
            if (pawn.needs.TryGetNeed(ABF_NeedDefOf.ABF_Need_Synstruct_Energy) is Need_SynstructEnergy energyNeed && energyNeed.CurLevelPercentage < 0.9f)
            {
                toChargeForPawn *= 1.5f;
            }
            // Charging should be split evenly between chargers, and should never exceed this building's maximum charge rate per pawn.
            return Mathf.Min(toChargeForPawn / Mathf.Max(cachedActiveChargersInRoomCount, 1), Props.maxChargeRatePerPawnPerDay);
        }
    }
}
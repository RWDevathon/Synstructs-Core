using RimWorld;
using UnityEngine;
using Verse;

namespace ArtificialBeings
{
    public class CompEnergyReservoir_Powered : CompEnergyReservoir
    {
        private CompPowerTrader compPowerTrader;

        public override bool Usable => CompPowerTrader.PowerOn && base.Usable;

        public CompPowerTrader CompPowerTrader
        {
            get
            {
                if (compPowerTrader == null)
                {
                    compPowerTrader = parent.GetComp<CompPowerTrader>();
                }
                return compPowerTrader;
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            if (CompPowerTrader.PowerOn)
            {
                reserve = Mathf.Clamp(reserve + Mathf.Abs(compPowerTrader.PowerOutput * Props.energyEfficiency * GenTicks.TickRareInterval) / GenDate.TicksPerDay, 0, Props.maximumReserve);
            }
            // If we are at maximum reserve, keep the power consumption much lower.
            if (reserve >= Props.maximumReserve)
            {
                CompPowerTrader.PowerOutput = (0 - CompPowerTrader.Props.PowerConsumption) * 0.1f;
            }
            else
            {
                CompPowerTrader.PowerOutput = 0 - CompPowerTrader.Props.PowerConsumption;
            }
        }

        // Energized reservoirs release all energy as an EMP upon taking damage.
        public override void ReactToDamage(DamageInfo dinfo)
        {
            if (parent.HitPoints > 0 && parent.Spawned)
            {
                // Only explode if there is a decent amount of energy in reserve.
                if (reserve > Props.maximumReserve * 0.2f)
                {
                    GenExplosion.DoExplosion(parent.Position, parent.Map, reserve / 500, DamageDefOf.EMP, parent, (int)reserve / 40);
                }
                reserve = 0;
            }
        }
    }
}
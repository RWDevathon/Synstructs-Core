using System.Collections.Generic;
using System.Diagnostics;
using Verse.AI;
using Verse;
using System;
using UnityEngine;
using RimWorld;

namespace ArtificialBeings
{
    public class JobDriver_RechargeSelf : JobDriver
    {
        public ThingWithComps Reservoir => job.GetTarget(TargetIndex.A).Thing as ThingWithComps;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        [DebuggerHidden]
        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
            Toil toil = Toils_General.Wait(600);
            toil.WithProgressBarToilDelay(TargetIndex.A);
            toil.FailOnDespawnedOrNull(TargetIndex.A);
            toil.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            yield return toil;
            yield return Toils_General.Do(new Action(Recharge));
        }

        // Replenish charge and drain the target
        private void Recharge()
        {
            if (!(pawn.needs.TryGetNeed(ABF_NeedDefOf.ABF_Need_Synstruct_Energy) is Need_SynstructEnergy need))
            {
                return;
            }

            if (Reservoir is ThingWithComps thing)
            {
                // Charging from reservoirs
                if (thing.GetComp<CompEnergyReservoir>() is CompEnergyReservoir reservoir)
                {
                    float toCharge = Mathf.Min(reservoir.reserve, need.AmountDesired);
                    need.CurLevel += toCharge;
                    reservoir.reserve -= toCharge;
                }

                // No need to try accessing the power grid if the reservoir was sufficient.
                if (need.AmountDesired <= 0)
                {
                    return;
                }

                // Charging from power grid access points
                if (thing.GetComp<CompPowerGridAccessPoint>() is CompPowerGridAccessPoint accessPoint)
                {
                    float chargeEfficiency = accessPoint.Props.energyEfficiency;
                    float modifiedDesire = need.AmountDesired / chargeEfficiency;
                    if (modifiedDesire > accessPoint.EnergyAvailable)
                    {
                        need.CurLevel += accessPoint.EnergyAvailable / chargeEfficiency;
                        foreach (CompPowerBattery compPowerBattery in accessPoint.compPowerTrader.PowerNet.batteryComps)
                        {
                            compPowerBattery.DrawPower(compPowerBattery.StoredEnergy);
                        }
                    }
                    else
                    {
                        need.CurLevelPercentage = 1f;
                        List<CompPowerBattery> powerBatteryList = accessPoint.compPowerTrader.PowerNet.batteryComps;
                        powerBatteryList.Shuffle();
                        foreach (CompPowerBattery compPowerBattery in powerBatteryList)
                        {
                            float energyAvailable = compPowerBattery.StoredEnergy;
                            if (energyAvailable < modifiedDesire)
                            {
                                modifiedDesire -= energyAvailable;
                                compPowerBattery.DrawPower(energyAvailable);
                            }
                            else
                            {
                                compPowerBattery.DrawPower(modifiedDesire);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}
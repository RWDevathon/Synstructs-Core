using System.Collections.Generic;
using Verse;

namespace ArtificialBeings
{
    public class CompProperties_PowerGridAccessPoint : CompProperties
    {
        public CompProperties_PowerGridAccessPoint()
        {
            compClass = typeof(CompPowerGridAccessPoint);
        }

        public float energyEfficiency = 1f;

        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            foreach (string error in base.ConfigErrors(parentDef))
            {
                yield return error;
            }

            if (energyEfficiency <= 0)
            {
                yield return $"{parentDef.defName} specified an efficiency of zero or less for its ability to provide charge from the grid. This will cause errors.";
            }
        }
    }
}
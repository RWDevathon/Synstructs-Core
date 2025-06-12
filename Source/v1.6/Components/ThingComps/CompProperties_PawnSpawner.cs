﻿using Verse;

namespace ArtificialBeings
{
    public class CompProperties_PawnSpawner : CompProperties
    {
        public CompProperties_PawnSpawner()
        {
            compClass = typeof(CompPawnSpawner);
        }

        public PawnKindDef pawnKind;

        public MentalStateDef mentalStateOnSpawn;

        public HediffDef hediffOnSpawn;
    }
}
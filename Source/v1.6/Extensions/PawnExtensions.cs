﻿using Verse;

namespace ArtificialBeings
{
    // Simple mod extension that marks the race as a synstruct and holds data for what pawn kinds should be applied when becoming a drone/sapient.
    public class ABF_SynstructExtension : DefModExtension
    {
        // These pawn kind defs will be used as part of the operations to switch the pawn states, to control details like backstories, skills, traits, etc.
        // Do NOT provide any of these with a pawn kind def that has a different race than the race the def mod extension is applied to!
        public PawnKindDef playerDronePawnKindDef;
        public PawnKindDef playerReprogrammableDronePawnKindDef;
        public PawnKindDef playerSapientPawnKindDef;
    }

    // Mod extension for particular races to mark them as something that should explode on downed or death.
    public class ABF_DetonateOnIncapacitation : DefModExtension
    {
        public float explosionRadius = 0.5f;
        public DamageDef damageType;
        public int damageAmount;

        public ThingDef itemToSpawnOnDetonation;
        public int itemCountToSpawn;
    }
}
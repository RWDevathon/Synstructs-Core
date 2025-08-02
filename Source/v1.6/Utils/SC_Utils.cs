using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace ArtificialBeings
{
    public static class SC_Utils
    {
        // Cached pawn that represents a blank pawn to be used for various duplication and modification tasks.
        private static Pawn blankPawn;

        // GENERAL UTILITIES
        public static bool IsSynstruct(Pawn pawn)
        {
            return cachedSynstructs.Contains(pawn.def);
        }

        /* === POWER UTILITIES === */
        public static bool CanCharge(Pawn pawn)
        {
            return pawn.needs.TryGetNeed(ABF_NeedDefOf.ABF_Need_Synstruct_Energy) != null;
        }

        // Locate the nearest available charging bed for the given pawn user, as carried by the given pawn carrier. Pawns may carry themselves here, if they are not downed.
        public static Building_Bed GetChargingBed(Pawn user, Pawn carrier)
        {
            if (user.Map == null)
                return null;

            // Downed pawns in beds may count their current bed as a charging bed if it is charge-capable.
            if (user.Downed && user == carrier)
            {
                if (user.CurrentBed() is Building_Bed bed && RestUtility.IsValidBedFor(bed, user, carrier, true) && (bed.GetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading.Any(thing => thing.HasComp<CompPawnCharger>() && (thing.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)) ?? false))
                {
                    return bed;
                }
                else
                {
                    return null;
                }
            }

            // Try to use the user's owned bed if it is legal.
            if (user.ownership?.OwnedBed != null)
            {
                Building_Bed ownedBed = user.ownership.OwnedBed;
                if ((int)ownedBed.Position.GetDangerFor(user, user.Map) <= (int)Danger.Deadly && RestUtility.IsValidBedFor(ownedBed, user, carrier, true) && (ownedBed.TryGetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading.Any(thing => thing.HasComp<CompPawnCharger>() && thing.TryGetComp<CompPowerTrader>()?.PowerOn == true) == true))
                {
                    return ownedBed;
                }
            }

            return (Building_Bed)GenClosest.ClosestThingReachable(user.PositionHeld, user.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.Bed), PathEndMode.OnCell, TraverseParms.For(carrier), 9999f, (Thing b) => b.def.IsBed && (int)b.Position.GetDangerFor(user, user.Map) <= (int)Danger.Deadly && RestUtility.IsValidBedFor(b, user, carrier, true) && (b.TryGetComp<CompAffectedByFacilities>()?.LinkedFacilitiesListForReading.Any(thing => thing.HasComp<CompPawnCharger>() && (thing.TryGetComp<CompPowerTrader>()?.PowerOn ?? false)) ?? false));
        }

        // Locate the nearest available energy reservoir for the given pawn user. 
        public static Thing GetReservoir(Pawn user)
        {
            if (user.Map == null)
            {
                return null;
            }

            HashSet<ThingWithComps> reservoirs = user.Map.GetComponent<SC_MapComponent>().reservoirs;
            if (reservoirs.NullOrEmpty())
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(user.PositionHeld, user.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(user), 9999f, (Thing b) => b.TryGetComp<CompEnergyReservoir>() is CompEnergyReservoir reservoir && reservoir.AutomaticallyUsableFor(user), reservoirs);
        }

        // Locate the nearest available power grid access point for the given pawn user. 
        public static Thing GetAccessPoint(Pawn user)
        {
            if (user.Map == null)
            {
                return null;
            }

            HashSet<ThingWithComps> accessPoints = user.Map.GetComponent<SC_MapComponent>().accessPoints;
            if (accessPoints.NullOrEmpty())
            {
                return null;
            }

            return GenClosest.ClosestThingReachable(user.PositionHeld, user.MapHeld, ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial), PathEndMode.Touch, TraverseParms.For(user), 9999f, (Thing b) => b.TryGetComp<CompPowerGridAccessPoint>() is CompPowerGridAccessPoint accessPoint && accessPoint.Usable, accessPoints);
        }

        /* === HEALTH UTILITIES === */

        // Misc
        // Get a cached Blank pawn (to avoid having to create a new pawn whenever a synstruct is made blank)
        public static Pawn GetBlank()
        {
            if (blankPawn != null)
            {
                return blankPawn;
            }

            // Create the Blank pawn that will be used for all blank synstructs
            PawnGenerationRequest request = new PawnGenerationRequest(Faction.OfPlayer.def.basicMemberKind, Faction.OfPlayer, PawnGenerationContext.PlayerStarter, canGeneratePawnRelations: false, forceNoIdeo: true, forceBaselinerChance: 1, colonistRelationChanceFactor: 0f, forceGenerateNewPawn: true);
            Pawn pawn = PawnGenerator.GeneratePawn(request);
            pawn.story.Childhood = ABF_BackstoryDefOf.ABF_Backstory_Synstruct_Childhood_Blank;
            pawn.story.Adulthood = ABF_BackstoryDefOf.ABF_Backstory_Synstruct_Adulthood_Blank;
            pawn.story.traits.allTraits.Clear();
            pawn.skills.Notify_SkillDisablesChanged();
            pawn.skills.skills.ForEach(delegate (SkillRecord record)
            {
                record.passion = 0;
                record.Level = 0;
                record.xpSinceLastLevel = 0;
                record.xpSinceMidnight = 0;
            });
            pawn.workSettings.EnableAndInitializeIfNotAlreadyInitialized();
            pawn.workSettings.DisableAll();
            if (ModsConfig.BiotechActive)
            {
                for (int i = pawn.genes.GenesListForReading.Count - 1; i >= 0; i--)
                {
                    pawn.genes.RemoveGene(pawn.genes.GenesListForReading[i]);
                }
            }
            if (ModsConfig.IdeologyActive && pawn.Ideo != null)
            {
                pawn.ideo.SetIdeo(null);
            }
            if (pawn.timetable == null)
                pawn.timetable = new Pawn_TimetableTracker(pawn);
            if (pawn.playerSettings == null)
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            if (pawn.foodRestriction == null)
                pawn.foodRestriction = new Pawn_FoodRestrictionTracker(pawn);
            if (pawn.drugs == null)
                pawn.drugs = new Pawn_DrugPolicyTracker(pawn);
            if (pawn.outfits == null)
                pawn.outfits = new Pawn_OutfitTracker(pawn);
            pawn.Name = new NameTriple("ABF_BlankPawnFirstName".Translate(), "ABF_BlankPawnNickname".Translate(), "ABF_BlankPawnLastName".Translate());
            blankPawn = pawn;
            return blankPawn;
        }

        // Duplicate the source pawn into the destination pawn. If overwriteAsDeath is true, then it is considered murdering the destination pawn.
        public static void Duplicate(Pawn source, Pawn dest, bool overwriteAsDeath=true)
        {
            try
            {
                DuplicateStory(ref source, ref dest);

                DuplicateSkills(source, dest);

                // If this duplication is considered to be killing a sapient individual, then handle some relations before they're duplicated.
                if (overwriteAsDeath)
                {
                    PawnDiedOrDownedThoughtsUtility.TryGiveThoughts(dest, null, PawnDiedOrDownedThoughtsKind.Died);
                    Pawn spouse = dest.relations?.GetFirstDirectRelationPawn(PawnRelationDefOf.Spouse);
                    if (spouse != null && !spouse.Dead && spouse.needs.mood != null)
                    {
                        MemoryThoughtHandler memories = spouse.needs.mood.thoughts.memories;
                        memories.RemoveMemoriesOfDef(ThoughtDefOf.GotMarried);
                        memories.RemoveMemoriesOfDef(ThoughtDefOf.HoneymoonPhase);
                    }
                    Traverse.Create(dest.relations).Method("AffectBondedAnimalsOnMyDeath").GetValue();
                    dest.health.NotifyPlayerOfKilled(null, null, null);

                    // Synstructs may have up to one creator.
                    Pawn creator = dest.relations?.GetFirstDirectRelationPawn(SC_PawnRelationDefOf.ABF_PawnRelation_Synstruct_Creator);
                    if (creator != null && !creator.Dead && creator.needs.mood != null)
                    {
                        creator.relations.TryRemoveDirectRelation(SC_PawnRelationDefOf.ABF_PawnRelation_Synstruct_Creation, dest);
                    }
                    // Synstructs may have many creations.
                    List<DirectPawnRelation> relationsToRemove = new List<DirectPawnRelation>();
                    foreach (DirectPawnRelation directPawnRelation in dest.relations.DirectRelations)
                    {
                        if (directPawnRelation.def == SC_PawnRelationDefOf.ABF_PawnRelation_Synstruct_Creation)
                        {
                            relationsToRemove.Add(directPawnRelation);
                        }
                    }
                    for (int i = relationsToRemove.Count - 1; i >= 0; i--)
                    {
                        relationsToRemove[i].otherPawn.relations.RemoveDirectRelation(relationsToRemove[i]);
                    }

                    dest.relations.ClearAllRelations();
                }

                // Duplicate relations.
                DuplicateRelations(source, dest);

                // If Ideology dlc is active, duplicate pawn ideology into destination.
                if (ModsConfig.IdeologyActive)
                {
                    DuplicateIdeology(source, dest);
                }

                // If Royalty dlc is active, then handle it. Royalty is non-transferable, but it should be checked for the other details that have been duplicated.
                if (ModsConfig.RoyaltyActive)
                {
                    DuplicateRoyalty(source, dest);
                }

                // Duplicate faction. No difference if tethered or not.
                if (source.Faction != dest.Faction)
                    dest.SetFaction(source.Faction);

                // Duplicate source needs into destination. This is not tetherable.
                DuplicateNeeds(source, dest);

                // Only duplicate source settings for player pawns as foreign pawns don't need them. Can not be tethered as otherwise pawns would be forced to have same work/time/role settings.
                if (source.Faction != null && dest.Faction != null && source.Faction.IsPlayer && dest.Faction.IsPlayer)
                {
                    DuplicatePlayerSettings(source, dest);
                }

                // Duplicate source name into destination.
                NameTriple sourceName = (NameTriple)source.Name;
                dest.Name = new NameTriple(sourceName.First, sourceName.Nick, sourceName.Last);

                dest.Drawer.renderer.SetAllGraphicsDirty();
            }
            catch(Exception e)
            {
                Log.Error("[ABF] Synstructs Core: error occurred duplicating " + source + " into " + dest + ". This will have severe consequences. " + e.Message + e.StackTrace);
            }
        }

        // Duplicate all appropriate details from the StoryTracker of the source into the destination.
        public static void DuplicateStory(ref Pawn source, ref Pawn dest)
        {
            dest.story.favoriteColor = source.story.favoriteColor;
            dest.story.Childhood = source.story.Childhood;
            dest.story.Adulthood = source.story.Adulthood;
            dest.story.title = source.story.title;
            dest.story.traits.allTraits.Clear();
            foreach (Trait sourceTrait in source.story.traits.allTraits)
            {
                if (!ModsConfig.BiotechActive || sourceTrait.sourceGene == null)
                {
                    dest.story.traits.GainTrait(new Trait(sourceTrait.def, sourceTrait.Degree, sourceTrait.ScenForced));
                }
            }
            dest.Notify_DisabledWorkTypesChanged();
            dest.skills.Notify_SkillDisablesChanged();
        }

        // Duplicate ideology details from the source to the destination.
        public static void DuplicateIdeology(Pawn source, Pawn dest)
        {
            // If source ideology is null, then destination's ideology should also be null. Vanilla handles null ideoligions relatively gracefully.
            if (source.ideo == null)
            {
                dest.ideo = null;
            }
            else
            {
                dest.ideo = new Pawn_IdeoTracker(dest);
                dest.ideo.SetIdeo(source.Ideo);
                dest.ideo.OffsetCertainty(source.ideo.Certainty - dest.ideo.Certainty);
                dest.ideo.joinTick = source.ideo.joinTick;
            }
        }

        // Royalty status can not actually be duplicated, but duplicating a pawn should still handle cases around royal abilities/details.
        public static void DuplicateRoyalty(Pawn source, Pawn dest)
        {
            try
            {
                if (source.royalty != null)
                {
                    source.royalty.UpdateAvailableAbilities();
                    if (source.needs != null)
                        source.needs.AddOrRemoveNeedsAsAppropriate();
                    source.abilities.Notify_TemporaryAbilitiesChanged();
                }
                if (dest.royalty != null)
                {
                    dest.royalty.UpdateAvailableAbilities();
                    if (dest.needs != null)
                        dest.needs.AddOrRemoveNeedsAsAppropriate();
                    dest.abilities.Notify_TemporaryAbilitiesChanged();
                }
            }
            catch (Exception exception)
            {
                Log.Warning("[ABF] An unexpected error occurred during royalty duplication between " + source + " " + dest + ". No further issues are anticipated." + exception.Message + exception.StackTrace);
            }
        }

        // Duplicate all skill levels, xp gains, and passions into the destination.
        public static void DuplicateSkills(Pawn source, Pawn dest)
        {
            dest.skills.skills.Clear();
            foreach (SkillRecord skill in source.skills.skills)
            {
                SkillRecord item = new SkillRecord(dest, skill.def)
                {
                    levelInt = skill.levelInt,
                    passion = skill.passion,
                    xpSinceLastLevel = skill.xpSinceLastLevel,
                    xpSinceMidnight = skill.xpSinceMidnight
                };
                dest.skills.skills.Add(item);
            }
        }

        // Duplicate relations from the source to the destination. This should also affect other pawn relations, and any animals involved.
        public static void DuplicateRelations(Pawn source, Pawn dest)
        {
            Pawn_RelationsTracker destRelations = new Pawn_RelationsTracker(dest);

            List<Pawn> checkedOtherPawns = new List<Pawn>();
            // Duplicate all of the source's relations. Ensure that other pawns with relations to the source also have them to the destination.
            foreach (DirectPawnRelation pawnRelation in source.relations?.DirectRelations?.ToList())
            {
                // Ensure that we check the pawn relations for the opposite side only once to avoid doing duplicate relations.
                if (!checkedOtherPawns.Contains(pawnRelation.otherPawn))
                {
                    // Iterate through all of the other pawn's relations and copy any they have with the source onto the destination.
                    foreach (DirectPawnRelation otherPawnRelation in pawnRelation.otherPawn.relations?.DirectRelations.ToList())
                    {
                        if (otherPawnRelation.otherPawn == source)
                        {
                            pawnRelation.otherPawn.relations?.AddDirectRelation(otherPawnRelation.def, dest);
                        }
                    }
                    checkedOtherPawns.Add(pawnRelation.otherPawn);
                }
                destRelations.AddDirectRelation(pawnRelation.def, pawnRelation.otherPawn);
            }

            destRelations.everSeenByPlayer = true;
            dest.relations = destRelations;

            // Transfer animal master status to destination
            foreach (Map map in Find.Maps)
            {
                foreach (Pawn animal in map.mapPawns.SpawnedColonyAnimals)
                {
                    if (animal.playerSettings == null)
                        continue;

                    if (animal.playerSettings.Master != null && animal.playerSettings.Master == source)
                        animal.playerSettings.Master = dest;
                }
            }
        }

        // Duplicate applicable needs from the source to the destination. This includes mood thoughts, memories, and ensuring it updates its needs as appropriate.
        public static void DuplicateNeeds(Pawn source, Pawn dest)
        {
            dest.needs?.AddOrRemoveNeedsAsAppropriate();
            if (source.needs.mood == null || dest.needs.mood == null)
            {
                return;
            }
            List<Thought_Memory> memories = dest.needs.mood.thoughts.memories.Memories;
            memories.Clear();
            foreach (Thought_Memory memory in source.needs.mood.thoughts.memories.Memories)
            {
                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(memory.def);
                thought_Memory.CopyFrom(memory);
                thought_Memory.pawn = dest;
                memories.Add(thought_Memory);
            }
            dest.needs?.mood?.thoughts?.situational?.Notify_SituationalThoughtsDirty();
        }

        public static void DuplicatePlayerSettings(Pawn source, Pawn dest)
        {
            // Initialize destination work settings if not initialized.
            if (dest.workSettings == null)
            {
                dest.workSettings = new Pawn_WorkSettings(dest);
            }
            dest.workSettings.EnableAndInitializeIfNotAlreadyInitialized();

            // Apply work settings to the destination from the source.
            if (source.workSettings != null && source.workSettings.EverWork)
            {
                foreach (WorkTypeDef workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                {
                    if (!dest.WorkTypeIsDisabled(workTypeDef))
                        dest.workSettings.SetPriority(workTypeDef, source.workSettings.GetPriority(workTypeDef));
                }
            }

            if (source.timetable != null)
            {
                for (int i = 0; i != 24; i++)
                {
                    dest.timetable.SetAssignment(i, source.timetable.GetAssignment(i));
                }
            }

            if (source.playerSettings != null)
            {
                dest.playerSettings.joinTick = source.playerSettings.joinTick;
                dest.playerSettings.AreaRestrictionInPawnCurrentMap = source.playerSettings.AreaRestrictionInPawnCurrentMap;
                dest.playerSettings.hostilityResponse = source.playerSettings.hostilityResponse;
                dest.playerSettings.medCare = source.playerSettings.medCare;
            }

            if (source.outfits != null)
            {
                dest.outfits = new Pawn_OutfitTracker(dest);
                dest.outfits.CurrentApparelPolicy = source.outfits.CurrentApparelPolicy;
            }
        }

        // Get all coherence effects for the given race from the appropriate dictionary as a HashSet, generating the HashSet as needed.
        public static HashSet<HediffDef> GetCoherenceEffects(ThingDef thingDef)
        {
            // Acquire the enumerable of all coherence effect hediffs if it hasn't been initialized yet.
            if (allCoherenceHediffs == null)
            {
                allCoherenceHediffs = DefDatabase<HediffDef>.AllDefsListForReading.Where(hediffDef => hediffDef.HasModExtension<ABF_CoherenceEffectExtension>());
            }

            try
            {
                // Generate the HashSet if one does not exist already.
                if (!cachedCoherenceHediffs.ContainsKey(thingDef))
                {
                    HashSet<HediffDef> validHediffs = new HashSet<HediffDef>();
                    // Scan all HediffDefs and add them to the HashSet if they are legal.
                    foreach (HediffDef hediffDef in allCoherenceHediffs)
                    {
                        // The HediffDef is guaranteed to have the mod extension as that is a pre-requisite condition for being in the enumerable.
                        ABF_CoherenceEffectExtension effectExtension = hediffDef.GetModExtension<ABF_CoherenceEffectExtension>();

                        // If the coherence worker specifies this thingDef is invalid for this Hediff, move on.
                        if (!effectExtension.CoherenceWorkers.NullOrEmpty() && effectExtension.CoherenceWorkers.Any(coherenceWorker => !coherenceWorker.CanEverApplyTo(thingDef)))
                        {
                            continue;
                        }

                        // If the extension has its whitelist defined, ensure the thingDef is included in that list.
                        if (effectExtension.racesToAffect != null && !effectExtension.racesToAffect.Any(targetDef => targetDef == thingDef))
                        {
                            continue;
                        }

                        // If the extension is stage-related and isn't thingDef-restricted/worker-restricted, allow it.
                        if (effectExtension.isCoherenceStageEffect)
                        {
                            validHediffs.Add(hediffDef);
                            continue;
                        }

                        // If the extension defines specific parts to affect and the thingDef has any of those parts, it may be applied.
                        if (effectExtension.partsToAffect != null)
                        {
                            bool locatedViablePart = false;
                            foreach (BodyPartDef partDef in effectExtension.partsToAffect)
                            {
                                if (thingDef.race.body.GetPartsWithDef(partDef).Count > 0)
                                {
                                    validHediffs.Add(hediffDef);
                                    locatedViablePart = true;
                                    break;
                                }
                            }
                            // If this hediff is valid here, no need to run other checks.
                            if (locatedViablePart)
                            {
                                continue;
                            }
                        }

                        // If the extension defines specific part tags to affect and the thingDef has any of that tagged part, it may be applied.
                        if (effectExtension.bodyPartTagsToAffect != null)
                        {
                            bool locatedViableTag = false;
                            foreach (BodyPartTagDef tagDef in effectExtension.bodyPartTagsToAffect)
                            {
                                if (thingDef.race.body.HasPartWithTag(tagDef))
                                {
                                    validHediffs.Add(hediffDef);
                                    locatedViableTag = true;
                                    break;
                                }
                            }
                            // If this hediff is valid here, no need to run other checks.
                            if (locatedViableTag)
                            {
                                continue;
                            }
                        }

                        // If the extension defines a depth to affect and the thingDef has any part with that depth, it may be applied.
                        if (effectExtension.partDepthToAffect != BodyPartDepth.Undefined)
                        {
                            if (thingDef.race.body.AllParts.Any(partRecord => partRecord.depth == effectExtension.partDepthToAffect))
                            {
                                validHediffs.Add(hediffDef);
                            }
                        }
                    }
                    cachedCoherenceHediffs[thingDef] = validHediffs;
                }
                return cachedCoherenceHediffs[thingDef];
            }
            catch (Exception ex)
            {
                Log.Error("[ABF] Encountered an error while trying to generate and return coherence-related HediffDefs for " + thingDef.defName + ". Additional errors may occur! " + ex.Message + ex.StackTrace);
                return new HashSet<HediffDef>();
            }
        }

        // For a given map, recache the number of player drones on it with the amicability directive.
        public static void UpdateAmicableDroneCount(Map map)
        {
            List<Pawn> playerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            amicableDroneCount = new Dictionary<Map, int>();
            int resultCount = 0;
            for (int i = playerPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = playerPawns[i];
                if (ABF_Utils.IsProgrammableDrone(pawn) && pawn.GetComp<CompArtificialPawn>().ActiveDirectives.Contains(ABF_DirectiveDefOf.ABF_Directive_Synstruct_Friend))
                {
                    resultCount++;
                }
            }
            amicableDroneCount[map] = resultCount;
        }

        // For a given map, recache the number of player organic colonists and animals.
        public static void UpdatePlayerOrganicPawnCount(Map map)
        {
            List<Pawn> playerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            playerOrganicPawnCount = new Dictionary<Map, int>();
            int organicPawnCount = 0;
            int organicAnimalCount = 0;
            for (int i = playerPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = playerPawns[i];
                if (!ABF_Utils.IsArtificial(pawn))
                {
                    organicPawnCount++;
                    if (pawn.RaceProps.intelligence == Intelligence.Animal)
                    {
                        organicAnimalCount++;
                    }
                }
            }
            playerOrganicPawnCount[map] = organicPawnCount;
            playerOrganicAnimalCount[map] = organicAnimalCount;
        }

        // For a given map, recache the number of player biomimetics.
        public static void UpdatePlayerBiomimeticCount(Map map)
        {
            List<Pawn> playerPawns = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
            playerBiomimeticCount = new Dictionary<Map, int>();
            int resultCount = 0;
            for (int i = playerPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = playerPawns[i];
                if (ABF_Utils.IsArtificial(pawn) && pawn.RaceProps.intelligence == Intelligence.Animal)
                {
                    resultCount++;
                }
            }
            playerBiomimeticCount[map] = resultCount;
        }

        // For a given enumerable of room chargers, recalculate their cached values.
        public static void UpdateRoomChargers(IEnumerable<CompRoomCharger> roomChargers)
        {
            // Build a dict matching rooms to the list of chargers in that room. Each charger will want to know how many other chargers are within it.
            Dictionary<Room, List<CompRoomCharger>> chargersInRoom = new Dictionary<Room, List<CompRoomCharger>>();
            foreach (CompRoomCharger roomCharger in roomChargers)
            {
                Room room = roomCharger.parent.GetRoom();
                // No need to do store chargers that are in improper rooms or are inactive - they only calculate baselines.
                if (room == null || !room.ProperRoom || (roomCharger.Props.inoperableOutdoors && room.UsesOutdoorTemperature) || (roomCharger.Props.inoperableInLargeRooms && room.IsHuge))
                {
                    roomCharger.RecalculateCaches(null, null, null, baselineOnly: true);
                    continue;
                }
                if (chargersInRoom.ContainsKey(room))
                {
                    chargersInRoom[room].Add(roomCharger);
                }
                else
                {
                    chargersInRoom[room] = new List<CompRoomCharger>
                    {
                        roomCharger
                    };
                }
            }

            // Rooms are not required to be on the same Map, but if they happen to be, then there's no reason to search the pawn list repeatedly for valid pawns.
            Dictionary<Map, List<Pawn>> validPawnsInMap = new Dictionary<Map, List<Pawn>>();
            Dictionary<Room, List<Pawn>> validPawnsInRoom = new Dictionary<Room, List<Pawn>>();
            foreach (Room room in chargersInRoom.Keys)
            {
                validPawnsInRoom[room] = new List<Pawn>();

                Map map = room.Map;
                // If we haven't gotten a list of valid pawns on the map, we need to scan them all.
                if (!validPawnsInMap.ContainsKey(map))
                {
                    List<Pawn> validPawns = new List<Pawn>();
                    foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                    {
                        if (CanCharge(pawn))
                        {
                            // If the valid pawn happened to be in the right room, they aren't in any other room, so no need to check them later.
                            if (pawn.GetRoom() == room)
                            {
                                validPawnsInRoom[room].Add(pawn);
                            }
                            else
                            {
                                validPawns.Add(pawn);
                            }
                        }
                    }
                    validPawnsInMap[map] = validPawns;
                }
                // If we do have a list of known, valid pawns on the map already, we can scan them and remove found pawns for other iterations.
                else
                {
                    List<Pawn> validPawns = validPawnsInMap[map];
                    for (int i = validPawns.Count - 1; i >= 0; i--)
                    {
                        if (validPawns[i].GetRoom() == room)
                        {
                            validPawnsInRoom[room].Add(validPawns[i]);
                            validPawnsInMap[map].RemoveAt(i);
                        }
                    }
                }
            }

            // Having the chargers and pawns in each room, it should now be relatively straightforward to recalculate the caches.
            foreach (Room room in chargersInRoom.Keys)
            {
                foreach (CompRoomCharger roomCharger in chargersInRoom[room])
                {
                    roomCharger.RecalculateCaches(room, chargersInRoom[room], validPawnsInRoom[room]);
                }
            }
        }

        // Cached races that are considered synstructs for establishing correct behavior, cached at startup.
        public static HashSet<ThingDef> cachedSynstructs = new HashSet<ThingDef>();

        // Cached Hediff Set for all coherence effect Hediffs and a dictionary matching ThingDef to the valid coherence effects for that race, cached when needed.
        private static IEnumerable<HediffDef> allCoherenceHediffs;
        private static Dictionary<ThingDef, HashSet<HediffDef>> cachedCoherenceHediffs = new Dictionary<ThingDef, HashSet<HediffDef>>();

        // Cached dictionary matching maps to the number of drones on it that have the amicability directive. Cached via map component regularly.
        public static Dictionary<Map, int> amicableDroneCount = new Dictionary<Map, int>();

        // Cached dictionary matching maps to the number of organic beings in the player's colony. Cached via map component regularly.
        public static Dictionary<Map, int> playerOrganicPawnCount = new Dictionary<Map, int>();

        // Cached dictionary matching maps to the number of organic animals in the player's colony. Cached via map component regularly.
        public static Dictionary<Map, int> playerOrganicAnimalCount = new Dictionary<Map, int>();

        // Cached dictionary matching maps to the number of biomimetics in the player's colony. Cached via map component regularly.
        public static Dictionary<Map, int> playerBiomimeticCount = new Dictionary<Map, int>();

        // Comparer of pawns, used in various places to keep lists in a particular sort order.
        public class PawnComparer : Comparer<Pawn>
        {
            public override int Compare(Pawn x, Pawn y)
            {
                return x.LabelShortCap.CompareTo(y.LabelShortCap);
            }
        }
    }
}

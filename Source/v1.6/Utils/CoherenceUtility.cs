﻿using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ArtificialBeings
{
    public class CoherenceUtility
    {
        // Locate a viable coherence spot for this pawn. Order is: assigned spot, then owned room, then a room with a charging station in it, then anywhere in the colony.
        public static LocalTargetInfo FindCoherenceSpot(Pawn pawn)
        {
            // Downed pawns skip the entire process and use their current position.
            if (pawn.Downed)
            {
                return pawn.Position;
            }

            float highestPreferability = float.MinValue;
            LocalTargetInfo spot = LocalTargetInfo.Invalid;
            Room ownedRoom = pawn.ownership.OwnedRoom;
            var tmep = AllCoherenceSpotCandidates(pawn);
            foreach (LocalTargetInfo item in tmep)
            {
                // If this spot is illegal for the pawn, skip.
                if (!SafeEnvironmentalConditions(pawn, item.Cell, pawn.Map) || !item.Cell.Standable(pawn.Map) || item.Cell.IsForbidden(pawn) || !pawn.CanReserveAndReach(item.Cell, PathEndMode.OnCell, Danger.None))
                {
                    continue;
                }

                // Set up the preferability score, defaulting to how far the given candidate is from the pawn (no higher than 1).
                float preferabilityScore = 1f / Mathf.Max(item.Cell.DistanceToSquared(pawn.Position), 0.1f);
                Room room = item.Cell.GetRoom(pawn.Map);

                if (item.Thing != null && item.Thing is Building building && building.def == ABF_ThingDefOf.ABF_Thing_Synstruct_CoherenceSpot)
                {
                    // Assigned coherence spot to this pawn or assigned to no one, give an additive weight of 100.
                    if (building.GetAssignedPawns()?.Contains(pawn) == true)
                    {
                        preferabilityScore += 100f;
                    }
                    // Coherence spot assigned to no one, give an additive weight of 10.
                    else if (building.GetAssignedPawns() == null)
                    {
                        preferabilityScore += 10f;
                    }
                    // Coherence spot is assigned to someone else, skip this.
                    else
                    {
                        continue;
                    }
                }
                // Pawn's owned room, give an additive weight of 20.
                if (room != null && ownedRoom == room)
                {
                    preferabilityScore += 20f;
                }
                // Cell is unroofed, give a subtractive weight of 5.
                if (!item.Cell.Roofed(pawn.Map))
                {
                    preferabilityScore -= 5f;
                }
                // If this target score is higher than our stored value, this is the most preferable option right now.
                if (preferabilityScore > highestPreferability)
                {
                    spot = item;
                    highestPreferability = preferabilityScore;
                }
            }
            // Final check - use the current position of the pawn if none can be identified.
            if (!spot.IsValid)
            {
                if (pawn.Position.IsValid && SafeEnvironmentalConditions(pawn, pawn.Position, pawn.Map))
                {
                    spot = pawn.Position;
                }
            }
            return spot;
        }

        // Generates and returns an enumerable of all viable coherence spot candidates in the order of preferability.
        public static IEnumerable<LocalTargetInfo> AllCoherenceSpotCandidates(Pawn pawn)
        {
            List<Room> checkedRooms = new List<Room>();
            // Coherence spots are always (and only) candidates for units of the player faction.
            if (pawn.Faction == Faction.OfPlayer)
            {
                foreach (Building item in pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ABF_ThingDefOf.ABF_Thing_Synstruct_CoherenceSpot))
                {
                    yield return item;
                }
            }

            // A pawn's owned bed allows it to use the room that the bed is in for coherence.
            Building_Bed bed = pawn.ownership?.OwnedBed;
            Room ownedRoom = bed?.GetRoom();
            IntVec3 location;
            if (ownedRoom != null && !ownedRoom.PsychologicallyOutdoors && pawn.CanReserveAndReach(bed, PathEndMode.OnCell, pawn.NormalMaxDanger()))
            {
                checkedRooms.Add(ownedRoom);
                for (int i = 0; i < 3; i++)
                {
                    location = RCellFinder.RandomWanderDestFor(pawn, bed.Position, 3f, (Pawn p, IntVec3 c, IntVec3 r) => c.Standable(p.Map) && c.GetDoor(p.Map) == null && WanderRoomUtility.IsValidWanderDest(p, c, r), pawn.NormalMaxDanger());
                    if (location.IsValid)
                    {
                        yield return location;
                    }
                }
            }

            // Prisoners may only use the room their owned bed is in.
            if (pawn.IsPrisonerOfColony)
            {
                yield break;
            }

            // Locate three random colony positions that are valid and yield them.
            IntVec3 colonyWanderRoot = WanderUtility.GetColonyWanderRoot(pawn);
            for (int i = 0; i < 3; i++)
            {
                location = RCellFinder.RandomWanderDestFor(pawn, colonyWanderRoot, 10f, delegate (Pawn p, IntVec3 c, IntVec3 r)
                {
                    if (!c.Standable(p.Map) || c.GetDoor(p.Map) != null || !p.CanReserveAndReach(c, PathEndMode.OnCell, p.NormalMaxDanger()))
                    {
                        return false;
                    }
                    Room randomRoom = c.GetRoom(p.Map);
                    if (randomRoom != null && CanUseRoomForCohering(randomRoom, pawn) && !checkedRooms.Contains(randomRoom))
                    {
                        checkedRooms.Add(randomRoom);
                        return true;
                    }
                    return false;
                }, pawn.NormalMaxDanger());
                if (location.IsValid)
                {
                    yield return location;
                }
            }
        }

        // Returns an enumerable of valid spots in a given room for doing coherence for a given pawn.
        public static IEnumerable<LocalTargetInfo> CoherenceSpotsInTheRoom(Pawn pawn, Room r)
        {
            if (r == null)
            {
                yield break;
            }

            List<Thing> things = r.ContainedAndAdjacentThings;
            for (int i = 0; i < things.Count; i++)
            {
                LocalTargetInfo localTargetInfo = things[i].OccupiedRect().ExpandedBy(2).AdjacentCellsCardinal.Where((IntVec3 cell) => r.ContainsCell(cell) && !cell.IsForbidden(pawn) && pawn.CanReserveAndReach(cell, PathEndMode.OnCell, pawn.NormalMaxDanger()) && cell.Standable(pawn.Map)).RandomElementWithFallback(IntVec3.Invalid);
                if (localTargetInfo.IsValid)
                {
                    yield return localTargetInfo;
                }
            }
        }

        // Returns true if the room is valid for doing coherence, and false otherwise.
        public static bool CanUseRoomForCohering(Room r, Pawn p)
        {
            if (!r.Owners.EnumerableNullOrEmpty() && !r.Owners.Contains(p))
            {
                return false;
            }
            if (r.IsPrisonCell && !p.IsPrisoner)
            {
                return false;
            }
            return true;
        }

        // Returns whether the provided cell is acceptable for a pawn on a given map for coherence.
        public static bool SafeEnvironmentalConditions(Pawn pawn, IntVec3 cell, Map map)
        {
            if (ModsConfig.BiotechActive && NoxiousHazeUtility.IsExposedToNoxiousHaze(pawn, cell, map))
            {
                return false;
            }
            if (cell.GetDangerFor(pawn, map) != Danger.None)
            {
                return false;
            }
            return true;
        }
    }
}

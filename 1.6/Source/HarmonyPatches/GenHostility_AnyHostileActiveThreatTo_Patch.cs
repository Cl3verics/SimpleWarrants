using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(GenHostility), "AnyHostileActiveThreatTo",
    new[] { typeof(Map), typeof(Faction), typeof(IAttackTarget), typeof(bool), typeof(bool) },
    new[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out, ArgumentType.Normal, ArgumentType.Normal })]
    internal static class GenHostility_AnyHostileActiveThreatTo_Patch
    {
        public static Dictionary<Map, Faction> lastFactionThreats = new();

        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref bool __result, Map map, ref IAttackTarget threat)
        {
            if (__result && map.IsPlayerHome is false && threat is Pawn { Faction: { } } pawn && pawn.Faction.def.humanlikeFaction)
            {
                lastFactionThreats[map] = pawn.Faction;
            }
        }

        public static Faction GetLastHostileFactionFromMap(Map map)
        {
            return lastFactionThreats.TryGetValue(map, out var faction)
                ? faction
                : map.ParentFaction != null && map.ParentFaction.def.humanlikeFaction && map.ParentFaction.HostileTo(Faction.OfPlayer)
                ? map.ParentFaction
                : null;
        }
    }
}

using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(FormCaravanComp), "CompTick")]
    public static class FormCaravanComp_CompTick_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var anyUnexploredFoggedRooms = AccessTools.Method(typeof(FormCaravanComp), "get_AnyUnexploredFoggedRooms");
            var codes = codeInstructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                if (i < codes.Count - 3 && codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].Calls(anyUnexploredFoggedRooms) && codes[i + 2].opcode == OpCodes.Brfalse_S)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FormCaravanComp_CompTick_Patch), nameof(RegisterAssault)));
                }
                yield return codes[i];
            }
        }

        public static void RegisterAssault(FormCaravanComp __instance)
        {
            if (__instance.parent is MapParent mapParent && mapParent.Faction != null && mapParent.Faction.def.humanlikeFaction && Rand.Chance(0.1f))
            {
                var pawns = mapParent.Map.mapPawns.FreeHumanlikesOfFaction(Faction.OfPlayer);
                if (pawns.Any())
                {
                    var random = pawns.RandomElement();
                    WarrantsManager.Instance.PutWarrantOn(random, "SW.Assault".Translate(), mapParent.Faction);
                }
            }
        }
    }

    [HarmonyPatch(typeof(SettlementDefeatUtility), "CheckDefeated")]
    public static class SettlementDefeatUtility_CheckDefeated_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var notify_PlayerRaidedSomeone = AccessTools.Method(typeof(IdeoUtility), "Notify_PlayerRaidedSomeone");
            var codes = codeInstructions.ToList();
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (codes[i].Calls(notify_PlayerRaidedSomeone))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SettlementDefeatUtility_CheckDefeated_Patch), nameof(RegisterAssault)));
                }
            }
        }

        public static void RegisterAssault(Map map)
        {
            if (map.ParentFaction != null && map.ParentFaction.def.humanlikeFaction && Rand.Chance(0.1f))
            {
                var pawns = map.mapPawns.FreeHumanlikesOfFaction(Faction.OfPlayer);
                if (pawns.Any())
                {
                    var random = pawns.RandomElement();
                    WarrantsManager.Instance.PutWarrantOn(random, "SW.Assault".Translate(), map.ParentFaction);
                }
            }
            Log.Message( map + " - " + " - " + string.Join(", ", map.mapPawns.FreeHumanlikesOfFaction(Faction.OfPlayer)));
        }
    }
}

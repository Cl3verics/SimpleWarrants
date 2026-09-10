using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleWarrants
{
    //[HarmonyPatch(typeof(FormCaravanComp), "CompTick")]
    //public static class FormCaravanComp_CompTick_Patch
    //{
    //    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
    //    {
    //        var anyUnexploredFoggedRooms = AccessTools.Method(typeof(FormCaravanComp), "get_AnyUnexploredFoggedRooms");
    //        var codes = codeInstructions.ToList();
    //        for (var i = 0; i < codes.Count; i++)
    //        {
    //            if (i < codes.Count - 3 && codes[i].opcode == OpCodes.Ldarg_0 && codes[i + 1].Calls(anyUnexploredFoggedRooms) && codes[i + 2].opcode == OpCodes.Brfalse_S)
    //            {
    //                yield return new CodeInstruction(OpCodes.Ldarg_0);
    //                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FormCaravanComp_CompTick_Patch), nameof(RegisterAssault)));
    //            }
    //            yield return codes[i];
    //        }
    //    }
    //
    //    public static void RegisterAssault(FormCaravanComp __instance)
    //    {
    //        if (__instance.parent is MapParent mapParent && mapParent.Map != null && Rand.Chance(0.25f))
    //        {
    //            var faction = AnyHostileActiveThreatTo_Patch.GetLastHostileFactionFromMap(mapParent.Map);
    //            if (faction != null)
    //            {
    //                var pawns = mapParent.Map.mapPawns.FreeHumanlikesOfFaction(Faction.OfPlayer).Where(x => WarrantsManager.Instance.CanPutWarrantOn(x));
    //                if (pawns.Any())
    //                {
    //                    var random = pawns.RandomElement();
    //                    WarrantsManager.Instance.PutWarrantOn(random, "SW.Assault".Translate(), faction);
    //                }
    //            }
    //        }
    //    }
    //}

    [HarmonyPatch(typeof(SettlementDefeatUtility), "CheckDefeated")]
    public static class SettlementDefeatUtility_CheckDefeated_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codeInstructions)
        {
            var notify_PlayerRaidedSomeone = AccessTools.Method(typeof(IdeoUtility), nameof(IdeoUtility.Notify_PlayerRaidedSomeone));
            foreach (var item in codeInstructions.ToList())
            {
                yield return item;
                if (item.Calls(notify_PlayerRaidedSomeone))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(SettlementDefeatUtility_CheckDefeated_Patch), nameof(RegisterAssault)));
                }
            }
        }

        public static void RegisterAssault(Map map)
        {
            var faction = GenHostility_AnyHostileActiveThreatTo_Patch.GetLastHostileFactionFromMap(map);
            if (faction == null || faction.HostileTo(Faction.OfPlayer) || SimpleWarrantsMod.Settings.enableWarrantsOnAssault is false || Rand.Chance(0.5f) is false)
            {
                return;
            }

            // Get a random player pawn from the attackers and slap a bounty on them.
            var pawns = map.mapPawns.FreeHumanlikesOfFaction(Faction.OfPlayer).Where(WarrantsManager.Instance.CanPutWarrantOn);
            if (pawns.TryRandomElement(out Pawn selectedPawn))
            {
                WarrantsManager.Instance.PutWarrantOn(selectedPawn, SW_DefOf.SW_Assault, faction);
            }
        }
    }
}

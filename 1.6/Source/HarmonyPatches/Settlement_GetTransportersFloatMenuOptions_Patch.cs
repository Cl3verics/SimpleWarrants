using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace SimpleWarrants
{
    [HarmonyPatch]
    public static class Settlement_GetTransportersFloatMenuOptions_Patch
    {
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Settlement), nameof(Settlement.GetTransportersFloatMenuOptions));
            yield return AccessTools.Method(typeof(Settlement), nameof(Settlement.GetShuttleFloatMenuOptions));
        }

        public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, IEnumerable<IThingHolder> pods, Action<PlanetTile, TransportersArrivalAction> launchAction, Settlement __instance)
        {
            foreach (var floatMenuOption in __result)
            {
                yield return floatMenuOption;
            }
            foreach (var floatMenuOption in TransportersArrivalAction_ReturnWarrant.GetFloatMenuOptions(launchAction, pods, __instance))
            {
                yield return floatMenuOption;
            }
        }
    }
}

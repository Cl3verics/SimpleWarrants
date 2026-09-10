using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.ChoseWorldTarget), new[]
    {
        typeof(GlobalTargetInfo),
        typeof(PlanetTile),
        typeof(IEnumerable<IThingHolder>),
        typeof(int),
        typeof(Action<PlanetTile, TransportersArrivalAction>),
        typeof(CompLaunchable),
        typeof(float?)
    })]
    public static class CompLaunchable_ChoseWorldTarget_Patch
    {
        public static bool suppressingSignalJammer;

        public static void Prefix(GlobalTargetInfo target, IEnumerable<IThingHolder> pods)
        {
            if (ModsConfig.OdysseyActive && target.WorldObject is Settlement settlement && TransportersArrivalAction_ReturnWarrant.HasReturnableWarrant(pods, settlement))
            {
                suppressingSignalJammer = true;
            }
        }

        public static void Postfix()
        {
            suppressingSignalJammer = false;
        }
    }
}

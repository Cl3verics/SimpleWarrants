using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(CompLaunchable), nameof(CompLaunchable.TargetingLabelGetter), new[]
    {
        typeof(GlobalTargetInfo),
        typeof(PlanetTile),
        typeof(int),
        typeof(IEnumerable<IThingHolder>),
        typeof(Action<PlanetTile, TransportersArrivalAction>),
        typeof(CompLaunchable),
        typeof(float?)
    })]
    public static class CompLaunchable_TargetingLabelGetter_Patch
    {
        public static void Prefix(GlobalTargetInfo target, IEnumerable<IThingHolder> pods)
        {
            if (ModsConfig.OdysseyActive && target.WorldObject is Settlement settlement && TransportersArrivalAction_ReturnWarrant.HasReturnableWarrant(pods, settlement))
            {
                CompLaunchable_ChoseWorldTarget_Patch.suppressingSignalJammer = true;
            }
        }

        public static void Postfix()
        {
            CompLaunchable_ChoseWorldTarget_Patch.suppressingSignalJammer = false;
        }
    }
}

using HarmonyLib;
using RimWorld.Planet;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.RequiresSignalJammerToReach), MethodType.Getter)]
    public static class WorldObject_RequiresSignalJammerToReach_Patch
    {
        public static void Postfix(ref bool __result)
        {
            if (__result && CompLaunchable_ChoseWorldTarget_Patch.suppressingSignalJammer)
            {
                __result = false;
            }
        }
    }
}

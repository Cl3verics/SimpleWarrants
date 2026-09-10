using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public static class Pawn_Kill_Patch
    {
        private const float PoachingWarrantChance = 0.25f;

        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            if (dinfo.HasValue && dinfo.Value.Instigator is Pawn instigator && instigator.RaceProps.Humanlike && instigator.Faction == Faction.OfPlayer
                && __instance.RaceProps.Animal && __instance.Faction is null && __instance.def.BaseMarketValue >= ThingDefOf.Thrumbo.BaseMarketValue)
            {
                if (HuntJobUtility.WasKilledByHunter(__instance, dinfo) is false || instigator.CurJob.ignoreDesignations || __instance.health.hediffSet.HasHediff(HediffDefOf.Scaria) || __instance.InMentalState && (__instance.MentalStateDef == MentalStateDefOf.Manhunter || __instance.MentalStateDef == MentalStateDefOf.ManhunterPermanent || __instance.MentalStateDef.IsAggro) || __instance.mindState != null && __instance.mindState.enemyTarget == instigator)
                {
                    return;
                }

                if (SimpleWarrantsMod.Settings.enableWarrantsOnPoaching && WarrantsManager.Instance.CanPutWarrantOn(instigator) && Rand.Chance(PoachingWarrantChance))
                {
                    WarrantsManager.Instance.PutWarrantOn(instigator, SW_DefOf.SW_Poaching, Utils.AnyHostileToPlayerFaction());
                }
            }
        }
    }
}

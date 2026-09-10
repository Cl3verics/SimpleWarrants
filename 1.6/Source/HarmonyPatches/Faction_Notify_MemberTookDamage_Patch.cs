using HarmonyLib;
using RimWorld;
using Verse;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
    public static class Faction_Notify_MemberTookDamage_Patch
    {
        private const float TortureWarrantChance = 0.05f;
        private const int RecentPrisonBreakTicksThreshold = 7500;

        public static void Postfix(Faction __instance, Pawn member, DamageInfo dinfo)
        {
            if (member.IsPrisoner && member.Faction != Faction.OfPlayer && dinfo.Instigator is Pawn instigator && instigator.Faction == Faction.OfPlayer
                && member.InMentalState is false && IsLegitimatePrisonerHarm(member, instigator, dinfo) is false && SimpleWarrantsMod.Settings.enableWarrantsOnTorture && WarrantsManager.Instance.CanPutWarrantOn(instigator) && Rand.Chance(TortureWarrantChance))
            {
                WarrantsManager.Instance.PutWarrantOn(instigator, SW_DefOf.SW_Torture, __instance);
            }
        }

        private static bool IsLegitimatePrisonerHarm(Pawn member, Pawn instigator, DamageInfo dinfo)
        {
            if (dinfo.Def == DamageDefOf.SurgicalCut || dinfo.Def == DamageDefOf.ExecutionCut || instigator.CurJobDef == JobDefOf.PrisonerExecution || instigator.CurJobDef == JobDefOf.DoBill && instigator.CurJob.bill is Bill_Medical || instigator.CurJobDef == JobDefOf.Arrest || instigator.CurJobDef == JobDefOf.Capture || PrisonBreakUtility.IsPrisonBreaking(member) || SlaveRebellionUtility.IsRebelling(member) || member.guest != null && member.guest.lastPrisonBreakTicks > 0 && Find.TickManager.TicksGame - member.guest.lastPrisonBreakTicks < RecentPrisonBreakTicksThreshold || member.CurJobDef == JobDefOf.AttackMelee || member.CurJobDef == JobDefOf.AttackStatic)
            {
                return true;
            }
            return member.mindState?.enemyTarget != null;
        }
    }
}

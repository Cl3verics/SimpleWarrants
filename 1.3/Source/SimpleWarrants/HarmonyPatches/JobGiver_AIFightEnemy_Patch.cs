using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SimpleWarrants
{
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class JobGiver_AIFightEnemy_Patch
    {
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (__result is null)
            {
                var lord = pawn.GetLord();
                if (lord != null)
                {
                    var raidLords = WarrantsManager.Instance.raidLords.Where(x => x.Value.lords.Contains(lord));
                    if (raidLords.Any())
                    {
                        foreach (var raidLord in raidLords.InRandomOrder())
                        {
                            var victim = raidLord.Key.pawn;
                            if (victim.Map == pawn.Map)
                            {
                                if (victim.Downed)
                                {
                                    if (ReservationUtility.CanReserve(pawn, victim) && RCellFinder.TryFindBestExitSpot(pawn, out IntVec3 spot))
                                    {
                                        __result = JobMaker.MakeJob(JobDefOf.Kidnap);
                                        __result.targetA = victim;
                                        __result.targetB = spot;
                                        __result.count = 1;
                                    }
                                }
                                else if (victim.Corpse != null)
                                {
                                    if (ReservationUtility.CanReserve(pawn, victim.Corpse) && RCellFinder.TryFindBestExitSpot(pawn, out IntVec3 spot))
                                    {
                                        __result = JobMaker.MakeJob(JobDefOf.Kidnap);
                                        __result.targetA = victim.Corpse;
                                        __result.targetB = spot;
                                        __result.count = 1;
                                    }
                                }
                                else
                                {
                                    pawn.mindState.enemyTarget = victim;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

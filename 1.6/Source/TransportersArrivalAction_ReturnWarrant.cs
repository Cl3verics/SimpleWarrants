using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
namespace SimpleWarrants
{
    public class TransportersArrivalAction_ReturnWarrant : TransportersArrivalAction
    {
        private Settlement settlement;
        private Warrant warrant;

        public override bool GeneratesMap => false;

        public TransportersArrivalAction_ReturnWarrant()
        {
        }

        public TransportersArrivalAction_ReturnWarrant(Settlement settlement, Warrant warrant)
        {
            this.settlement = settlement;
            this.warrant = warrant;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref warrant, "warrant");
        }

        public override FloatMenuAcceptanceReport StillValid(IEnumerable<IThingHolder> pods, PlanetTile destinationTile)
        {
            var floatMenuAcceptanceReport = base.StillValid(pods, destinationTile);
            if (!floatMenuAcceptanceReport)
            {
                return floatMenuAcceptanceReport;
            }
            if (settlement != null && settlement.Tile != destinationTile)
            {
                return false;
            }
            return CanReturnWarrant(pods, settlement, warrant);
        }

        public override void Arrived(List<ActiveTransporterInfo> transporters, PlanetTile tile)
        {
            Thing target = null;
            ThingOwner container = null;

            foreach (var pod in transporters)
            {
                foreach (var thing in pod.innerContainer)
                {
                    if (TryGetWarrantTargetInContainer(warrant, thing) != null)
                    {
                        target = thing;
                        container = pod.innerContainer;
                        break;
                    }

                    var compTransporter = thing.TryGetComp<CompTransporter>();
                    if (compTransporter != null)
                    {
                        foreach (var innerThing in compTransporter.innerContainer)
                        {
                            if (TryGetWarrantTargetInContainer(warrant, innerThing) != null)
                            {
                                target = innerThing;
                                container = compTransporter.innerContainer;
                                break;
                            }
                        }
                    }
                    if (target != null) break;
                }
                if (target != null) break;
            }

            if (target == null || container == null)
            {
                Log.Error($"Failed to find warrant target {warrant.thing.LabelCap} in transport pods for warrant {warrant.loadID}.");
            }
            else
            {
                container.Remove(target);

                warrant.status = WarrantStatus.Completed;
                var questTarget = target is Corpse corpse ? corpse.InnerPawn : target;
                QuestUtility.SendQuestTargetSignals(questTarget.questTags, "WarrantRequestFulfilled", settlement.Named("SUBJECT"));

                if (warrant.relatedQuest is { State: <= QuestState.Ongoing })
                    warrant.relatedQuest.End(QuestEndOutcome.Success);

                WarrantsManager.Instance.acceptedWarrants.Remove(warrant);

                // Drop silver reward
                var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = warrant.MaxRewardValue();
                var playerHomeMap = Find.AnyPlayerHomeMap;
                if (playerHomeMap != null)
                {
                    var dropSpot = DropCellFinder.TradeDropSpot(playerHomeMap);
                    DropPodUtility.DropThingsNear(dropSpot, playerHomeMap, new List<Thing> { silver });
                    Messages.Message("SW.WarrantCompletedByPods".Translate(warrant.issuer.Name), MessageTypeDefOf.PositiveEvent, false);
                }
                else
                {
                    Log.Error("Could not find any player home map to drop silver reward.");
                }
            }
        }

        public static FloatMenuAcceptanceReport CanReturnWarrant(IEnumerable<IThingHolder> pods, Settlement settlement, Warrant warrant)
        {
            if (settlement == null || settlement.Spawned is false || settlement.Faction == null || settlement.Faction == Faction.OfPlayer || settlement.HasMap || warrant == null || warrant.issuer != settlement.Faction || warrant.IsWarrantActive() is false)
            {
                return false;
            }

            foreach (var pod in pods)
            {
                foreach (var thing in pod.GetDirectlyHeldThings())
                {
                    if (TryGetWarrantTargetInContainer(warrant, thing) != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Thing TryGetWarrantTargetInContainer(Warrant warrant, Thing thing)
        {
            if (warrant is Warrant_TameAnimal tame && thing is Pawn tamee && tamee.kindDef == tame.AnimalRace || warrant is Warrant_Pawn pw && (thing is Pawn p2 && p2 == pw.Pawn || thing is Corpse c && c.InnerPawn == pw.Pawn) || thing == warrant.thing)
            {
                return thing;
            }
            return null;
        }

        public static bool HasReturnableWarrant(IEnumerable<IThingHolder> pods, Settlement settlement)
        {
            if (settlement == null || settlement.Faction == null || settlement.Faction == Faction.OfPlayer)
            {
                return false;
            }
            var warrants = WarrantsManager.Instance.acceptedWarrants;
            if (warrants == null)
            {
                return false;
            }
            foreach (var warrant in warrants)
            {
                if (warrant.issuer == settlement.Faction && CanReturnWarrant(pods, settlement, warrant))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Action<PlanetTile, TransportersArrivalAction> launchAction, IEnumerable<IThingHolder> pods, Settlement settlement)
        {
            if (settlement.Faction == Faction.OfPlayer)
            {
                yield break;
            }

            var warrants = WarrantsManager.Instance.acceptedWarrants?.Where(x => x.issuer == settlement.Faction && x.IsWarrantActive()).ToList();
            if (warrants == null || warrants.Any() is false)
            {
                yield break;
            }

            foreach (var warrant in warrants)
            {
                if (CanReturnWarrant(pods, settlement, warrant))
                {
                    yield return new FloatMenuOption("SW.ReturnWarrantViaTransportPods".Translate(warrant.thing.LabelCap), () => launchAction(settlement.Tile, new TransportersArrivalAction_ReturnWarrant(settlement, warrant)));
                }
            }
        }
    }
}

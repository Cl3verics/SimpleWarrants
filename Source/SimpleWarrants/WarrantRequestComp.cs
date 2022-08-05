using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SimpleWarrants
{
	public class WorldObjectCompProperties_WarrantRequest : WorldObjectCompProperties
	{
        public WorldObjectCompProperties_WarrantRequest()
		{
			compClass = typeof(WarrantRequestComp);
		}
    }

	[StaticConstructorOnStartup]
	public class WarrantRequestComp : WorldObjectComp
	{
        private static readonly Texture2D TradeCommandTex = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");
        public bool ActiveRequest => ActiveWarrants.Any();
        public IEnumerable<Warrant> ActiveWarrants => WarrantsManager.Instance.acceptedWarrants?.Where(x => x.issuer == parent?.Faction && x.IsWarrantActive()) ?? new List<Warrant>();

        public override string CompInspectStringExtra()
		{
			if (ActiveRequest)
			{
				var requestInfo = string.Join(", ", ActiveWarrants.Select(x => x.thing.LabelCap ?? x.thing.def.label));
				return "SW.CaravanRequestInfo".Translate(requestInfo);
			}
			return null;
		}

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
		{
			if (ActiveRequest && CaravanVisitUtility.SettlementVisitedNow(caravan) == parent)
			{
				yield return FulfillRequestCommand(caravan);
			}
		}

        private Command FulfillRequestCommand(Caravan caravan)
		{
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = "SW.CommandFulfillWarrant".Translate();
			command_Action.defaultDesc = "SW.CommandFulfillWarrantDesc".Translate();
			command_Action.icon = TradeCommandTex;
			command_Action.action = delegate
			{
				Fulfill(caravan);
			};
			if (ActiveWarrants.All(x => ThingFromCaravan(x, caravan) == null))
            {
				command_Action.Disable("SW.CommandFulfillWarrantFailInsufficient".Translate(ActiveWarrants.Select(x => x.thing).First()));
			}
			return command_Action;
		}

        private void Fulfill(Caravan caravan)
		{
			foreach (var warrant in ActiveWarrants.ToList())
            {
				var thing = ThingFromCaravan(warrant, caravan);
				if (thing != null)
				{
					thing.holdingOwner.Remove(thing);
					warrant.GiveReward(caravan);
					var questTarget = thing is Corpse corpse ? corpse.InnerPawn : thing;
					QuestUtility.SendQuestTargetSignals(questTarget.questTags, "WarrantRequestFulfilled", parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
					WarrantsManager.Instance.acceptedWarrants.Remove(warrant);
					thing.Destroy();
				}
			}
		}

        private Thing ThingFromCaravan(Warrant warrant, Caravan caravan)
        {
			foreach (var thing in CaravanInventoryUtility.AllInventoryItems(caravan).Concat(caravan.PawnsListForReading))
            {
                if (warrant.thing is Pawn pawn && pawn.Dead && thing == pawn.Corpse)
                {
					return pawn.Corpse;
				}

                if (thing == warrant.thing)
                {
                    return thing;
                }
            }
			return null;
		}
    }
}
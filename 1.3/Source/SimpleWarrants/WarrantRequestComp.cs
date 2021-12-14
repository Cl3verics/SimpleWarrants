using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Sound;

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
		public IEnumerable<Warrant> ActiveWarrants => WarrantsManager.Instance.acceptedWarrants?.Where(x => x.issuer == this.parent.Faction && x.IsWarrantActive());
		public bool ActiveRequest => ActiveWarrants.Any();
		public override string CompInspectStringExtra()
		{
			if (ActiveRequest)
			{
				var requestInfo = string.Join(", ", ActiveWarrants.Select(x => x.thing.LabelCap));
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
					QuestUtility.SendQuestTargetSignals(thing.questTags, "WarrantRequestFulfilled", parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
					WarrantsManager.Instance.acceptedWarrants.Remove(warrant);
					thing.holdingOwner.Remove(thing);
					thing.Destroy();
					warrant.GiveReward(caravan);
				}
			}
		}

		private Thing ThingFromCaravan(Warrant warrant, Caravan caravan)
        {
			return CaravanInventoryUtility.AllInventoryItems(caravan).Concat(caravan.PawnsListForReading).FirstOrDefault(x => x == warrant.thing);
		}
	}
}
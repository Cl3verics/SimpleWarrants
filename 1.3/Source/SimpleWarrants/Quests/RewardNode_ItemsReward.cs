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
using Verse.Grammar;
using Verse.Sound;

namespace SimpleWarrants
{
	public class RewardNode_ItemsReward : RewardNode
	{
		public List<ThingDefCountClass> things;
		public List<Thing> items;
		public RewardNode_ItemsReward()
		{

		}
		public float TotalMarketValue
		{
			get
			{
				float num = 0f;
				for (int i = 0; i < items.Count; i++)
				{
					Thing innerIfMinified = items[i].GetInnerIfMinified();
					num += innerIfMinified.MarketValue * (float)items[i].stackCount;
				}
				return num;
			}
		}
		public override IEnumerable<Reward> GenerateRewards(Slate slate)
		{
			items = new List<Thing>();
			foreach (var t in things)
            {
				var thing = ThingMaker.MakeThing(t.thingDef);
				thing.stackCount = t.count;
				items.Add(thing);
			}
			var reward = new Reward_Items()
			{
				items = items
			};
			yield return reward;
		}

		public override IEnumerable<QuestPart> GenerateQuestParts(Slate slate)
		{
			QuestPart_DropPods dropPods = new QuestPart_DropPods();
			dropPods.inSignal = (QuestGenUtility.HardcodedSignalWithQuestID(inSignal) ?? QuestGen.slate.Get<string>("inSignal"));
			dropPods.outSignalResult = (QuestGenUtility.HardcodedSignalWithQuestID(outSignalChoiceAccepted) ?? QuestGen.slate.Get<string>("outSignalChoiceAccepted"));
			dropPods.mapParent = slate.Get<Map>("map").Parent;
			dropPods.useTradeDropSpot = true;
			dropPods.Things = items;
			slate.Set("itemsReward_items", items);
			slate.Set("itemsReward_totalMarketValue", TotalMarketValue);
			yield return dropPods;
		}
	}
}
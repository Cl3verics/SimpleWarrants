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
	public class SitePartWorker_Camp : SitePartWorker_Outpost
	{
		public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
		{
			base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
			part.things = new ThingOwner<Pawn>(part, oneStackOnly: true);
			part.things.TryAdd(slate.Get<Pawn>("victim"));
		}
	}

	public class GenStep_Camp : GenStep_Outpost
	{
        public override void Generate(Map map, GenStepParams parms)
        {
            base.Generate(map, parms);
			var pawn = (Pawn)parms.sitePart.things.Take(parms.sitePart.things[0]);
			var faction = map.ParentFaction;
			if (pawn.Faction != faction)
            {
				pawn.SetFaction(faction);
			}
			var pawns = map.mapPawns.SpawnedPawnsInFaction(faction);
			var cell = CellFinder.RandomClosewalkCellNear(pawns.RandomElement().Position, map, 5);
			GenSpawn.Spawn(pawn, cell, map);
			var warrant = WarrantsManager.Instance.acceptedWarrants.First(x => x.thing == pawn);
			pawns.FirstOrDefault().GetLord().AddPawn(pawn);
		}
    }
}
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
	public class SitePartWorker_ArtifactStash : SitePartWorker
	{
		public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
		{
			base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
			part.things = new ThingOwner<Thing>(part, oneStackOnly: true);
			var artifact = slate.Get<Thing>("artifact");
			Log.Message("TEST: " + part.things.GetCountCanAccept(artifact));
			var result = part.things.TryAdd(artifact, false);

			Log.Message(result + " - artifact: " + artifact + " - " + part.things.Count + " - " + artifact.holdingOwner);
		}
	}

	public class GenStep_ArtifactStash : GenStep_ItemStash
    {
        public override void Generate(Map map, GenStepParams parms)
        {
			var artifact = parms.sitePart.things[0];
			var warrant = WarrantsManager.Instance.acceptedWarrants.First(x => x.thing == artifact);
			warrant.spawned = true;
			Log.Message("artifact: " + artifact);
			base.Generate(map, parms);
		}
	}
}
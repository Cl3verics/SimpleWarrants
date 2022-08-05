using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;
using Verse.Grammar;

namespace SimpleWarrants
{
    public class SitePartWorker_Pawn : SitePartWorker_Outpost
	{
        public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
		{
			base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
			part.things = new ThingOwner<Pawn>(part, oneStackOnly: true);
			part.things.TryAdd(slate.Get<Pawn>("victim"));
		}
    }
}
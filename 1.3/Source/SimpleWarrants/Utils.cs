using RimWorld;
using RimWorld.Planet;
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

    [DefOf]
    public static class SW_DefOf
    {
        public static RulePackDef SW_WantedFor;
        public static QuestScriptDef SW_Warrant_Pawn;
        public static QuestScriptDef SW_Warrant_Animal;
        public static QuestScriptDef SW_Warrant_Artifact;
        public static IncidentDef SW_Visitors;
    }
    public static class Utils
    {
        [DebugAction("General", "Populate warrants (x15)")]
        private static void PopulateWarrants()
        {
            WarrantsManager.Instance.PopulateWarrants(15);
        }

        public static Faction AnyHostileToPlayerFaction()
        {
            return Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated && !faction.Hidden && !faction.IsPlayer
                                        && faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Hostile 
                                        && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction))
                                        .RandomElement();
        }
        public static IEnumerable<ThingDef> AllArtifactDefs => DefDatabase<ThingDef>.AllDefs.Where(x => (x.tradeTags?.Contains("Artifact") ?? false)
                    || (x.thingCategories?.Contains(ThingCategoryDefOf.Artifacts) ?? false)
                    || (x.tradeTags?.Contains("ExoticMisc") ?? false));

        public static IEnumerable<PawnKindDef> AllWorthAnimalDefs => DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race.race.Animal && x.race.GetStatValueAbstract(StatDefOf.MarketValue) >= 400);
        public static TaggedString GenerateTextFromRule(RulePackDef rule, int seed = -1)
        {
            if (seed != -1)
            {
                Rand.PushState();
                Rand.Seed = seed;
            }
            string rootKeyword = rule.RulesPlusIncludes.Where(x => x.keyword == "r_logentry").RandomElement().keyword;
            GrammarRequest request = default(GrammarRequest);
            request.Includes.Add(rule);
            string str = GrammarResolver.Resolve(rootKeyword, request);
            if (seed != -1)
            {
                Rand.PopState();
            }
            return str;
        }

        public static HashSet<string> GenerateAllTextFromRule(RulePackDef rule)
        {
            HashSet<string> results = new HashSet<string>();
            for (var i = 0;i < 100; i++)
            {
                string rootKeyword = rule.FirstRuleKeyword;
                GrammarRequest request = default(GrammarRequest);
                request.Includes.Add(rule);
                string str = GrammarResolver.Resolve(rootKeyword, request);
                results.Add(str);
            }
            return results;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.Grammar;

namespace SimpleWarrants
{
    public static class Utils
    {
        public static IEnumerable<ThingDef> AllArtifactDefs => DefDatabase<ThingDef>.AllDefs.Where(x => (x.tradeTags?.Contains("Artifact") ?? false)
                    || (x.thingCategories?.Contains(SW_DefOf.Artifacts) ?? false)
                    || (x.tradeTags?.Contains("ExoticMisc") ?? false));

        public static IEnumerable<PawnKindDef> AllWorthAnimalDefs => DefDatabase<PawnKindDef>.AllDefs.Where(x => x.race.race.Animal && x.race.GetStatValueAbstract(StatDefOf.MarketValue) >= 400);

        [DebugAction("General", "Populate warrants (x15)")]
        private static void PopulateWarrants()
        {
            WarrantsManager.Instance.PopulateWarrants(15);
        }

        [DebugAction("General", "Put warrant on pawn", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void PutWarrantOn(Pawn p)
        {
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(PutWarrant(p)));
        }

        public static List<DebugMenuOption> PutWarrant(Pawn pawn)
        {
            var list = new List<DebugMenuOption>();
            foreach (var issuer in Find.FactionManager.AllFactions)
            {
                list.Add(new DebugMenuOption(issuer.Name, DebugMenuOptionMode.Action, () => WarrantsManager.Instance.PutWarrantOn(pawn, DefDatabase<WarrantReasonDef>.AllDefs.RandomElement(), issuer)));
            }
            return list;
        }

        public static Faction AnyHostileToPlayerFaction()
        {
            return Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && faction.defeated is false && faction.Hidden is false && faction.IsPlayer is false
                                        && faction.RelationKindWith(Faction.OfPlayer) == FactionRelationKind.Hostile
                                        && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction))
                                        .RandomElement();
        }

        public static TaggedString GenerateTextFromRule(RulePackDef rule, int seed = -1)
        {
            if (seed != -1)
            {
                Rand.PushState();
                Rand.Seed = seed;
            }
            var rootKeyword = rule.RulesPlusIncludes.Where(x => x.keyword == "r_logentry").RandomElement().keyword;
            GrammarRequest request = default;
            request.Includes.Add(rule);
            var str = GrammarResolver.Resolve(rootKeyword, request);
            if (seed != -1)
            {
                Rand.PopState();
            }
            return str;
        }

        public static List<Thing> AllPlayerSilver()
        {
            var result = new List<Thing>();
            foreach (var map in Find.Maps)
            {
                if (map.IsPlayerHome is false) continue;
                result.AddRange(map.listerThings.ThingsOfDef(ThingDefOf.Silver)
                    .Where(s => s.Position.Fogged(s.Map) is false &&
                                (map.areaManager.Home[s.Position] || s.IsInAnyStorage())));
            }
            return result;
        }

        public static bool PlayerHomeIsOrbital()
        {
            var home = Find.AnyPlayerHomeMap;
            if (home == null) return false;
            return ModsConfig.OdysseyActive &&
                    home.Tile.LayerDef == PlanetLayerDefOf.Orbit;
        }
    }
}
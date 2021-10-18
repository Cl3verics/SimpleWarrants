using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SimpleWarrants
{
    public class WarrantsManager : GameComponent
    {
        public static WarrantsManager Instance;
        public List<Warrant> availableWarrants;
        public List<Warrant> acceptedWarrants;
        public List<Warrant> givenWarrants;
        public bool initialized;
        public int lastWarrantID;
        public WarrantsManager()
        {
            Instance = this;
        }

        public WarrantsManager(Game game)
        {
            Instance = this;
        }

        public void PreInit()
        {
            Instance = this;
            availableWarrants ??= new List<Warrant>();
            acceptedWarrants ??= new List<Warrant>();
            givenWarrants ??= new List<Warrant>();
        }

        public override void StartedNewGame()
        {
            PreInit();
            base.StartedNewGame();
            if (!initialized && !availableWarrants.Any())
            {
                PopulateWarrants();
            }
        }

        public override void LoadedGame()
        {
            PreInit();
            base.LoadedGame();
            if (!initialized && !availableWarrants.Any())
            {
                PopulateWarrants();
            }
        }

        public void PopulateWarrants()
        {
            var count = Rand.RangeInclusive(3, 5);
            for (var i = 0; i < count; i++)
            {
                availableWarrants.Add(GetRandomWarrant());
            }
        }

        private Warrant GetRandomWarrant()
        {
            var issuer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated && !faction.Hidden && !faction.IsPlayer 
            && faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction)).RandomElement();
            if (Rand.Chance(0.5f))
            {
                var warrant = new Warrant_Pawn
                {
                    loadID = GetWarrantID(),
                    issuer = issuer
                };
                var randomKind = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.RaceProps.Humanlike).RandomElement();
                Faction faction = null;
                if (randomKind.defaultFactionType != null)
                {
                    faction = Find.FactionManager.FirstFactionOfDef(randomKind.defaultFactionType);
                }
                if (faction is null)
                {
                    faction = Find.FactionManager.AllFactions.Where(x => x.def.humanlikeFaction && !x.defeated && !x.IsPlayer && !x.Hidden).RandomElement();
                }
                warrant.thing = PawnGenerator.GeneratePawn(randomKind, faction);
                warrant.rewardForLiving = (int)(warrant.pawn.MarketValue * Rand.Range(0.5f, 2f));
                if (Rand.Chance(0.3f))
                {
                    warrant.rewardForDead = 0;
                }
                else
                {
                    warrant.rewardForDead = (int)(warrant.rewardForLiving * Rand.Range(0.3f, 0.7f));
                }
                warrant.reason = Utils.GenerateTextFromRule(SW_DefOf.SW_WantedFor, warrant.pawn.thingIDNumber);
                return warrant;
            }
            else
            {
                var warrant = new Warrant_Artifact
                {
                    loadID = GetWarrantID(),
                    issuer = issuer
                };
                var artifacts = DefDatabase<ThingDef>.AllDefs.Where(x => (x.tradeTags?.Contains("Artifact") ?? false) || (x.thingCategories?.Contains(ThingCategoryDefOf.Artifacts) ?? false));
                var randomArtifact = artifacts.RandomElement();
                warrant.thing = ThingMaker.MakeThing(randomArtifact);
                warrant.reward = (int)(warrant.thing.MarketValue * Rand.Range(0.5f, 2f));
                return warrant;
            }
        }

        public string GetWarrantID()
        {
            lastWarrantID++;
            return "Warrant" + lastWarrantID;
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            if (Find.TickManager.TicksGame % 3000 == 0)
            {

            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref availableWarrants, "warrants", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Values.Look(ref lastWarrantID, "lastWarrantID");
            PreInit();
        }
    }
}
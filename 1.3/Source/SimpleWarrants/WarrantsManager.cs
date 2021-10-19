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
        public List<Warrant> takenWarrants;
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
            takenWarrants ??= new List<Warrant>();
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
                    issuer = issuer,
                    createdTick = Find.TickManager.TicksGame
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
                    issuer = issuer,
                    createdTick = Find.TickManager.TicksGame
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
            if (Find.TickManager.TicksGame % 60 == 0)
            {
                for (int num = availableWarrants.Count - 1; num >= 0; num--)
                {
                    if ((Find.TickManager.TicksGame - availableWarrants[num].createdTick).TicksToDays() >= 15)
                    {
                        availableWarrants.RemoveAt(num);
                    }
                }
            }

            if (Rand.MTBEventOccurs(7, GenDate.TicksPerDay, 1))
            {
                availableWarrants.Add(GetRandomWarrant());
            }

            for (int num = givenWarrants.Count - 1; num >= 0; num--)
            {
                var warrant = givenWarrants[num];
                if (Rand.Chance(warrant.AcceptChance() / (GenDate.TicksPerDay * 15)))
                {
                    var accepteer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated 
                    && !faction.Hidden && !faction.IsPlayer && warrant.issuer != faction && faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile 
                    && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction)).RandomElement();
                    warrant.AcceptBy(accepteer);
                    givenWarrants.RemoveAt(num);
                    takenWarrants.Add(warrant);
                    warrant.tickToBeCompleted = Find.TickManager.TicksGame + (GenDate.TicksPerDay * Rand.Range(3, 20));
                    Messages.Message("SW.FactionTookYourWarrant".Translate(accepteer.Named("FACTION"), warrant.thing.LabelCap), MessageTypeDefOf.PositiveEvent);
                }
            }

            for (int num = takenWarrants.Count - 1; num >= 0; num--)
            {
                var warrant = takenWarrants[num];
                if (Find.TickManager.TicksGame > warrant.tickToBeCompleted)
                {
                    takenWarrants.RemoveAt(num);
                    if (Rand.Chance(warrant.SuccessChance()))
                    {
                        var reward = 0;
                        bool dead = false;
                        if (warrant is Warrant_Pawn wp)
                        {
                            if (!Rand.Chance(wp.rewardForLiving / wp.rewardForDead))
                            {
                                dead = true;
                            }
                            reward = wp.rewardForDead > wp.rewardForLiving ? wp.rewardForDead : dead ? wp.rewardForDead : wp.rewardForLiving;
                        }
                        else if (warrant is Warrant_Artifact wa)
                        {
                            reward = wa.reward;
                        }
                        var map = Find.AnyPlayerHomeMap;
                        var silvers = map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where((Thing x) => !x.Position.Fogged(x.Map) && (map.areaManager.Home[x.Position] || x.IsInAnyStorage())).ToList();

                        string title = "SW.FactionCompletedWarrant".Translate(warrant.accepteer.Named("FACTION"));
                        DiaNode diaNode = new DiaNode("SW.FactionCompletedWarrantDesc".Translate(warrant.accepteer.Named("FACTION"), warrant.thing.LabelCap, reward));
                        DiaOption payOption = new DiaOption("SW.Pay".Translate(reward));
                        payOption.action = delegate
                        {
                            while (reward > 0)
                            {
                                Thing thing = silvers.RandomElement();
                                silvers.Remove(thing);
                                if (thing == null)
                                {
                                    break;
                                }
                                int num = Math.Min(reward, thing.stackCount);
                                thing.SplitOff(num).Destroy();
                                reward -= num;
                            }
                            var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.FactionArrival, map);
                            parms.faction = warrant.accepteer;
                            var toDeliver = warrant.thing;
                            if (dead)
                            {
                                var pawn = warrant.thing as Pawn;
                                pawn.Kill(null);
                                toDeliver = pawn.Corpse;
                            }
                            else
                            {
                                var pawn = warrant.thing as Pawn;
                                if (pawn != null)
                                {
                                    HealthUtility.DamageUntilDowned(pawn);
                                }
                            }
                            IncidentWorker_Visitors.toDeliver = toDeliver;
                            SW_DefOf.SW_Visitors.Worker.TryExecute(parms);
                            IncidentWorker_Visitors.toDeliver = null;
                        };
                        payOption.resolveTree = true;
                        if (silvers.Sum(x => x.stackCount) < reward)
                        {
                            payOption.Disable("SW.NotEnoughSilver".Translate());
                        }
                        diaNode.options.Add(payOption);

                        DiaOption refuseOption = new DiaOption("SW.Refuse".Translate());
                        refuseOption.action = delegate
                        {
                            warrant.accepteer.TryAffectGoodwillWith(Faction.OfPlayer, -100, true, true);
                            var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                            parms.faction = warrant.accepteer;
                            IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
                        };
                        refuseOption.resolveTree = true;
                        diaNode.options.Add(refuseOption);
                        Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(diaNode, warrant.accepteer, delayInteractivity: true, radioMode: false, title));
                        Find.Archive.Add(new ArchivedDialog(diaNode.text, title, warrant.accepteer));
                    }
                    else
                    {
                        Messages.Message("SW.FactionFailedWarrant".Translate(warrant.accepteer.Named("FACTION"), warrant.thing.LabelCap), MessageTypeDefOf.NegativeEvent);
                        warrant.accepteer.TryAffectGoodwillWith(Faction.OfPlayer, -30);
                    }
                }
            }

        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref availableWarrants, "warrants", LookMode.Deep);
            Scribe_Collections.Look(ref acceptedWarrants, "acceptedWarrants", LookMode.Deep);
            Scribe_Collections.Look(ref givenWarrants, "givenWarrants", LookMode.Deep);
            Scribe_Collections.Look(ref takenWarrants, "takenWarrants", LookMode.Deep);
            Scribe_Values.Look(ref initialized, "initialized");
            Scribe_Values.Look(ref lastWarrantID, "lastWarrantID");
            PreInit();
        }
    }
}
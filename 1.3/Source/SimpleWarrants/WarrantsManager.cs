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
                PopulateWarrants(Rand.RangeInclusive(3, 5));
                initialized = true;
            }
        }

        public override void LoadedGame()
        {
            PreInit();
            base.LoadedGame();
            if (!initialized && !availableWarrants.Any())
            {
                PopulateWarrants(Rand.RangeInclusive(3, 5));
                initialized = true;
            }
        }

        public void PopulateWarrants(int amountToPopulate)
        {
            int num = 0;
            var count = 0;
            while (count < amountToPopulate && num < amountToPopulate * 2)
            {
                num++;
                var warrant = GetRandomWarrant(false);
                if (warrant.CanPlayerReceive())
                {
                    availableWarrants.Add(warrant);
                    count++;
                }
            }
        }

        private Warrant GetRandomWarrant(bool includeColonists = true)
        {
            if (Rand.Chance(0.5f) || !SimpleWarrantsSettings.enableWarrantsOnArtifact)
            {
                var warrant = new Warrant_Pawn
                {
                    loadID = GetWarrantID(),
                    createdTick = Find.TickManager.TicksGame
                };

                if (Rand.Chance(0.3f))
                {
                    warrant.thing = PawnGenerator.GeneratePawn(Utils.AllWorthAnimalDefs.RandomElement(), null);
                    warrant.issuer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated && !faction.Hidden && !faction.IsPlayer
                            && faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction))
                            .RandomElement();
                }
                else
                {
                    var pawns = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists.Where(x => x.HomeFaction == Faction.OfPlayer && x.RaceProps.Humanlike).ToList();
                    if (Rand.Chance(1f - SimpleWarrantsSettings.chanceOfWarrantsMadeOnColonist) || !pawns.Any() || !includeColonists || !SimpleWarrantsSettings.enableWarrantsOnColonists)
                    {
                        var randomKind = DefDatabase<PawnKindDef>.AllDefs.Where(x => x.RaceProps.Humanlike && x.defaultFactionType != Faction.OfPlayer.def).RandomElement();
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
                        warrant.issuer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated && !faction.Hidden && !faction.IsPlayer
                                && faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction))
                                .RandomElement();
                    }
                    else
                    {
                        var colonist = pawns.RandomElement();
                        warrant.thing = colonist;
                        warrant.issuer = Utils.AnyHostileToPlayerFaction();
                        Find.LetterStack.ReceiveLetter("SW.WarrantOnYourColonist".Translate(colonist.Named("PAWN")), "SW.WarrantOnYourColonistDesc".Translate(colonist.Named("PAWN"))
                            , LetterDefOf.NegativeEvent, colonist);
                    }
                    warrant.reason = Utils.GenerateTextFromRule(SW_DefOf.SW_WantedFor, warrant.pawn.thingIDNumber);
                }
                AssignRewards(warrant);
                return warrant;
            }
            else
            {
                var warrant = new Warrant_Artifact
                {
                    loadID = GetWarrantID(),
                    createdTick = Find.TickManager.TicksGame
                };
                warrant.issuer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated && !faction.Hidden && !faction.IsPlayer
                    && faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction))
                    .RandomElement();
                var artifacts = Utils.AllArtifactDefs;
                var randomArtifact = artifacts.RandomElement();
                warrant.thing = ThingMaker.MakeThing(randomArtifact);
                warrant.reward = (int)(warrant.thing.MarketValue * Rand.Range(0.5f, 2f));
                return warrant;
            }
        }

        private static void AssignRewards(Warrant_Pawn warrant)
        {
            var baseReward = (int)(warrant.pawn.MarketValue * Rand.Range(0.5f, 2f));
            if (!warrant.thing.def.race.Animal)
            {
                warrant.rewardForLiving = baseReward;
            }
            if (Rand.Chance(0.3f) && !warrant.thing.def.race.Animal)
            {
                warrant.rewardForDead = 0;
            }
            else
            {
                warrant.rewardForDead = (int)(baseReward * Rand.Range(0.3f, 0.7f));
            }
        }

        public void PutWarrantOn(Pawn victim, string reason, Faction issuer = null)
        {
            var warrant = new Warrant_Pawn
            {
                loadID = GetWarrantID(),
                createdTick = Find.TickManager.TicksGame
            };
            warrant.thing = victim;
            if (issuer != null)
            {
                warrant.issuer = issuer;
            }
            else
            {
                warrant.issuer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated && !faction.Hidden && !faction.IsPlayer
                     && faction.RelationKindWith(victim.Faction) == FactionRelationKind.Hostile && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction))
                     .RandomElement();
            }
            warrant.reason = reason;
            Find.LetterStack.ReceiveLetter("SW.WarrantOnYourColonistReason".Translate(victim.Named("PAWN"), reason), 
                "SW.WarrantOnYourColonistDesc".Translate(victim.Named("PAWN")), LetterDefOf.NegativeEvent, victim);
            AssignRewards(warrant);
            availableWarrants.Add(warrant);
        }
        public string GetWarrantID()
        {
            lastWarrantID++;
            return "Warrant" + lastWarrantID;
        }
        public override void GameComponentTick()
        {
            base.GameComponentTick();
            HandleAvailableWarrants();
            HandleAcceptedWarrants();
            HandleGivenWarrants();
            HandleFactionsTakenWarrants();
        }

        private void HandleAvailableWarrants()
        {
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
                var warrant = GetRandomWarrant();
                if (warrant.CanPlayerReceive())
                {
                    availableWarrants.Add(warrant);
                }
            }

            foreach (var warrant in availableWarrants.Where(x => x.IsThreatForPlayer()))
            {
                if (Rand.MTBEventOccurs(3, GenDate.TicksPerDay, 1))
                {
                    var map = Find.AnyPlayerHomeMap;
                    var parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
                    parms.faction = Find.FactionManager.AllFactionsVisible.Where(x => x.def.humanlikeFaction && x.HostileTo(Faction.OfPlayer)).RandomElement();
                    IncidentDefOf.RaidEnemy.Worker.TryExecute(parms);
                }
            }
        }

        public void HandleAcceptedWarrants()
        {
            for (int num = acceptedWarrants.Count - 1; num >= 0; num--)
            {
                var warrant = acceptedWarrants[num];
                if (!warrant.IsWarrantActive())
                {
                    warrant.End();
                    if (Rand.Chance(0.25f))
                    {
                        var pawns = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists_NoSlaves;
                        if (pawns.TryRandomElement(out var pawn))
                        {
                            PutWarrantOn(pawn, "SW.Fraud".Translate(), warrant.issuer);
                        }
                    }
                    acceptedWarrants.RemoveAt(num);
                }
            }
        }
        public void HandleGivenWarrants()
        {
            for (int num = givenWarrants.Count - 1; num >= 0; num--)
            {
                var warrant = givenWarrants[num];
                var chance = warrant.AcceptChance() / (GenDate.TicksPerDay * 7);
                var success = Rand.Chance(chance);
                if (success)
                {
                    var accepteer = Find.FactionManager.AllFactions.Where(faction => faction.def.humanlikeFaction && !faction.defeated
                    && !faction.Hidden && !faction.IsPlayer && warrant.issuer != faction && faction.RelationKindWith(Faction.OfPlayer) != FactionRelationKind.Hostile
                    && Find.World.worldObjects.Settlements.Any(settlement => settlement.Faction == faction)).RandomElement();
                    warrant.AcceptBy(accepteer);
                    givenWarrants.RemoveAt(num);
                    takenWarrants.Add(warrant);
                    warrant.tickToBeCompleted = Find.TickManager.TicksGame + (GenDate.TicksPerDay * (int)Rand.Range(3f, 15f));
                    Messages.Message("SW.FactionTookYourWarrant".Translate(accepteer.Named("FACTION"), warrant.thing.LabelCap), MessageTypeDefOf.PositiveEvent);
                }
            }
        }

        private void HandleFactionsTakenWarrants()
        {
            for (int num = takenWarrants.Count - 1; num >= 0; num--)
            {
                var warrant = takenWarrants[num];
                if (warrant.accepteer.HostileTo(Faction.OfPlayer))
                {
                    takenWarrants.RemoveAt(num);
                    givenWarrants.Add(warrant);
                    Messages.Message("SW.FactionDroppedWarrant".Translate(warrant.accepteer.Named("FACTION"), warrant.thing.LabelCap), MessageTypeDefOf.NegativeEvent);
                }
                else if (Find.TickManager.TicksGame > warrant.tickToBeCompleted)
                {
                    takenWarrants.RemoveAt(num);
                    var chance = warrant.SuccessChance();
                    var success = Rand.Chance(chance);
                    if (success)
                    {
                        var reward = 0;
                        bool dead = false;
                        if (warrant is Warrant_Pawn wp)
                        {
                            if (wp.rewardForDead > 0 && !Rand.Chance(wp.rewardForLiving / wp.rewardForDead))
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
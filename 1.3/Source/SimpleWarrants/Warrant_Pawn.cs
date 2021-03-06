using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System.Linq;
using UnityEngine;
using Verse;

namespace SimpleWarrants
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Warrant_Pawn : Warrant
    {
        public static readonly Texture2D IconCapture = ContentFinder<Texture2D>.Get("UI/Warrants/IconCapture");
        public static readonly Texture2D IconDeath = ContentFinder<Texture2D>.Get("UI/Warrants/IconDeath");
        public Pawn pawn
        {
            get
            {
                if (thing is Corpse corpse)
                {
                    return corpse.InnerPawn;
                }
                return thing as Pawn;
            }
        }
        public string reason;

        public int rewardForLiving;

        public int rewardForDead;
        public override void Draw(Rect rect, bool doAcceptAndDeclineButtons = true, bool doCompensateWarrantButton = false)
        {
            base.Draw(rect, doAcceptAndDeclineButtons, doCompensateWarrantButton);
            var pawnRect = new Rect(new Vector2(rect.x + 90, rect.y + 10), new Vector2(rect.height * 0.722f, rect.height));
            Vector2 pos = new Vector2(pawnRect.width, pawnRect.height);
            GUI.DrawTexture(pawnRect, PortraitsCache.Get(pawn, pos, Rot4.South, new Vector3(0f, 0f, 0f), 1.2f));
            Widgets.InfoCardButton(pawnRect.xMax - 24, pawnRect.yMax - 24, pawn);

            Text.Font = GameFont.Medium;
            var pawnName = this.pawn.RaceProps.Animal ? this.pawn.def.LabelCap.ToString() : pawn.Name.ToString();
            var textSize = Text.CalcSize(pawnName);
            var nameInfoBox = new Rect(pawnRect.xMax, pawnRect.y, textSize.x, 30);
            Widgets.Label(nameInfoBox, pawnName);

            if (this.issuer.IsPlayer && this.MaxReward() < (pawn.MarketValue * 0.75f))
            {
                var insufficientRewardBox = new Rect(nameInfoBox.xMax + 5, nameInfoBox.y + 3, 24, 24);
                GUI.DrawTexture(insufficientRewardBox, InsufficientRewardIcon);
                TooltipHandler.TipRegion(insufficientRewardBox, "SW.InsufficientReward".Translate());
            }
            var wantedForInfoBox = new Rect(nameInfoBox.x, nameInfoBox.yMax, rect.width - pawnRect.width, nameInfoBox.height);
            if (!pawn.RaceProps.Animal)
            {
                Widgets.Label(wantedForInfoBox, "SW.WantedFor".Translate(reason.Colorize(Color.yellow), issuer.NameColored));
            }
            else
            {
                Widgets.Label(wantedForInfoBox, "SW.PostedBy".Translate(issuer.NameColored));
            }

            var rewardsForDeadIconBox = new Rect(wantedForInfoBox.x, wantedForInfoBox.yMax, 24, 24);
            GUI.DrawTexture(rewardsForDeadIconBox, IconDeath);

            var rewardsForDeadInfoBox = new Rect(rewardsForDeadIconBox.xMax + 5, wantedForInfoBox.yMax, wantedForInfoBox.width / 3, wantedForInfoBox.height);
            if (this.rewardForDead > 0)
            {
                Widgets.Label(rewardsForDeadInfoBox, this.rewardForDead + " " + ThingDefOf.Silver.LabelCap);
            }
            else
            {
                Widgets.Label(rewardsForDeadInfoBox, "SW.NoReward".Translate());
            }

            var rewardsForLivingIconBox = new Rect(rewardsForDeadInfoBox.xMax, wantedForInfoBox.yMax, 24, 24);
            var rewardsForLivingInfoBox = new Rect(rewardsForLivingIconBox.xMax + 5, wantedForInfoBox.yMax, wantedForInfoBox.width / 3, wantedForInfoBox.height);

            if (!this.pawn.RaceProps.Animal || this.issuer.IsPlayer)
            {
                GUI.DrawTexture(rewardsForLivingIconBox, IconCapture);
                Widgets.Label(rewardsForLivingInfoBox, this.rewardForLiving + " " + ThingDefOf.Silver.LabelCap);
            }

            var infoBox = new Rect(rect.width - 250, rewardsForLivingInfoBox.yMax + 40, 250, 24);
            Text.Font = GameFont.Tiny;
            if (this.issuer != Faction.OfPlayer)
            {
                var expireDate = (this.relatedQuest != null ? this.acceptedTick : this.createdTick) + (GenDate.TicksPerDay * 15) - Find.TickManager.TicksGame;
                Widgets.Label(infoBox, "SW.WillExpireIn".Translate(expireDate.ToStringTicksToDays()));
            }
            else
            {
                if (this.accepteer != null)
                {
                    Widgets.Label(infoBox, "SW.ApproximateComplectionDate".Translate(ApproximateCompletionDate.ToStringTicksToDays()));
                }
                else
                {
                    Widgets.Label(infoBox, "SW.ApproximateAcceptionDate".Translate(ApproximateAcceptionDate.ToStringTicksToDays()));
                }
            }
            Text.Font = GameFont.Small;
        }

        public override void DoAcceptAction()
        {
            base.DoAcceptAction();
            Slate slate = new Slate();
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.World));
            slate.Set("asker", issuer.leader);
            slate.Set("victim", pawn);
            slate.Set("reason", reason);
            slate.Set("warrant", this);
            slate.Set("rewardForLiving", this.rewardForLiving);
            slate.Set("rewardForDead", this.rewardForDead);
            var questDef = this.pawn.RaceProps.Animal ? SW_DefOf.SW_Warrant_Animal : SW_DefOf.SW_Warrant_Pawn;
            var quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
            QuestUtility.SendLetterQuestAvailable(quest);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref reason, "reason");
            Scribe_Values.Look(ref rewardForLiving, "rewardForLiving");
            Scribe_Values.Look(ref rewardForDead, "rewardForDead");
        }

        public override bool IsWarrantActive()
        {
            if (rewardForDead == 0 && (pawn?.Dead ?? false))
            {
                return false;
            }
            else if (pawn?.Corpse is Corpse corpse)
            {
                if (thing != corpse)
                {
                    thing = corpse;
                }
                if (rewardForDead > 0 && corpse.ParentHolder is null && !corpse.Spawned)
                {
                    return false;
                }
            }
            else if (pawn is null || pawn.Destroyed)
            {
                return false;
            }
            return true;
        }

        public override void GiveReward(Caravan caravan)
        {
            base.GiveReward(caravan);
            var rewardAmount = 0;
            if (rewardForDead > 0 && (thing is Corpse || pawn.Dead))
            {
                rewardAmount = rewardForDead;
            }
            else if (!pawn.Dead)
            {
                rewardAmount = rewardForLiving;
            }
            if (rewardAmount > 0)
            {
                var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = rewardAmount;
                GiveThing(caravan, silver);
            }
        }
        public override void DoCompensateAction()
        {
            var map = Find.CurrentMap ?? Find.AnyPlayerHomeMap;
            var silvers = map.listerThings.ThingsOfDef(ThingDefOf.Silver).Where((Thing x) => !x.Position.Fogged(x.Map) && (map.areaManager.Home[x.Position] || x.IsInAnyStorage())).ToList();
            var toCompensate = Mathf.Max(rewardForDead, rewardForLiving);
            if (silvers.Sum(x => x.stackCount) >= toCompensate)
            {
                Pay(silvers, toCompensate);
                WarrantsManager.Instance.availableWarrants.Remove(this);
            }
            else
            {
                Messages.Message("SW.NoEnoughMoneyToCompensate".Translate(toCompensate), MessageTypeDefOf.CautionInput);
            }
        }

        public override float AcceptChance()
        {
            if (!acceptChanceCached.HasValue)
            {
                var reward = Mathf.Max(rewardForDead, rewardForLiving);
                acceptChanceCached = reward / thing.MarketValue;
            }
            return acceptChanceCached.Value;
        }

        public override float SuccessChance()
        {
            if (!successChanceCached.HasValue)
            {
                var reward = Mathf.Max(rewardForDead, rewardForLiving);
                successChanceCached = reward / thing.MarketValue;
            }
            return successChanceCached.Value;
        }

        public override bool ShouldShowCompensateButton()
        {
            return this.issuer != Faction.OfPlayer && this.accepteer != Faction.OfPlayer;
        }

        public override bool IsThreatForPlayer()
        {
            return this.pawn.Faction == Faction.OfPlayer;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            if (pawn.Faction != null && !pawn.Faction.HostileTo(Faction.OfPlayer))
            {
                pawn.Faction.TryAffectGoodwillWith(Faction.OfPlayer, -80);
            }
        }
        public override int MaxReward()
        {
            if (rewardForDead > rewardForLiving)
            {
                return rewardForDead;
            }
            return rewardForLiving;
        }
    }
}
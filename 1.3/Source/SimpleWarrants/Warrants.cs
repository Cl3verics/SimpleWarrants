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
using Verse.Sound;

namespace SimpleWarrants
{
    public enum WarrantStatus
    {
        Accepted,
        Completed,
        Failed,
        Expired
    }

    [HotSwappable]

    public abstract class Warrant : IExposable, ILoadReferenceable
    {
        public Thing thing;
        private bool savedSomewhere;
        public string loadID;
        public int acceptedTick = -1;
        public int createdTick = -1;
        public Faction issuer;
        public Quest relatedQuest;

        public WarrantStatus status;
        public Faction accepteer;
        public int tickToBeCompleted;
        public abstract float AcceptChance();
        public abstract float SuccessChance();
        public abstract bool IsWarrantActive();
        public abstract bool IsThreatForPlayer();
        public void DrawAcceptDeclineButtons(Rect rect)
        {
            var acceptRect = new Rect(rect.x + 5, rect.y + 50, 95, 30);
            if (Widgets.ButtonText(acceptRect, "Accept".Translate()))
            {
                DoAcceptAction();
            }

            var declineRect = new Rect(acceptRect.x, acceptRect.yMax + 10, acceptRect.width, acceptRect.height);
            if (Widgets.ButtonText(declineRect, "SW.Decline".Translate()))
            {
                WarrantsManager.Instance.availableWarrants.Remove(this);
            }
        }
        public void DrawCompensateButton(Rect rect)
        {
            var acceptRect = new Rect(rect.x + 5, rect.y + 65, 95, 30);
            if (Widgets.ButtonText(acceptRect, "SW.Compensate".Translate()))
            {
                DoCompensateAction();
            }
        }

        public void DrawRemoveWarrantButton(Rect rect)
        {
            var acceptRect = new Rect(rect.x + 5, rect.y + 65, 95, 30);
            if (Widgets.ButtonText(acceptRect, "SW.RemoveWarrant".Translate()))
            {
                WarrantsManager.Instance.givenWarrants.Remove(this);
            }
        }
        public abstract bool ShouldShowCompensateButton();
        public virtual void DoCompensateAction()
        {
        }

        public void Pay(List<Thing> silvers, int amountToPay)
        {
            while (amountToPay > 0)
            {
                Thing thing = silvers.RandomElement();
                silvers.Remove(thing);
                if (thing == null)
                {
                    break;
                }
                int num = Math.Min(amountToPay, thing.stackCount);
                thing.SplitOff(num).Destroy();
                amountToPay -= num;
            }
        }

        public virtual void DoAcceptAction()
        {
            WarrantsManager.Instance.availableWarrants.Remove(this);
            WarrantsManager.Instance.acceptedWarrants.Add(this);
            AcceptBy(Faction.OfPlayer);
        }

        public void AcceptBy(Faction faction)
        {
            acceptedTick = Find.TickManager.TicksGame;
            this.status = WarrantStatus.Accepted;
            this.accepteer = faction;
        }
        public virtual void Draw(Rect rect, bool doAcceptAndDeclineButtons = true, bool doCompensateWarrantButton = false)
        {
            Widgets.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), Color.gray, 1);
            if (doAcceptAndDeclineButtons)
            {
                DrawAcceptDeclineButtons(rect);
            }
            if (doCompensateWarrantButton && ShouldShowCompensateButton())
            {
                DrawCompensateButton(rect);
            }
            if (this.issuer == Faction.OfPlayer)
            {
                DrawRemoveWarrantButton(rect);
            }
        }

        public void End(QuestEndOutcome questEndOutcome = QuestEndOutcome.Fail)
        {
            this.relatedQuest?.End(questEndOutcome);
            this.issuer.TryAffectGoodwillWith(Faction.OfPlayer, -30);
        }
        public virtual void ExposeData()
        {
            if (thing is Pawn pawn && pawn.Corpse != null)
            {
                thing = pawn.Corpse;
            }
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                savedSomewhere = IsSavedSomewhereElse(thing);
            }
            Scribe_Values.Look(ref savedSomewhere, "savedSomewhere");
            Scribe_Values.Look(ref acceptedTick, "acceptedTick", -1);
            Scribe_References.Look(ref issuer, "issuer");
            Scribe_References.Look(ref accepteer, "accepteer");
            Scribe_Values.Look(ref tickToBeCompleted, "tickToBeCompleted");
            Scribe_Values.Look(ref loadID, "loadID");
            Scribe_References.Look(ref relatedQuest, "relatedQuest");
            if (!savedSomewhere)
            {
                Scribe_Deep.Look(ref thing, "thing");
            }
            else
            {
                Scribe_References.Look(ref thing, "thing");
            }

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (thing is null)
                {
                    Log.Error(this + " has null thing, bugged now and won't work.");
                }
            }
        }

        private bool IsSavedSomewhereElse(Thing thing)
        {
            if (thing is Pawn pawn)
            {
                if (Find.WorldPawns.Contains(pawn))
                {
                    return true;
                }
            }
            if (thing.holdingOwner != null)
            {
                return true;
            }
            return false;
        }

        public string GetUniqueLoadID()
        {
            return loadID;
        }

        public virtual void GiveReward(Caravan caravan)
        {
            this.status = WarrantStatus.Completed;
        }
    }

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
            var nameInfoBox = new Rect(pawnRect.xMax, pawnRect.y, rect.width - pawnRect.width, 30);
            Widgets.Label(nameInfoBox, pawn.Name.ToString());
            var wantedForInfoBox = new Rect(nameInfoBox.x, nameInfoBox.yMax, nameInfoBox.width, nameInfoBox.height);
            Widgets.Label(wantedForInfoBox, "SW.WantedFor".Translate(reason.Colorize(Color.yellow), issuer.NameColored));

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
            GUI.DrawTexture(rewardsForLivingIconBox, IconCapture);

            var rewardsForLivingInfoBox = new Rect(rewardsForLivingIconBox.xMax + 5, wantedForInfoBox.yMax, wantedForInfoBox.width / 3, wantedForInfoBox.height);
            Widgets.Label(rewardsForLivingInfoBox, this.rewardForLiving + " " + ThingDefOf.Silver.LabelCap);

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
            var quest = QuestUtility.GenerateQuestAndMakeAvailable(SW_DefOf.SW_Warrant_Pawn, slate);
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
            if (rewardForDead == 0 && pawn.Dead)
            {
                return false;
            }
            else if (pawn.Corpse is Corpse corpse)
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
            else if (pawn.Destroyed)
            {
                return false;
            }
            return true;
        }

        public override void GiveReward(Caravan caravan)
        {
            base.GiveReward(caravan);
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            if (rewardForDead > 0 && (thing is Corpse || pawn.Dead))
            {
                silver.stackCount = rewardForDead;
                CaravanInventoryUtility.GiveThing(caravan, silver);
            }
            else if (!pawn.Dead)
            {
                silver.stackCount = rewardForLiving;
                CaravanInventoryUtility.GiveThing(caravan, silver);
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
            var reward = Mathf.Max(rewardForDead, rewardForLiving);
            return reward / thing.MarketValue;
        }

        public override float SuccessChance()
        {
            var reward = Mathf.Max(rewardForDead, rewardForLiving);
            return reward / thing.MarketValue;
        }

        public override bool ShouldShowCompensateButton()
        {
            return this.issuer != Faction.OfPlayer && this.accepteer != Faction.OfPlayer;
        }

        public override bool IsThreatForPlayer()
        {
            return this.pawn.Faction == Faction.OfPlayer;
        }
    }
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Warrant_Artifact : Warrant
    {
        public static readonly Texture2D IconRetrieve = ContentFinder<Texture2D>.Get("UI/Warrants/IconRetrieve");
        public int reward;
        public override void Draw(Rect rect, bool doAcceptAndDeclineButtons = true, bool doCompensateWarrantButton = false)
        {
            base.Draw(rect, doAcceptAndDeclineButtons, doCompensateWarrantButton);
            var thingRect = new Rect(new Vector2(rect.x + 90, rect.y + 10), new Vector2(rect.height * 0.722f, rect.height * 0.722f));
            GUI.DrawTexture(thingRect, thing.Graphic.MatSouth.mainTexture);

            Widgets.InfoCardButton(thingRect.xMax - 24, thingRect.yMax + 18, thing);
            Text.Font = GameFont.Medium;
            var nameInfoBox = new Rect(thingRect.xMax, thingRect.y, 400, 30);
            Widgets.Label(nameInfoBox, thing.LabelCap);

            var postedByInfoBox = new Rect(nameInfoBox.x, nameInfoBox.yMax, nameInfoBox.width, nameInfoBox.height);
            Widgets.Label(postedByInfoBox, "SW.PostedBy".Translate(issuer.NameColored));

            var rewardIconBox = new Rect(nameInfoBox.x, postedByInfoBox.yMax, 24, 24);
            GUI.DrawTexture(rewardIconBox, IconRetrieve);
            var rewardInfoBox = new Rect(rewardIconBox.xMax + 5, postedByInfoBox.yMax, nameInfoBox.width, nameInfoBox.height);
            Widgets.Label(rewardInfoBox, this.reward + " " + ThingDefOf.Silver.LabelCap);

            Text.Font = GameFont.Small;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref reward, "reward");
        }

        public override void DoAcceptAction()
        {
            base.DoAcceptAction();
            Slate slate = new Slate();
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.World));
            slate.Set("asker", issuer.leader);
            slate.Set("artifactLabel", thing.Label);
            slate.Set("artifact", thing);
            slate.Set("warrant", this);
            slate.Set("reward", this.reward);
            var quest = QuestUtility.GenerateQuestAndMakeAvailable(SW_DefOf.SW_Warrant_Artifact, slate);
            QuestUtility.SendLetterQuestAvailable(quest);
        }
        public override void GiveReward(Caravan caravan)
        {
            base.GiveReward(caravan);
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            if (reward > 0)
            {
                silver.stackCount = reward;
                CaravanInventoryUtility.GiveThing(caravan, silver);
            }
        }
        public override bool IsWarrantActive()
        {
            if (thing.Destroyed)
            {
                return false;
            }
            return true;
        }

        public override float AcceptChance()
        {
            return reward / thing.MarketValue;
        }

        public override float SuccessChance()
        {
            return reward / thing.MarketValue;
        }

        public override bool ShouldShowCompensateButton()
        {
            return false;
        }

        public override bool IsThreatForPlayer()
        {
            return false;
        }
    }
}
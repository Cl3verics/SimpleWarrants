﻿using RimWorld;
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
    [HotSwappable]

    public abstract class Warrant : IExposable, ILoadReferenceable
    {
        public Thing thing;
        public bool spawned;
        public string loadID;
        public int acceptedTick = -1;
        public int createdTick = -1;
        public Faction issuer;


        public abstract bool IsWarrantFulfilled();
        public void DrawAcceptDeclineButtons(Rect rect)
        {
            var acceptRect = new Rect(rect.x + 5, rect.y + 50, 80, 30);
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

        public virtual void DoAcceptAction()
        {
            WarrantsManager.Instance.availableWarrants.Remove(this);
            WarrantsManager.Instance.acceptedWarrants.Add(this);
            acceptedTick = Find.TickManager.TicksGame;
        }
        public virtual void Draw(Rect rect)
        {
            Widgets.DrawLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), Color.gray, 1);
            DrawAcceptDeclineButtons(rect);
        }
        public virtual void ExposeData()
        {
            Scribe_Values.Look(ref acceptedTick, "acceptedTick", -1);
            Scribe_References.Look(ref issuer, "issuer");
            Scribe_Values.Look(ref loadID, "loadID");
            if (!spawned)
            {
                Scribe_Deep.Look(ref thing, "thing");
            }
            else
            {
                Scribe_References.Look(ref thing, "thing");
            }
        }

        public string GetUniqueLoadID()
        {
            return loadID;
        }

        public abstract void GiveReward(Caravan caravan);
    }

    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Warrant_Pawn : Warrant
    {
        public static readonly Texture2D IconCapture = ContentFinder<Texture2D>.Get("UI/Warrants/IconCapture");
        public static readonly Texture2D IconDeath = ContentFinder<Texture2D>.Get("UI/Warrants/IconDeath");
        public Pawn pawn => thing as Pawn;
        public string reason;

        public int rewardForLiving;

        public int rewardForDead;
        public override void Draw(Rect rect)
        {
            base.Draw(rect);
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

        public override bool IsWarrantFulfilled()
        {
            if (pawn.Destroyed)
            {
                return false;
            }
            if (rewardForDead == 0 && pawn.Dead)
            {
                return false;
            }
            return true;
        }

        public override void GiveReward(Caravan caravan)
        {
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            if (rewardForDead > 0 && pawn.Dead)
            {
                silver.stackCount = rewardForDead;
                CaravanInventoryUtility.GiveThing(caravan, silver);
            }
            else if (!pawn.Dead)
            {
                silver.stackCount = rewardForDead;
                CaravanInventoryUtility.GiveThing(caravan, silver);
            }
        }
    }
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Warrant_Artifact : Warrant
    {
        public static readonly Texture2D IconRetrieve = ContentFinder<Texture2D>.Get("UI/Warrants/IconRetrieve");
        public int reward;
        public override void Draw(Rect rect)
        {
            base.Draw(rect);
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
            var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            if (reward > 0)
            {
                silver.stackCount = reward;
                CaravanInventoryUtility.GiveThing(caravan, silver);
            }
        }
        public override bool IsWarrantFulfilled()
        {
            if (thing.Destroyed)
            {
                return false;
            }
            return true;
        }
    }
}
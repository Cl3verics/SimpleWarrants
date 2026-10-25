using RimWorld;
using RimWorld.QuestGen;
using System;
using UnityEngine;
using Verse;

namespace SimpleWarrants
{
    [HotSwappable]
    public class Warrant_TameAnimal : Warrant
    {
        public override bool UsesThings => false;

        public int Reward;
        public PawnKindDef AnimalRace;

        public override float AcceptChance()
        {
            throw new NotImplementedException();
        }

        public override float SuccessChance()
        {
            throw new NotImplementedException();
        }

        public override void DoAcceptAction()
        {
            if (Utils.TryResolveWarrantSiteFaction("SW_Site", issuer, out var siteFaction) is false)
            {
                Messages.Message("SW.WarrantSiteUnresolved".Translate(issuer.Name), MessageTypeDefOf.RejectInput);
                return;
            }

            base.DoAcceptAction();

            Slate slate = new Slate();
            slate.Set("enemyFaction", siteFaction);
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(Find.World));
            slate.Set("asker", issuer.leader);
            slate.Set("warrant", this);
            slate.Set("reward", Reward);
            slate.Set("animal", AnimalRace);
            this.thing = PawnGenerator.GeneratePawn(AnimalRace);
            slate.Set("victim", thing);

            var questDef = SW_DefOf.SW_Warrant_Tame;
            var quest = QuestUtility.GenerateQuestAndMakeAvailable(questDef, slate);
            QuestUtility.SendLetterQuestAvailable(quest);
        }

        public override void Draw(Rect rect, bool doAcceptAndDeclineButtons = true, bool doCompensateWarrantButton = false)
        {
            base.Draw(rect, doAcceptAndDeclineButtons, doCompensateWarrantButton);

            var pawnRect = new Rect(new Vector2(rect.x + 100, rect.y + 10), new Vector2(rect.height * 0.7f, rect.height));
            GUI.color = AnimalRace.race.uiIconColor;
            GUI.DrawTexture(pawnRect, AnimalRace.race.uiIcon, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
            Widgets.InfoCardButton(pawnRect.xMax - 24, pawnRect.yMax - 40, AnimalRace.race);

            Text.Font = GameFont.Medium;
            var postedByInfoBox = new Rect(pawnRect.xMax, rect.y + 6, rect.width - pawnRect.width, Text.LineHeight);
            DrawPostedBy(postedByInfoBox);

            var pawnName = $"<color=#bced93>{"SW.Tame".Translate()}</color> {AnimalRace.LabelCap}";
            var textSize = Text.CalcSize(pawnName);
            var nameInfoBox = new Rect(pawnRect.xMax, postedByInfoBox.yMax + 1, textSize.x, 30);
            Widgets.Label(nameInfoBox, pawnName);

            Text.Font = GameFont.Small;
            var rewardsForDeadIconBox = new Rect(nameInfoBox.x, nameInfoBox.yMax, 24, 24);
            GUI.DrawTexture(rewardsForDeadIconBox, Warrant_Pawn.IconCapture);

            var rewardsForDeadInfoBox = new Rect(rewardsForDeadIconBox.xMax + 5, nameInfoBox.yMax, (rect.width - pawnRect.width) / 3, nameInfoBox.height);
            if (Reward > 0)
            {
                Widgets.Label(rewardsForDeadInfoBox, Reward + " " + ThingDefOf.Silver.LabelCap);
            }
            else
            {
                Widgets.Label(rewardsForDeadInfoBox, "SW.NoReward".Translate());
            }

            var infoBox = new Rect(rect.width - 250, rect.yMax - 26, 250, 24);
            Text.Font = GameFont.Tiny;
            var expireDate = (relatedQuest != null ? acceptedTick : createdTick) + (GenDate.TicksPerDay * 15) - Find.TickManager.TicksGame;
            Widgets.Label(infoBox, "SW.WillExpireIn".Translate(expireDate.ToStringTicksToDays()));

            Text.Font = GameFont.Small;
        }

        public override string TargetLabel => AnimalRace.LabelCap;

        public override bool IsWarrantActive() => true;

        public override bool IsThreatForPlayer() => false;

        public override bool ShouldShowCompensateButton() => false;

        public override int MaxRewardValue() => Reward;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref AnimalRace, "animalRace");
            Scribe_Values.Look(ref Reward, "reward");
        }
    }
}

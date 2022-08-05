using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

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
    [StaticConstructorOnStartup]
    public abstract class Warrant : IExposable, ILoadReferenceable
    {
        public static readonly Texture2D InsufficientRewardIcon = ContentFinder<Texture2D>.Get("UI/Warrants/IconWarning");

        public int ApproximateAcceptionDate => Mathf.Max((int)(createdTick + ((GenDate.TicksPerDay * 7) / AcceptChance())), Find.TickManager.TicksGame) - Find.TickManager.TicksGame;
        public int ApproximateCompletionDate => Mathf.Max(tickToBeCompleted, Find.TickManager.TicksGame) - Find.TickManager.TicksGame;

        public float? acceptChanceCached;
        public int acceptedTick = -1;
        public Faction accepteer;
        public int createdTick = -1;
        public Faction issuer;
        public string loadID;
        public Quest relatedQuest;

        public WarrantStatus status;
        public float? successChanceCached;
        public Thing thing;
        public int tickToBeCompleted;
        private bool savedSomewhere;

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
            Scribe_Values.Look(ref createdTick, "createdTick", -1);
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
                    Log.Error(this + " has null thing, bugged now and won't work. Clearing it to avoid errors.");
                    End(affectGoodwill: false);
                    WarrantsManager.Instance.availableWarrants.Remove(this);
                    WarrantsManager.Instance.acceptedWarrants.Remove(this);
                    WarrantsManager.Instance.givenWarrants.Remove(this);
                    WarrantsManager.Instance.takenWarrants.Remove(this);
                }
            }
        }

        public string GetUniqueLoadID()
        {
            return loadID;
        }

        public abstract float AcceptChance();
        public abstract float SuccessChance();
        public abstract bool IsWarrantActive();
        public abstract bool IsThreatForPlayer();

        public virtual void OnCreate()
        {

        }

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

        public abstract int MaxReward();

        public virtual void DoAcceptAction()
        {
            WarrantsManager.Instance.availableWarrants.Remove(this);
            WarrantsManager.Instance.acceptedWarrants.Add(this);
            AcceptBy(Faction.OfPlayer);
        }

        public void AcceptBy(Faction faction)
        {
            acceptedTick = Find.TickManager.TicksGame;
            status = WarrantStatus.Accepted;
            accepteer = faction;
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
            if (issuer == Faction.OfPlayer && accepteer is null)
            {
                DrawRemoveWarrantButton(rect);
            }
        }

        public void End(QuestEndOutcome questEndOutcome = QuestEndOutcome.Fail, bool affectGoodwill = true)
        {
            relatedQuest?.End(questEndOutcome);
            if (affectGoodwill)
            {
                issuer.TryAffectGoodwillWith(Faction.OfPlayer, -30);
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

        public virtual void GiveReward(Caravan caravan)
        {
            status = WarrantStatus.Completed;
        }

        public static void GiveThing(Caravan caravan, Thing thing)
        {
            if (CaravanInventoryUtility.AllInventoryItems(caravan).Contains(thing))
            {
                Log.Error(string.Concat("Tried to give the same item twice (", thing, ") to a caravan (", caravan, ")."));
                return;
            }
            Pawn pawn = CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, caravan.PawnsListForReading
                .Where(x => x.IsColonist && !x.IsPrisoner && !x.Downed && !x.Dead && x != thing).ToList(), null);
            if (pawn == null)
            {
                Log.Error($"Failed to give item {thing} to caravan {caravan}; item was lost");
                thing.Destroy();
            }
            else if (!pawn.inventory.innerContainer.TryAdd(thing))
            {
                Log.Error($"Failed to give item {thing} to caravan {caravan}; item was lost");
                thing.Destroy();
            }
        }

        public bool CanPlayerReceive()
        {
            if (thing.Faction == Faction.OfPlayer)
            {
                return true;
            }
            if (issuer == Faction.OfPlayer)
            {
                return false;
            }
            if (SimpleWarrantsSettings.enableWarrantRewardScaling)
            {
                var wealth = Find.AnyPlayerHomeMap.wealthWatcher.WealthTotal;
                if ((wealth * 0.05f) >= MaxReward())
                {
                    return true;
                }
                return false;
            }
            return true;
        }
    }
}
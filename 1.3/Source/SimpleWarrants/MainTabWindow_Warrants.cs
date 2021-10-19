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
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class HotSwappableAttribute : Attribute
	{
	}

	[HotSwappableAttribute]
	[StaticConstructorOnStartup]
	public class MainTabWindow_Warrants : MainTabWindow
	{
		private enum WarrantsTab : byte
		{
			PublicWarrants,
			RelatedWarrants,
		}
		private WarrantsTab curTab;
		private List<TabRecord> tabs = new List<TabRecord>();
		public override void PreOpen()
		{
			base.PreOpen();
			tabs.Clear();
			tabs.Add(new TabRecord("SW.PublicWarrants".Translate(), delegate
			{
				curTab = WarrantsTab.PublicWarrants;
			}, () => curTab == WarrantsTab.PublicWarrants));
			tabs.Add(new TabRecord("SW.RelatedWarrants".Translate(), delegate
			{
				curTab = WarrantsTab.RelatedWarrants;

			}, () => curTab == WarrantsTab.RelatedWarrants));
		}

		public override void DoWindowContents(Rect rect)
		{
			Rect rect2 = rect;
			rect2.yMin += 45f;
			TabDrawer.DrawTabs(rect2, tabs);
			switch (curTab)
			{
				case WarrantsTab.PublicWarrants:
					DoPublicWarrants(rect2);
					break;
				case WarrantsTab.RelatedWarrants:
					DoRelatedWarrants(rect2);
					break;
			}
			DoWarrantCreation(rect2);
		}
		private Vector2 scrollPosition;
		private void DoPublicWarrants(Rect rect)
        {
			var warrants = WarrantsManager.Instance.availableWarrants.OrderByDescending(x => x.createdTick).ToList();
			var posY = rect.y + 10;
			var sectionWidth = 750;
			var outRect = new Rect(rect.x, posY, sectionWidth, 590);
			var viewRect = new Rect(outRect.x, posY, sectionWidth - 16, warrants.Count * 165);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			for (var i = 0; i < warrants.Count; i++)
            {
				var warrantBox = new Rect(rect.x, posY, sectionWidth - 30, 150);
				warrants[i].Draw(warrantBox);
				posY = warrantBox.yMax + 15;
			}
			Widgets.EndScrollView();
        }

		private void DoRelatedWarrants(Rect rect)
		{
			var warrants = WarrantsManager.Instance.acceptedWarrants.Concat(WarrantsManager.Instance.givenWarrants).OrderByDescending(x => x.createdTick).ToList();
			var posY = rect.y + 10;
			var sectionWidth = 750;
			var outRect = new Rect(rect.x, posY, sectionWidth, 590);
			var viewRect = new Rect(outRect.x, posY, sectionWidth - 16, warrants.Count * 165);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			for (var i = 0; i < warrants.Count; i++)
			{
				var warrantBox = new Rect(rect.x, posY, sectionWidth - 30, 150);
				warrants[i].Draw(warrantBox, false);
				posY = warrantBox.yMax + 15;
			}
			Widgets.EndScrollView();
		}

		public enum TargetType
        {
			Pawn,
			Artifact
        }

		TargetType curType;
		private void DoWarrantCreation(Rect rect)
		{
			var posY = rect.y + 10;
			var createWarrant = new Rect(790, posY, 160, 30);
			if (Widgets.ButtonText(createWarrant, "SW.CreateWarrant".Translate()))
            {
				WarrantsManager.Instance.givenWarrants.Add(CreateWarrant());
				curPawn = null;
				curArtifact = null;
            }

			Text.Font = GameFont.Medium;
			var warrantSubject = new Rect(createWarrant.x, createWarrant.yMax + 20, createWarrant.width, createWarrant.height);
			Widgets.Label(warrantSubject, "SW.WarrantSubject".Translate());

			var dropdownRect = new Rect(createWarrant.x, warrantSubject.yMax, createWarrant.width, createWarrant.height);
			if (Widgets.ButtonTextSubtle(dropdownRect, GetLabel(curType)))
            {
				var floatList = new List<FloatMenuOption>();
				foreach (var value in Enum.GetValues(typeof(TargetType)).Cast<TargetType>())
                {
					floatList.Add(new FloatMenuOption(GetLabel(value), delegate
					{
						this.curType = value;
					}));
                }
				Find.WindowStack.Add(new FloatMenu(floatList));
            }

			if (curType == TargetType.Pawn)
            {
				if (curPawn is null)
                {
					if (!Find.WorldPawns.AllPawnsAlive.Where(pawn => !WarrantsManager.Instance.givenWarrants.Any(warrant => pawn == warrant.thing)).TryRandomElement(out curPawn))
                    {
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
						curPawn = PawnGenerator.GeneratePawn(randomKind, faction);
					}
				}

				var pawnRect = new Rect(new Vector2(createWarrant.x + 40, dropdownRect.yMax + 10), new Vector2(100 * 0.722f, 100));
				Vector2 pos = new Vector2(pawnRect.width, pawnRect.height);
				GUI.DrawTexture(pawnRect, PortraitsCache.Get(curPawn, pos, Rot4.South, new Vector3(0f, 0f, 0f), 1.2f));
				Widgets.InfoCardButton(pawnRect.xMax, pawnRect.y + 10, curPawn);

				var nameRect = new Rect(createWarrant.x, pawnRect.yMax, createWarrant.width, createWarrant.height);
				Widgets.Label(nameRect, curPawn.Name.ToString());

				dropdownRect = new Rect(createWarrant.x, nameRect.yMax, createWarrant.width, createWarrant.height);
				if (Widgets.ButtonTextSubtle(dropdownRect, "SW.Select".Translate()))
				{
					var floatList = new List<FloatMenuOption>();
					foreach (var value in Find.WorldPawns.AllPawnsAlive.Where(pawn => !WarrantsManager.Instance.givenWarrants.Any(warrant => pawn == warrant.thing)))
					{
						floatList.Add(new FloatMenuOption(value.Name.ToString(), delegate
						{
							this.curPawn = value;
						}));
					}
					Find.WindowStack.Add(new FloatMenu(floatList));
				}

				var reasonRect = new Rect(dropdownRect.x, dropdownRect.yMax + 10, createWarrant.width, createWarrant.height);
				if (curReason.NullOrEmpty())
				{
					curReason = Utils.GenerateTextFromRule(SW_DefOf.SW_WantedFor, this.curPawn.thingIDNumber);
				}

				if (Widgets.ButtonTextSubtle(reasonRect, "SW.Reason".Translate(curReason)))
                {
					var floatList = new List<FloatMenuOption>();
					foreach (var value in Utils.GenerateAllTextFromRule(SW_DefOf.SW_WantedFor))
					{
						floatList.Add(new FloatMenuOption(value.ToString(), delegate
						{
							this.curReason = value;
						}));
					}
					Find.WindowStack.Add(new FloatMenu(floatList));
				}

				Text.Font = GameFont.Small;
				var capturePayment = new Rect(reasonRect.x - 30, reasonRect.yMax + 10, 130, 24);
				Widgets.Label(capturePayment, "SW.CapturePayment".Translate());
				var capturePaymentInput = new Rect(capturePayment.xMax, capturePayment.y, 60, 24);
				Widgets.TextFieldNumeric<int>(capturePaymentInput, ref curCapturePayment, ref buffCurCapturePayment);

				var deathPayment = new Rect(capturePayment.x, capturePayment.yMax + 5, capturePayment.width, capturePayment.height);
				Widgets.Label(deathPayment, "SW.DeathPayment".Translate());

				var deathPaymentInput = new Rect(deathPayment.xMax, deathPayment.y, 60, 24);
				Widgets.TextFieldNumeric<int>(deathPaymentInput, ref curDeathPayment, ref buffCurDeathPayment);
			}
			else
            {
				if (curArtifact is null) 
				{
					var artifactDef = Utils.AllArtifactDefs.RandomElement();
					curArtifact = ThingMaker.MakeThing(artifactDef);
				}

				var thingRect = new Rect(new Vector2(createWarrant.x + 40, dropdownRect.yMax + 10), new Vector2(100 * 0.722f, 100 * 0.722f));
				GUI.DrawTexture(thingRect, curArtifact.Graphic.MatSouth.mainTexture);
				Widgets.InfoCardButton(thingRect.xMax, thingRect.y + 10, curArtifact);

				var nameRect = new Rect(createWarrant.x, thingRect.yMax, createWarrant.width, createWarrant.height);
				Widgets.Label(nameRect, curArtifact.LabelCap.ToString());

				dropdownRect = new Rect(createWarrant.x, nameRect.yMax, createWarrant.width, createWarrant.height);
				if (Widgets.ButtonTextSubtle(dropdownRect, "SW.Select".Translate()))
				{
					var floatList = new List<FloatMenuOption>();
					foreach (var value in Utils.AllArtifactDefs)
					{
						floatList.Add(new FloatMenuOption(value.LabelCap.ToString(), delegate
						{
							this.curArtifact = ThingMaker.MakeThing(value);
						}));
					}
					Find.WindowStack.Add(new FloatMenu(floatList));
				}

				Text.Font = GameFont.Small;
				var rewardPayment = new Rect(createWarrant.x - 30, dropdownRect.yMax + 10, 130, 24);
				Widgets.Label(rewardPayment, "SW.RewardPayment".Translate());
				var rewardPaymentInput = new Rect(rewardPayment.xMax, rewardPayment.y, 60, 24);
				Widgets.TextFieldNumeric<int>(rewardPaymentInput, ref curReward, ref buffCurReward);
			}
			Text.Font = GameFont.Small;
		}

		private Warrant CreateWarrant()
        {
			if (curType == TargetType.Pawn)
            {
				var warrant = new Warrant_Pawn
				{
					loadID = WarrantsManager.Instance.GetWarrantID(),
					issuer = Faction.OfPlayer,
					createdTick = Find.TickManager.TicksGame
				};
				warrant.thing = curPawn;
				warrant.rewardForLiving = curCapturePayment;
				warrant.rewardForDead = curDeathPayment;
				warrant.reason = curReason;
				return warrant;
			}
			else
			{
				var warrant = new Warrant_Artifact
				{
					loadID = WarrantsManager.Instance.GetWarrantID(),
					issuer = Faction.OfPlayer,
					createdTick = Find.TickManager.TicksGame
				};
				warrant.thing = curArtifact;
				warrant.reward = curReward;
				return warrant;
			}
        }

		public Pawn curPawn;
		public string curReason;
		public Thing curArtifact;

		public int curCapturePayment;
		public int curDeathPayment;
		public int curReward;

		public string buffCurCapturePayment;
		public string buffCurDeathPayment;
		public string buffCurReward;
		public string GetLabel(TargetType targetType)
        {
			switch (targetType)
            {
				case TargetType.Pawn: return "SW.Pawn".Translate();
				case TargetType.Artifact: return "SW.Artifact".Translate();
            }
			return null;
        }
	}
}

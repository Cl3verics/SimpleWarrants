using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SimpleWarrants
{
    [HotSwappableAttribute]
	public class Dialog_SelectPawn : Window
	{
		private MainTabWindow_Warrants parent;
		private Vector2 scrollPosition;
		public override Vector2 InitialSize => new Vector2(620f, 500f);

		public List<Pawn> allPawns;
		public Dialog_SelectPawn(MainTabWindow_Warrants parent)
		{
			doCloseButton = true;
			doCloseX = true;
			closeOnClickedOutside = false;
			absorbInputAroundWindow = false;
			allPawns = Find.WorldPawns.AllPawnsAlive.Where(pawn => pawn?.story != null && pawn?.Name != null 
			&& !WarrantsManager.Instance.givenWarrants.Any(warrant => pawn == warrant.thing)).ToList();
			this.parent = parent;
		}

		string searchKey;
		public override void DoWindowContents(Rect inRect)
		{
			Text.Font = GameFont.Small;

			Text.Anchor = TextAnchor.MiddleLeft;
			var searchLabel = new Rect(inRect.x, inRect.y, 60, 24);
			Widgets.Label(searchLabel, "SW.Search".Translate());
			var searchRect = new Rect(searchLabel.xMax + 5, searchLabel.y, 200, 24f);
			searchKey = Widgets.TextField(searchRect, searchKey);
			Text.Anchor = TextAnchor.UpperLeft;

			Rect outRect = new Rect(inRect);
			outRect.y = searchRect.yMax + 5;
			outRect.yMax -= 70f;
			outRect.width -= 16f;

			var pawns = searchKey.NullOrEmpty() ? allPawns : allPawns.Where(x => x.Name.ToStringFull.ToLower().Contains(searchKey.ToLower())).ToList();

			Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, (float)pawns.Count() * 35f);
			Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
			try
			{
				float num = 0f;
				foreach (Pawn pawn in pawns.OrderBy(x => x.Name.ToStringFull))
				{
					Widgets.InfoCardButton(0, num, pawn);
					Rect iconRect = new Rect(24, num, 24, 24);
					Widgets.ThingIcon(iconRect, pawn);
					iconRect.x += 24;
					if (pawn.Faction != null)
                    {
						FactionUIUtility.DrawFactionIconWithTooltip(iconRect, pawn.Faction);
					}
					Rect rect = new Rect(iconRect.xMax + 5, num, viewRect.width * 0.65f, 32f);
					Text.Anchor = TextAnchor.MiddleLeft;
					Widgets.Label(rect, pawn.Name.ToStringFull);
					Text.Anchor = TextAnchor.UpperLeft;
					rect.x = rect.xMax + 10;
					rect.width = 100;
					if (Widgets.ButtonText(rect, "SW.Select".Translate()))
					{
						if (pawn.Faction != null && !pawn.Faction.HostileTo(Faction.OfPlayer))
                        {
							Find.WindowStack.Add(new Dialog_MessageBox("SW.WarrantOnNonHostileWarning".Translate(pawn.Faction.Named("FACTION")), "Accept".Translate(), delegate
							{
								this.parent.AssignPawn(pawn);
							}, "Cancel".Translate(), null));
                        }
						else
                        {
							this.parent.AssignPawn(pawn);
						}
						SoundDefOf.Click.PlayOneShotOnCamera();
						this.Close();
					}
					num += 35f;
				}
			}
			finally
			{
				Widgets.EndScrollView();
			}
		}
	}
}

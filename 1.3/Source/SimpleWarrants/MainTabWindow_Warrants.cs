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
		}
		private Vector2 scrollPosition;
		private void DoPublicWarrants(Rect rect)
        {
			var warrants = WarrantsManager.Instance.availableWarrants;
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

		}
	}
}

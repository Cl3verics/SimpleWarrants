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
using Verse.Grammar;
using Verse.Sound;

namespace SimpleWarrants
{
	public class QuestNode_Warrant : QuestNode
	{
		public SlateRef<string> inSignal;
		protected override bool TestRunInt(Slate slate)
		{
			return true;
		}

		protected override void RunInt()
		{
			Slate slate = QuestGen.slate;
			QuestPart_Warrant questPart = new QuestPart_Warrant();
			questPart.inSignal = QuestGenUtility.HardcodedSignalWithQuestID(inSignal.GetValue(slate)) ?? slate.Get<string>("inSignal");
			questPart.warrant = slate.Get<Warrant>("warrant");
			QuestGen.quest.AddPart(questPart);
		}
	}
	public class QuestPart_Warrant : QuestPart
	{
		public Warrant warrant;
		public string inSignal;
		public override void Notify_QuestSignalReceived(Signal signal)
		{
			base.Notify_QuestSignalReceived(signal);
			if (!(signal.tag == inSignal))
			{
				return;
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref inSignal, "inSignal");
			Scribe_References.Look(ref warrant, "warrant");
		}
	}
}
using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using Verse;

namespace SimpleWarrants
{
    public class RewardList
    {
        public List<RewardNode> rewards;
    }
    public class QuestNode_Rewards : QuestNode
    {
        [NoTranslate]

        public List<RewardList> nodes = new List<RewardList>();
        protected override bool TestRunInt(Slate slate)
        {
            if (!slate.Exists("map"))
            {
                return false;
            }
            return true;
        }

        protected override void RunInt()
        {
            Slate slate = QuestGen.slate;
            QuestPart_Choice questPart_Choice = new QuestPart_Choice();
            foreach (var node in nodes)
            {
                QuestPart_Choice.Choice choice = new QuestPart_Choice.Choice();
                foreach (var rewardNode in node.rewards)
                {
                    var rewards = rewardNode.GenerateRewards(slate);
                    foreach (var reward in rewards)
                    {
                        choice.rewards.Add(reward);
                    }
                    foreach (var item in rewardNode.GenerateQuestParts(slate))
                    {
                        QuestGen.quest.AddPart(item);
                        choice.questParts.Add(item);
                    }
                }
                questPart_Choice.choices.Add(choice);
            }
            QuestGen.quest.AddPart(questPart_Choice);
        }
    }
}
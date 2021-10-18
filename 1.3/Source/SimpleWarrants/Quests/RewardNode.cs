using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;

namespace SimpleWarrants
{
    public abstract class RewardNode
    {
        public string inSignal;

        public string outSignalChoiceAccepted;
        public abstract IEnumerable<Reward> GenerateRewards(Slate slate);
        public abstract IEnumerable<QuestPart> GenerateQuestParts(Slate slate);
    }
}
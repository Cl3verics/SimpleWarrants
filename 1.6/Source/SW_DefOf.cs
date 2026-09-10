using RimWorld;
using Verse;

namespace SimpleWarrants
{
    [DefOf]
    public static class SW_DefOf
    {
        public static IncidentDef SW_Visitors;
        public static RulePackDef SW_WantedFor;
        public static RulePackDef SW_Messages;
        public static QuestScriptDef SW_Warrant_Animal;
        public static QuestScriptDef SW_Warrant_Tame;
        public static QuestScriptDef SW_Warrant_Artifact;
        public static QuestScriptDef SW_Warrant_Pawn;
        public static IncidentCategoryDef FactionArrival;
        public static ThingCategoryDef Artifacts;
        public static WarrantReasonDef SW_Poaching;
        public static WarrantReasonDef SW_Torture;
        public static WarrantReasonDef SW_Assault;
        public static WarrantReasonDef SW_Fraud;
    }
}

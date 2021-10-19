using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;
using Verse.Sound;

namespace SimpleWarrants
{
    public class IncidentWorker_Visitors : IncidentWorker_VisitorGroup
    {
		public static Thing toDeliver;
		protected override bool TryExecuteWorker(IncidentParms parms)
		{
			Map map = (Map)parms.target;
			if (!TryResolveParms(parms))
			{
				return false;
			}
			List<Pawn> list = SpawnPawns(parms);
			if (list.Count == 0)
			{
				return false;
			}
			LordMaker.MakeNewLord(parms.faction, CreateLordJob(parms, list), map, list);
			Pawn leader = list.Find((Pawn x) => parms.faction.leader == x);
			var deliveree = leader ?? list.RandomElement();
			GenSpawn.Spawn(toDeliver, deliveree.Position, map);
			IntVec3 cell = IntVec3.Invalid;
			cell = map.areaManager.Home.ActiveCells.Where(x => x.Walkable(map) && deliveree.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Deadly))
				.RandomElementByWeight(x => x.DistanceTo(deliveree.Position));
			if (!cell.IsValid && !RCellFinder.TryFindRandomCellOutsideColonyNearTheCenterOfTheMap(deliveree.Position, map, 50, out cell))
            {
				cell = map.AllCells.Where(x => x.Walkable(map) && deliveree.CanReserveAndReach(x, PathEndMode.OnCell, Danger.Deadly)).RandomElement();
			}
			var job = JobMaker.MakeJob(JobDefOf.HaulToCell, toDeliver, cell);
			job.count = 1;
			job.locomotionUrgency = LocomotionUrgency.Walk;
			deliveree.jobs.TryTakeOrderedJob(job);
			SendLetter(parms, list, leader, false);
			return true;
		}
	}
}
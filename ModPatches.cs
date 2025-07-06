using System.Collections.Generic;
using System.Reflection.Emit;
using BBSchoolMaze.Plugin;
using HarmonyLib;
using MTM101BaldAPI.PlusExtensions;
using UnityEngine;

namespace BBSchoolMaze.Patches
{

	[HarmonyPatch(typeof(LevelGenerator))]
	public class MazeChaos
	{
		internal static BasePlugin.ChaosMode ChaosMode
		{
			get
			{
				var mark = Singleton<BaseGameManager>.Instance.GetComponent<ChaosGameManager>();
				return !mark ? BasePlugin.ChaosMode.None : (BasePlugin.ChaosMode)mark.modeUsed;
			}
		}

		[HarmonyPatch("StartGenerate")]
		[HarmonyPrefix]
		private static void GetGenerator(LevelGenerator __instance)
		{
			if (ChaosMode == BasePlugin.ChaosMode.None)
				return;

			tripEntrances.Clear();
			afterAllRoomsCalled = false;
			i = __instance;
		}

		[HarmonyPatch(typeof(RoomFunctionContainer), "AfterAllRoomsPlaced")] // Called right after rooms are generated; perfect spot to generate chaos!!!
		[HarmonyPrefix]
		static void StartTheMess(LevelBuilder builder, System.Random rng)
		{
			if (!ModeClearsOutMap || afterAllRoomsCalled)
				return;
			afterAllRoomsCalled = true;


			List<Cell> reconnectionCells = [];

			foreach (var tile in builder.Ec.mainHall.GetNewTileList())
			{
				if (!tile.offLimits)
				{
					if (IsInBorder(tile.position))
						builder.Ec.DestroyCell(tile);
					else if (tile.TileMatches(i.Ec.mainHall)) // feels useless, but it makes ElevatorsInSpecialRoom work properly
						reconnectionCells.Add(tile);
				}

			}


			builder.Ec.mainHall.size = builder.levelSize;
			builder.Ec.mainHall.maxSize = builder.levelSize;

			for (int x = 0; x < builder.Ec.levelSize.x; x++) // Basically fill every single spot with a maze tile so the level doesn't break in a case a rare occurrance happen
			{
				for (int z = 0; z < builder.Ec.levelSize.z; z++)
				{
					if (!builder.Ec.ContainsCoordinates(x, z) || !builder.Ec.CellFromPosition(x, z).Null || !IsInBorder(x, z)) continue;
					builder.Ec.mainHall.position = new(x, z);

					MazeGenerator.Generate(builder.Ec.mainHall, rng);
				}

			}

			foreach (var cell in reconnectionCells)
			{
				for (int i = 0; i < 4; i++)
				{
					var dir = (Direction)i;
					var nextPos = cell.position + dir.ToIntVector2();
					if (builder.Ec.ContainsCoordinates(nextPos))
					{
						var nextCell = builder.Ec.CellFromPosition(nextPos);
						if (nextCell.TileMatches(cell.room))
							builder.Ec.ConnectCells(cell.position, dir);
					}
				}
			}

			builder.Ec.mainHall.position = new();

			foreach (var door in tripEntrances)
			{
				var tile = builder.Ec.CellFromPosition(door.Key);
				if (tile != null)
				{
					foreach (var dir in door.Value)
					{
						builder.Ec.ConnectCells(door.Key, dir);
						tile = builder.Ec.CellFromPosition(door.Key);
						tile.doorHere = true;
						tile.doorDirs.Add(dir);
						tile.doorDirsSpace.Add(dir); // Fix the issue with blocked off entrances

						tile = builder.Ec.CellFromPosition(door.Key + dir.ToIntVector2());
						tile.doorHere = true;
						tile.doorDirs.Add(dir.GetOpposite());
						tile.doorDirsSpace.Add(dir.GetOpposite()); // Fix the issue with blocked off entrances
					}
				}
			}

			tripEntrances.Clear(); // Done
		}

		static bool ModeClearsOutMap =>
			ChaosMode == BasePlugin.ChaosMode.MazeChaos || ChaosMode == BasePlugin.ChaosMode.HallChaos;
		static LevelGenerator i;
		static bool afterAllRoomsCalled = false;

		internal static Dictionary<IntVector2, Direction[]> tripEntrances = [];

#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
		internal static bool IsInBorder(IntVector2 pos) =>
			IsInBorder(pos.x, pos.z);
		internal static bool IsInBorder(int x, int z) =>
			i.ld.outerEdgeBuffer <= x &&
			i.ld.outerEdgeBuffer <= z &&
			(i.Ec.levelSize.x - i.ld.outerEdgeBuffer) >= x &&
			(i.Ec.levelSize.z - i.ld.outerEdgeBuffer) >= z;
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
	}




	[HarmonyPatch(typeof(BaseGameManager), "Initialize")]
	internal class NocheatJustfullmap
	{
		private static void Prefix(BaseGameManager __instance)
		{
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.MazeChaos || MazeChaos.ChaosMode == BasePlugin.ChaosMode.RoomChaos)
				__instance.CompleteMapOnReady();
		}

	}

	[HarmonyPatch(typeof(LevelBuilder))]
	internal class MakeEntrancesVisible
	{
		[HarmonyPatch("CreateElevator")]
		[HarmonyPostfix]
		private static void RegisterThisElevator(IntVector2 pos, ref Direction dir)
		{
			if (MazeChaos.ChaosMode != BasePlugin.ChaosMode.None)
				MazeChaos.tripEntrances.Add(pos, [dir.GetOpposite(), dir.PerpendicularList()[0], dir.PerpendicularList()[1], dir]); // Register this entrance to fix a later bug
		}
	}

	[HarmonyPatch(typeof(Elevator), "Close")]
	internal class DisableGateAnimation
	{
		private static bool Prefix(ref bool ___open, ref MapIcon ___mapIcon, Sprite ___lockedIconSprite, ref MeshCollider ___gateCollider)
		{
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.None || MazeChaos.ChaosMode == BasePlugin.ChaosMode.RoomChaos)
				return true;

			___open = false;
			___mapIcon.spriteRenderer.sprite = ___lockedIconSprite;
			___mapIcon.spriteRenderer.color = Color.red;
			___gateCollider.enabled = false;

			return false;
		}
	}

	[HarmonyPatch(typeof(PlayerMovement), "Start")]
	internal class GottaGoFast
	{
		[HarmonyPostfix]
		private static void GoFastFunc(PlayerMovement __instance)
		{

			switch (MazeChaos.ChaosMode)
			{
				case BasePlugin.ChaosMode.MazeChaos:
					__instance.pm.GetMovementStatModifier().AddModifier("runSpeed", new(3.2f));
					__instance.pm.GetMovementStatModifier().AddModifier("walkSpeed", new(3.2f));
					__instance.pm.GetMovementStatModifier().AddModifier("staminaDrop", new(0.6f));
					break;
				case BasePlugin.ChaosMode.HallChaos:
					__instance.pm.GetMovementStatModifier().AddModifier("runSpeed", new(1.6f));
					__instance.pm.GetMovementStatModifier().AddModifier("walkSpeed", new(1.6f));
					__instance.pm.GetMovementStatModifier().AddModifier("staminaDrop", new(0.9f));
					break;
				case BasePlugin.ChaosMode.RoomChaos:
					__instance.pm.GetMovementStatModifier().AddModifier("runSpeed", new(4f));
					__instance.pm.GetMovementStatModifier().AddModifier("walkSpeed", new(2f));
					__instance.pm.GetMovementStatModifier().AddModifier("staminaDrop", new(0.85f));
					break;
			}
		}
	}

	[HarmonyPatch(typeof(EnvironmentController))]
	internal class NPCFast_AndBaldiIcon
	{
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void GottaGoFastNPCs(EnvironmentController __instance)
		{
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.MazeChaos)
				__instance.AddTimeScale(mod);
		}

		[HarmonyPatch("SpawnNPC")]
		[HarmonyPostfix]
		private static void AddBaldiIcon(EnvironmentController __instance, List<NPC> ___npcs)
		{
			if (MazeChaos.ChaosMode != BasePlugin.ChaosMode.MazeChaos)
				return;

			NPC npc = ___npcs[___npcs.Count - 1];
			if (npc.Character == Character.Baldi)
				__instance.map.AddArrow(npc.Navigator.Entity, Color.green); // Baldo has an icon now >:D

		}

		[HarmonyPatch("SetupDoor")]
		[HarmonyPrefix]
		private static void FixDoorWallCover(EnvironmentController __instance, ref Cell tile, Direction dir)
		{
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.None)
				return;

			var pos = tile.position;
			__instance.ConnectCells(pos, dir);
			tile = __instance.CellFromPosition(pos);
		}

		//[HarmonyPatch("SetTileInstantiation")]
		//[HarmonyPostfix]
		//static void AlwaysVisible(EnvironmentController __instance) =>
		//	__instance.instantiateTiles = true;


		readonly static TimeScaleModifier mod = new() { environmentTimeScale = 1f, npcTimeScale = 2.5f };
	}

	[HarmonyPatch(typeof(VentController), "Initialize")]
	static class FastVents
	{
		static void Postfix(ref float ___speed)
		{
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.RoomChaos)
				___speed = 200f;
		}
	}


}

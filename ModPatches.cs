﻿using BBSchoolMaze.Plugin;
using HarmonyLib;
using MTM101BaldAPI.PlusExtensions;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

namespace BBSchoolMaze.Patches
{

	[HarmonyPatch(typeof(LevelGenerator))]
	public class MazeChaos
	{
		internal static BasePlugin.ChaosMode ChaosMode { 
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
			tripEntrances.Clear();
			i = __instance;
		}


		[HarmonyPatch("Generate", MethodType.Enumerator)]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> EstablishChaos(IEnumerable<CodeInstruction> instructions)
		{
			var match = new CodeMatcher(instructions)

			.MatchForward(false,
				new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(RoomController), "forcedDoorPositions")),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld, name: "<i>5__50"),
				new CodeMatch(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(List<IntVector2>), "Item")),
				new CodeMatch(OpCodes.Ldarg_0),
				new CodeMatch(OpCodes.Ldfld, name: "<room>5__74"),
				new CodeMatch(OpCodes.Ldc_I4_0),
				new CodeMatch(OpCodes.Ldc_I4_1),
				new CodeMatch(OpCodes.Ldloca_S, name: "V_87"),
				new CodeMatch(OpCodes.Ldloca_S, name: "V_88"),
				new CodeMatch(CodeInstruction.Call("LevelBuilder:BuildDoorIfPossible", [typeof(IntVector2), typeof(RoomController), typeof(bool), typeof(bool), typeof(RoomController).MakeByRefType(), typeof(IntVector2).MakeByRefType()]))
				); // Get to this method

			match.Advance(-20); // Goes to right spot
			match.InsertAndAdvance(
			Transpilers.EmitDelegate(() =>
			{
				if (ChaosMode == BasePlugin.ChaosMode.None)
					return;
				

				List<Cell> reconnectionCells = [];

				foreach (var tile in i.Ec.mainHall.GetNewTileList())
				{
					if (!tile.offLimits)
					{
						if (IsInBorder(tile.position))
							i.Ec.DestroyCell(tile);
						else if (tile.TileMatches(i.Ec.mainHall)) // feels useless, but it makes ElevatorsInSpecialRoom work properly
							reconnectionCells.Add(tile);
					}

				}


				var rng = new System.Random(i.controlledRNG.Next());
				i.Ec.mainHall.size = i.levelSize;
				i.Ec.mainHall.maxSize = i.levelSize;

				for (int x = 0; x < i.Ec.levelSize.x; x++) // Basically fill every single spot with a maze tile so the level doesn't break in a case a rare occurrance happen
				{
					for (int z = 0; z < i.Ec.levelSize.z; z++)
					{
						if (!i.Ec.ContainsCoordinates(x, z) || !i.Ec.CellFromPosition(x, z).Null || !IsInBorder(x, z)) continue;
						i.Ec.mainHall.position = new(x, z);

						MazeGenerator.Generate(i.Ec.mainHall, rng);
					}

				}

				foreach (var cell in reconnectionCells)
				{
					for (int i = 0; i < 4; i++)
					{
						var dir = (Direction)i;
						var nextPos = cell.position + dir.ToIntVector2();
						if (MazeChaos.i.Ec.ContainsCoordinates(nextPos))
						{
							var nextCell = MazeChaos.i.Ec.CellFromPosition(nextPos);
							if (nextCell.TileMatches(cell.room))
								MazeChaos.i.Ec.ConnectCells(cell.position, dir);
						}
					}
				}

				i.Ec.mainHall.position = new();

				foreach (var door in tripEntrances)
				{
					var tile = i.Ec.CellFromPosition(door.Key);
					if (tile != null)
					{
						foreach (var dir in door.Value)
						{
							i.Ec.ConnectCells(door.Key, dir);
							tile = i.Ec.CellFromPosition(door.Key);
							tile.doorHere = true;
							tile.doorDirs.Add(dir);
							tile.doorDirsSpace.Add(dir); // Fix the issue with blocked off entrances

							tile = i.Ec.CellFromPosition(door.Key + dir.ToIntVector2());
							tile.doorHere = true;
							tile.doorDirs.Add(dir.GetOpposite());
							tile.doorDirsSpace.Add(dir.GetOpposite()); // Fix the issue with blocked off entrances
						}
					}
				}

				tripEntrances.Clear(); // Done
			}));

			return match.InstructionEnumeration();
		}

		static LevelGenerator i;

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
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.MazeChaos)
				__instance.CompleteMapOnReady();
		}

	}

	[HarmonyPatch(typeof(LevelBuilder))]
	internal class MakeElevatorVisible
	{
		[HarmonyPatch("CreateElevator")]
		[HarmonyPostfix]
		private static void RegisterThisElevator(IntVector2 pos, ref Direction dir) =>
			MazeChaos.tripEntrances.Add(pos, [dir.GetOpposite(), dir.PerpendicularList()[0], dir.PerpendicularList()[1], dir]); // Register this entrance to fix a later bug
	}

	[HarmonyPatch(typeof(Elevator), "Close")]
	internal class DisableGateAnimation
	{
		private static bool Prefix(ref bool ___open, ref MapIcon ___mapIcon, Sprite ___lockedIconSprite, ref MeshCollider ___gateCollider)
		{
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.None)
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
			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.None)
				return;

			if (MazeChaos.ChaosMode == BasePlugin.ChaosMode.MazeChaos)
			{
				__instance.pm.GetMovementStatModifier().AddModifier("runSpeed", new(3.2f));
				__instance.pm.GetMovementStatModifier().AddModifier("walkSpeed", new(3.2f));
				__instance.pm.GetMovementStatModifier().AddModifier("staminaDrop", new(0.6f));
				return;
			}
			__instance.pm.GetMovementStatModifier().AddModifier("runSpeed", new(1.6f));
			__instance.pm.GetMovementStatModifier().AddModifier("walkSpeed", new(1.6f));
			__instance.pm.GetMovementStatModifier().AddModifier("staminaDrop", new(0.9f));
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


}

using HarmonyLib;
using MTM101BaldAPI.PlusExtensions;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace BBSchoolMaze.Patches
{

	[HarmonyPatch(typeof(LevelGenerator))]
	public class MazeChaos
	{

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
				foreach (var tile in i.Ec.mainHall.GetNewTileList())
				{
					if (!tile.offLimits)
						i.Ec.DestroyCell(tile);
					
				}
				

				var rng = new System.Random(i.controlledRNG.Next());
				i.Ec.mainHall.size = i.levelSize;
				i.Ec.mainHall.maxSize = i.levelSize;

				for (int x = 0; x < i.Ec.levelSize.x; x++) // Basically fill every single spot with a maze tile so the level doesn't break in a case a rare occurrance happen
				{
					for (int z = 0; z < i.Ec.levelSize.z; z++)
					{
						if (!i.Ec.CellFromPosition(x, z).Null) continue;
						i.Ec.mainHall.position = new(x, z);

						MazeGenerator.Generate(i.Ec.mainHall, rng);
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
	}


	[HarmonyPatch(typeof(BaseGameManager), "Initialize")]
	internal class NocheatJustfullmap
	{
		private static void Prefix(BaseGameManager __instance) =>
			__instance.CompleteMapOnReady();
		
	}

	[HarmonyPatch(typeof(StoreScreen), "Start")]
	internal class NoFullmapForYou
	{
		private static void Postfix(ref TMP_Text ___mapPriceText, ref bool[] ___itemPurchased, ref GameObject ___mapHotSpot)
		{
			___itemPurchased[6] = true; // It is the fullmap index
			___mapPriceText.text = "Out";
			___mapPriceText.color = Color.red; // Map price text, duh
			___mapHotSpot.SetActive(false); // That hover spot for johnny to explain the item
		}
	}

	[HarmonyPatch(typeof(LevelBuilder))]
	internal class MakeElevatorVisible
	{
		/*  <-- Obsolete (as this method doesn't even exist anymore), the elevator has an indication now.
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		private static void ChangeConstBins(ref MapTile[] ___mapTiles) =>
			___mapTiles[16] = ___mapTiles[15]; // Literally this

		[HarmonyPatch("AddMapTile")]
		[HarmonyPostfix]
		private static void FixElevatorColor(ref IntVector2 position, EnvironmentController ___ec, ref Map ___map) // Basically make elevators cyan
		{
			Cell tileController = ___ec.CellFromPosition(position);
			if (!tileController.Null && tileController.ConstBin == 16)
				___map.tiles[position.x, position.z].SpriteRenderer.color = Color.cyan;
			
		}
		*/

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
			__instance.pm.GetMovementStatModifier().AddModifier("runSpeed", new(3.2f));
			__instance.pm.GetMovementStatModifier().AddModifier("walkSpeed", new(3.2f));
			__instance.pm.GetMovementStatModifier().AddModifier("staminaDrop", new(2/3));
		}
	}

	[HarmonyPatch(typeof(EnvironmentController))]
	internal class NPCFast_AndBaldiIcon
	{
		[HarmonyPatch("Start")]
		[HarmonyPostfix]
		private static void GottaGoFastNPCs(EnvironmentController __instance) =>
			__instance.AddTimeScale(mod);

		[HarmonyPatch("SpawnNPC")]
		[HarmonyPostfix]
		private static void AddBaldiIcon(EnvironmentController __instance, List<NPC> ___npcs)
		{
			NPC npc = ___npcs[___npcs.Count - 1];
			if (npc.Character == Character.Baldi)
				__instance.map.AddArrow(npc.transform, Color.green); // Baldo has an icon now >:D
			
		}

		[HarmonyPatch("SetupDoor")]
		[HarmonyPrefix]
		private static void FixDoorWallCover(EnvironmentController __instance, ref Cell tile, Direction dir)
		{
			var pos = tile.position;
			__instance.ConnectCells(pos, dir);
			tile = __instance.CellFromPosition(pos);
		}


		readonly static TimeScaleModifier mod = new() { environmentTimeScale = 1f, npcTimeScale = 2.5f };
	}


}

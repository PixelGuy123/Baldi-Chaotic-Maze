using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using TMPro;
using UnityEngine;

namespace BBSchoolMaze.Patches
{

	[HarmonyPatch(typeof(LevelGenerator))]
	internal class MazeChaos
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
				new CodeMatch(OpCodes.Ldnull),
				new CodeMatch(OpCodes.Ldc_I4_1),
				new CodeMatch(OpCodes.Ldc_I4_0),
				new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(LevelBuilder), "AddRandomDoor")) // Lesson learned: one code match doesn't work, you have to be super specific
				); // Get to this method

			match.Advance(-7);
			match.InsertAndAdvance(
			Transpilers.EmitDelegate(() =>
			{
				foreach (var tile in i.Ec.mainHall.GetNewTileList())
				{
					if (!tile.keepBin)
					{
						i.Ec.DestroyTile(tile.position, tile);
						i.Ec.mainHall.RemoveTile(tile);
					}
				}

				var maze = new GameObject("MazeGen").AddComponent<MazeGenerator>();
				i.Ec.mainHall.size = i.levelSize;
				i.Ec.mainHall.maxSize = i.levelSize;

				AccessTools.Field(typeof(MazeGenerator), "patchesToSpawn").SetValue(maze, 0); // No "library" centers

				// Setups the maze manually to call the Generate() method
				AccessTools.Field(typeof(MazeGenerator), "cRng").SetValue(maze, new System.Random(i.controlledRNG.Next()));

				for (int x = 0; x < i.Ec.levelSize.x; x++) // Basically fill every single spot with a maze tile so the level doesn't break in a case a rare occurrance happen
				{
					for (int z = 0; z < i.Ec.levelSize.z; z++)
					{
						if (i.Ec.tiles[x, z] != null) continue;
						i.Ec.mainHall.position = new(x, z);

						

						var method = (IEnumerator)AccessTools.Method("MazeGenerator:Generate", [typeof(RoomController)]).Invoke(maze, [i.Ec.mainHall]); // Invokes the method, now just manually wait
						while (method.MoveNext()) { } // Manually move next the method
						
					}
				}

				foreach (var door in tripEntrances)
				{
					var tile = i.Ec.TileFromPos(door.Key);
					if (tile != null)
					{
						tile.doorHere = true;
						tile.doorDirs.Add(door.Value);
						tile.doorDirsSpace.Add(door.Value); // Fix the issue with blocked off entrances
					}
				}

				tripEntrances.Clear(); // Done
			}));

			return match.InstructionEnumeration();
		}

		static LevelGenerator i;

		internal static Dictionary<IntVector2, Direction> tripEntrances = [];
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
		[HarmonyPatch("Start")]
		[HarmonyPrefix]
		private static void ChangeConstBins(ref MapTile[] ___mapTiles) =>
			___mapTiles[16] = ___mapTiles[15]; // Literally this

		[HarmonyPatch("AddMapTile")]
		[HarmonyPostfix]
		private static void FixElevatorColor(ref IntVector2 position, EnvironmentController ___ec, ref Map ___map) // Basically make elevators cyan
		{
			TileController tileController = ___ec.tiles[position.x, position.z];
			if (tileController != null && tileController.ConstBin == 16)
				___map.tiles[position.x, position.z].SpriteRenderer.color = Color.cyan;
			
		}

		[HarmonyPatch("CreateTripEntrance")]
		[HarmonyPostfix]
		private static void RegisterThisEntrance(IntVector2 pos, Direction dir) =>
			MazeChaos.tripEntrances.Add(pos, dir); // Register this entrance to fix a later bug
		
		
	}

	[HarmonyPatch(typeof(PlayerMovement), "Start")]
	internal class GottaGoFast
	{
		[HarmonyPostfix]
		private static void GoFastFunc(ref float ___runSpeed, ref float ___walkSpeed, ref float ___staminaDrop)
		{
			___runSpeed *= 3.2f;
			___walkSpeed *= 3.2f;
			___staminaDrop /= 1.5f;
		}
	}

	[HarmonyPatch(typeof(EnvironmentController), "Start")]
	internal class NPCFast
	{
		[HarmonyPostfix]
		private static void GottaGoFastNPCs(EnvironmentController __instance) =>
			__instance.AddTimeScale(mod);


		readonly static TimeScaleModifier mod = new() { environmentTimeScale = 1f, npcTimeScale = 2.5f };
	}


}

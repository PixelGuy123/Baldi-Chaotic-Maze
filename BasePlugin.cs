using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;
using ModdedModesAPI.ModesAPI;
using System.Collections.Generic;

namespace BBSchoolMaze.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
	[BepInDependency("pixelguy.pixelmodding.baldiplus.moddedmodesapi")]

	public class BasePlugin : BaseUnityPlugin
	{
		internal enum ChaosMode
		{
			None = -1,
			MazeChaos = 0,
			HallChaos = 1,
			RoomChaos = 2,
		}

#pragma warning disable IDE0051 // Remover membros privados não utilizados
		void Awake()
#pragma warning restore IDE0051 // Remover membros privados não utilizados
		{
			AssetLoader.LocalizationFromFunction((_) => new()
			{
				{ "Men_MazeChaos_Label", "Pick a chaotic challenge!" },
				{ "Men_MazeChaos_Main_Name", "Chaotic Levels" },
				{ "Men_MazeChaos_Main_Desc", "This category stores some chaotic challenges like <color=red>mazes</color> and <color=red>full open areas</color>, things that you may never have seen before!" },
				{ "Men_MazeChaos_Name", "Maze Chaos" },
				{ "Men_MazeChaos_Desc", "<color=green>Baldi</color> had a funny idea to truly test the utility of the Advanced Map! Behold... <color=red>a maze schoolhouse!</color> However, for a fair challenge, you'll <color=blue>find a lot more Portal Posters</color> and <color=blue>run faster!</color>" },
				{ "Men_HallChaos_Name", "Hallway Chaos" },
				{ "Men_HallChaos_Desc", "The schoolhouse sometimes feels too small, it may be hot inside, air barely flows through... that\'s why it\'s <color=red>fully open</color> now! You can go anywhere in a straight line!" },
				{ "Men_RoomChaos_Name", "Room Chaos" },
				{ "Men_RoomChaos_Desc", "Too much hallways? <color=red>Why not have none</color>? The school never needed them anyways!" }
			});

			SceneObject[] scObjs = new SceneObject[3]; // 0 = maze chaos, 1 = hall chaos
			int idx = 0;

			LoadingEvents.RegisterOnAssetsLoaded(Info, () =>
			{

				// -- Maze Chaos
				var mazeChaosMan = CreateManagerClone("MazeChaos", ChaosMode.MazeChaos);
				mazeChaosMan.managerNameKey = "Men_MazeChaos_Name";

				var mazeChaosScene = CreateSceneObjectClone("F3");

				mazeChaosScene.name = "MazeChaosSceneObject";
				mazeChaosScene.levelTitle = "CC1";
				mazeChaosScene.nameKey = "Men_MazeChaos_Name";
				mazeChaosScene.levelObject.name = "MazeChaosLevelObject";

				mazeChaosScene.levelObject.timeBonusLimit *= 2;
				mazeChaosScene.levelObject.timeBonusVal *= 2;
				mazeChaosScene.levelObject.timeLimit *= 3;

				bool foundPortalPoster = false;

				mazeChaosScene.levelObject.potentialItems.DoIf(x => x.selection.itemType == Items.PortalPoster, x => { x.weight *= 192; foundPortalPoster = true; });

				if (!foundPortalPoster)
					mazeChaosScene.levelObject.potentialItems = mazeChaosScene.levelObject.potentialItems.AddToArray(new WeightedItemObject() { selection = ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value, weight = 9249 });

				mazeChaosScene.levelObject.maxItemValue *= 4;
				mazeChaosScene.levelObject.roomGroup.Do(y =>
				{
					y.potentialRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => { z.chance *= 20f; z.weight = 9999; }));
					if (y.name == "Faculty")
					{
						y.minRooms *= 4;
						y.maxRooms *= 4;
					}
				});

				mazeChaosScene.additionalNPCs += 5;
				mazeChaosScene.levelObject.maxSize += new IntVector2(15, 10);
				mazeChaosScene.levelObject.minSize += new IntVector2(7, 8);

				mazeChaosScene.levelObject.minSpecialRooms = 2;
				mazeChaosScene.levelObject.maxSpecialRooms = 4;
				mazeChaosScene.levelObject.specialRoomsStickToEdge = true;

				mazeChaosScene.manager = mazeChaosMan;

				// --- Hall Chaos
				var hallChaosMan = CreateManagerClone("HallChaos", ChaosMode.HallChaos);
				hallChaosMan.managerNameKey = "Men_HallChaos_Name";

				var hallChaosScene = CreateSceneObjectClone("F3");

				hallChaosScene.name = "HallChaosSceneObject";
				hallChaosScene.levelTitle = "CC2";
				hallChaosScene.nameKey = "Men_HallChaos_Name";

				hallChaosScene.levelObject.name = "HallChaosLevelObject";

				hallChaosScene.additionalNPCs += 3;
				hallChaosScene.levelObject.maxSize += new IntVector2(25, 24);
				hallChaosScene.levelObject.minSize += new IntVector2(14, 16);
				hallChaosScene.levelObject.roomGroup.Do(x =>
				{
					x.minRooms *= 2;
					x.maxRooms *= 2;
				});

				hallChaosScene.levelObject.minSpecialRooms = 2;
				hallChaosScene.levelObject.maxSpecialRooms = 4;
				hallChaosScene.levelObject.specialRoomsStickToEdge = true;

				hallChaosScene.manager = hallChaosMan;

				// --- Room Chaos
				var roomChaosMan = CreateManagerClone("RoomChaos", ChaosMode.RoomChaos);
				roomChaosMan.managerNameKey = "Men_RoomChaos_Name";

				var roomChaosScene = CreateSceneObjectClone("F3");

				roomChaosScene.name = "RoomChaosSceneObject";
				roomChaosScene.levelTitle = "CC3";
				roomChaosScene.nameKey = "Men_RoomChaos_Name";

				roomChaosScene.levelObject.name = "RoomChaosLevelObject";

				roomChaosScene.additionalNPCs += 5;
				roomChaosScene.levelObject.timeLimit *= 4f;
				roomChaosScene.levelObject.outerEdgeBuffer += 10 * roomChaosScene.levelObject.roomGroup.Length;
				Dictionary<int, RoomGroup> activityGroups = [];
				for (int i = 0; i < roomChaosScene.levelObject.roomGroup.Length; i++)
				{
					var x = roomChaosScene.levelObject.roomGroup[i];
					if (!x.potentialRooms.Any(z => z.selection.hasActivity))
					{
						x.minRooms *= 6;
						x.maxRooms *= 6;
					}
					else
						activityGroups.Add(i, x);
					if (x.name == "Office")
						x.stickToHallChance = 1f;
					else
						x.stickToHallChance = 0f;
				}

				int index = roomChaosScene.levelObject.roomGroup.Length - activityGroups.Count;
				foreach (var roomPair in activityGroups) // Basically every activity room will be the last to spawn. So it goes really far
				{
					var group = roomChaosScene.levelObject.roomGroup[index];
					roomChaosScene.levelObject.roomGroup[index] = roomPair.Value;
					roomChaosScene.levelObject.roomGroup[roomPair.Key] = group;
					index++;
				}





				roomChaosScene.levelObject.includeBuffers = false;
				roomChaosScene.levelObject.fillEmptySpace = false;
				roomChaosScene.levelObject.exitCount = 1;

				roomChaosScene.levelObject.postPlotSpecialHallChance = 0;
				roomChaosScene.levelObject.potentialPostPlotSpecialHalls = [];
				roomChaosScene.levelObject.minPostPlotSpecialHalls = 0;
				roomChaosScene.levelObject.maxPostPlotSpecialHalls = 0;

				roomChaosScene.levelObject.additionTurnChance = 0;
				roomChaosScene.levelObject.bridgeTurnChance = 0;
				roomChaosScene.levelObject.deadEndBuffer = 0;

				roomChaosScene.levelObject.maxHallsToRemove = 0;
				roomChaosScene.levelObject.minHallsToRemove = 0;

				roomChaosScene.levelObject.maxHallAttempts = 0;

				roomChaosScene.levelObject.maxReplacementHalls = 0;
				roomChaosScene.levelObject.minReplacementHalls = 0;

				roomChaosScene.levelObject.maxSideHallsToRemove = 0;
				roomChaosScene.levelObject.minSideHallsToRemove = 0;

				roomChaosScene.levelObject.minSpecialRooms = 0;
				roomChaosScene.levelObject.maxSpecialRooms = 0;

				roomChaosScene.levelObject.lightMode = LightMode.Additive;
				roomChaosScene.levelObject.standardLightColor = new(1f, 0.95f, 0.94f);

				StructureWithParameters vent = new() {
					prefab = Resources.FindObjectsOfTypeAll<Structure_Vent>().First(x => x.GetInstanceID() > 0),
					parameters = new() {
						minMax = [
							new(1, 35), // z defines how many vent iterations will spawn
							new(4, 6), // Min max to tell how many corners the vent will have in its path (like, 5 segments before going straight).
							new(15, 0) // uses x axis to tell how far the vent's exit needs to be from the entrance
						]
					}
				};

				roomChaosScene.levelObject.forcedStructures = [vent];

				roomChaosScene.levelObject.minSpecialBuilders = 0;
				roomChaosScene.levelObject.maxSpecialBuilders = 0;

				roomChaosScene.manager = roomChaosMan;

				SceneObject CreateSceneObjectClone(string lvlName)
				{
					var sce = Resources.FindObjectsOfTypeAll<SceneObject>().First(x => x.GetInstanceID() > 0 && x.levelTitle == lvlName);
					var newSce = Instantiate(sce);
					newSce.MarkAsNeverUnload();

					newSce.levelObject = Instantiate(newSce.levelObject);
					newSce.levelObject.maxSpecialBuilders = Mathf.Min(newSce.levelObject.maxSpecialBuilders, newSce.levelObject.potentialStructures.Length);
					newSce.levelObject.minSpecialBuilders = Mathf.Min(newSce.levelObject.minSpecialBuilders, newSce.levelObject.maxSpecialBuilders);
					newSce.levelObject.finalLevel = false;

					scObjs[idx++] = newSce;

					return newSce;
				}

				static ChaosGameManager CreateManagerClone(string prefix, ChaosMode mode)
				{
					var man = Resources.FindObjectsOfTypeAll<MainGameManager>().First(x => x.GetInstanceID() > 0 && x.name.StartsWith("Lvl3"));

					man.gameObject.SetActive(false);
					var newMan = Instantiate(man); // To safely instantiate without triggering any Awake()
					man.gameObject.SetActive(true);

					newMan.name = prefix + "_MainGameManager";
					newMan.gameObject.ConvertToPrefab(true);
					newMan.gameObject.SetActive(true);

					

					var chaos = newMan.ReplaceComponent<ChaosGameManager, MainGameManager>();

					chaos.modeUsed = (int)mode;
					chaos.winScreen = Instantiate(Resources.FindObjectsOfTypeAll<ChallengeWin>().First(x => x.GetInstanceID() > 0));
					chaos.winScreen.transform.SetParent(chaos.transform);
					chaos.winScreen.transform.localPosition = Vector3.zero;
					chaos.winScreen.gameObject.SetActive(false);

					chaos.ambience = chaos.GetComponentInChildren<Ambience>();

					Destroy(newMan);

					return chaos;
				}


			}
			, true);

			// ----------------- Menu Setup ----------------

			CustomModesHandler.OnMainMenuInitialize += () =>
			{
				var chaosScreen = ModeObject.CreateBlankScreenInstance("Pick_ChaoticChallenges", true, new(-135f, 35f), new(135f, 35f));
				chaosScreen.StandardButtonBuilder.CreateSeedInput(out _);
				chaosScreen.StandardButtonBuilder.CreateTextLabel(Vector3.up * 110f, "Men_MazeChaos_Label");

				var modeBut = chaosScreen.StandardButtonBuilder.CreateModeButton(scObjs[0], lives: 0)
								.AddTextVisual("Men_MazeChaos_Name", out _);
				chaosScreen.StandardButtonBuilder.AddDescriptionText(modeBut, "Men_MazeChaos_Desc");

				modeBut = chaosScreen.StandardButtonBuilder.CreateModeButton(scObjs[1], lives: 0)
							.AddTextVisual("Men_HallChaos_Name", out _);
				chaosScreen.StandardButtonBuilder.AddDescriptionText(modeBut, "Men_HallChaos_Desc");

				modeBut = chaosScreen.StandardButtonBuilder.CreateModeButton(scObjs[2], lives: 0)
							.AddTextVisual("Men_RoomChaos_Name", out _);
				chaosScreen.StandardButtonBuilder.AddDescriptionText(modeBut, "Men_RoomChaos_Desc");

				var challengeObj = ModeObject.CreateModeObjectOverExistingScreen(SelectionScreen.ChallengesScreen);

				var transitBut = challengeObj.StandardButtonBuilder.CreateTransitionButton(chaosScreen)
								.AddTextVisual("Men_MazeChaos_Main_Name", out _);

				challengeObj.StandardButtonBuilder.AddDescriptionText(transitBut, "Men_MazeChaos_Main_Desc");
			};

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();
		}



	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbcrazymaze";
		internal const string Name = "BB+ Crazy School Maze";
		internal const string Version = "1.2.3.1";
	}


}

using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using ModdedModesAPI.ModesAPI;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using UnityEngine;

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
			AllAtOnceChaos = 3
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
				{ "Men_RoomChaos_Desc", "Too much hallways? <color=red>Why not have none</color>? The school never needed them anyways!" },
				{ "Men_AllTypesChallenge_Name", "All-at-once Chaos" },
				{ "Men_AllTypesChallenge_Desc", "<color=green>Baldi</color> thought it would be hilarious to <color=red>throw everything at you at once</color>! All the <color=blue>level types</color> combined into one <color=red>chaotic mess</color>! Can you survive <color=green>Baldi's</color> ultimate experiment?" }
			});

			SceneObject[] scObjs = new SceneObject[4]; // 0 = maze chaos, 1 = hall chaos, 2 = room chaos, 3 = all-at-once chaos
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
				roomChaosScene.levelObject.maxHallAttempts = 0;

				roomChaosScene.levelObject.minSpecialRooms = 0;
				roomChaosScene.levelObject.maxSpecialRooms = 0;

				roomChaosScene.levelObject.lightMode = LightMode.Additive;
				roomChaosScene.levelObject.standardLightColor = new(1f, 0.95f, 0.94f);

				StructureWithParameters vent = new()
				{
					prefab = Resources.FindObjectsOfTypeAll<Structure_Vent>().First(x => x.GetInstanceID() > 0),
					parameters = new()
					{
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

				// --- All-at-once Chaos
				var allAtOnceChaosMan = CreateManagerClone("AllAtOnceChaos", ChaosMode.AllAtOnceChaos);
				allAtOnceChaosMan.managerNameKey = "Men_AllTypesChallenge_Name";

				var allAtOnceChaosScene = CreateSceneObjectClone("F4", LevelType.Laboratory);

				allAtOnceChaosScene.name = "AllAtOnceChaosSceneObject";
				allAtOnceChaosScene.levelTitle = "CC4";
				allAtOnceChaosScene.nameKey = "Men_AllTypesChallenge_Name";
				allAtOnceChaosScene.levelObject.name = "AllAtOnceChaosLevelObject";

				// Combine features from all chaos types for a chaotic experience
				allAtOnceChaosScene.levelObject.timeBonusLimit *= 2;
				allAtOnceChaosScene.levelObject.timeBonusVal *= 2;
				allAtOnceChaosScene.levelObject.timeLimit *= 4f;
				allAtOnceChaosScene.levelObject.maxItemValue *= 4;
				allAtOnceChaosScene.levelObject.maxSize = new IntVector2(8, 8);
				allAtOnceChaosScene.levelObject.minSize = new IntVector2(5, 5);
				allAtOnceChaosScene.levelObject.outerEdgeBuffer += 3 * allAtOnceChaosScene.levelObject.roomGroup.Length;
				allAtOnceChaosScene.levelObject.includeBuffers = false;
				allAtOnceChaosScene.levelObject.fillEmptySpace = false;
				allAtOnceChaosScene.levelObject.exitCount = 1;
				allAtOnceChaosScene.levelObject.forcedItems.Clear();
				allAtOnceChaosScene.levelObject.minPlots = 1;
				allAtOnceChaosScene.levelObject.maxPlots = 1;
				allAtOnceChaosScene.levelObject.minPlotSize = 0;

				allAtOnceChaosScene.levelObject.standardLightColor = new(0.95f, 0.64f, 0.69f);
				// Adds all the forced structures from other levels
				allAtOnceChaosScene.levelObject.forcedStructures = allAtOnceChaosScene.levelObject.forcedStructures.AddNewStructures(GetRandomizedLevelObject("F4", LevelType.Maintenance).forcedStructures);
				allAtOnceChaosScene.levelObject.forcedStructures = allAtOnceChaosScene.levelObject.forcedStructures.AddNewStructures(GetRandomizedLevelObject("F4", LevelType.Factory).forcedStructures, typeof(Structure_ConveyorBelt));
				allAtOnceChaosScene.levelObject.forcedStructures = allAtOnceChaosScene.levelObject.forcedStructures.AddNewStructures(GetActualLevelObject("F3").forcedStructures);

				// Adds all the other special rooms from other levels
				allAtOnceChaosScene.levelObject.potentialSpecialRooms = allAtOnceChaosScene.levelObject.potentialSpecialRooms.AddNewSpecialRooms(GetRandomizedLevelObject("F4", LevelType.Maintenance).potentialSpecialRooms);
				allAtOnceChaosScene.levelObject.potentialSpecialRooms = allAtOnceChaosScene.levelObject.potentialSpecialRooms.AddNewSpecialRooms(GetRandomizedLevelObject("F4", LevelType.Factory).potentialSpecialRooms);

				allAtOnceChaosScene.levelObject.minSpecialRooms = allAtOnceChaosScene.levelObject.potentialSpecialRooms.Length;
				allAtOnceChaosScene.levelObject.maxSpecialRooms = allAtOnceChaosScene.levelObject.minSpecialRooms;

				// Grabbing all possible random textures
				allAtOnceChaosScene.levelObject.hallFloorTexs = allAtOnceChaosScene.levelObject.hallFloorTexs.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Maintenance).hallFloorTexs);
				allAtOnceChaosScene.levelObject.hallFloorTexs = allAtOnceChaosScene.levelObject.hallFloorTexs.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Factory).hallFloorTexs);

				allAtOnceChaosScene.levelObject.hallCeilingTexs = allAtOnceChaosScene.levelObject.hallCeilingTexs.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Maintenance).hallCeilingTexs);
				allAtOnceChaosScene.levelObject.hallCeilingTexs = allAtOnceChaosScene.levelObject.hallCeilingTexs.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Factory).hallCeilingTexs);

				allAtOnceChaosScene.levelObject.hallWallTexs = allAtOnceChaosScene.levelObject.hallWallTexs.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Maintenance).hallWallTexs);
				allAtOnceChaosScene.levelObject.hallWallTexs = allAtOnceChaosScene.levelObject.hallWallTexs.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Factory).hallWallTexs);

				allAtOnceChaosScene.levelObject.hallLights = allAtOnceChaosScene.levelObject.hallLights.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Maintenance).hallLights);
				allAtOnceChaosScene.levelObject.hallLights = allAtOnceChaosScene.levelObject.hallLights.AddRangeToArray(GetRandomizedLevelObject("F4", LevelType.Factory).hallLights);

				allAtOnceChaosScene.levelObject.maxLightDistance += 3;
				allAtOnceChaosScene.levelObject.standardLightStrength -= 4;

				// Add more NPCs for chaos
				allAtOnceChaosScene.additionalNPCs += 8;
				for (int i = 0; i < allAtOnceChaosScene.levelObject.roomGroup.Length; i++)
				{
					var group = allAtOnceChaosScene.levelObject.roomGroup[i];
					if (group.name != "Class" && group.name != "Office")
					{
						group.minRooms *= 6;
						group.maxRooms *= 6;
					}
				}

				allAtOnceChaosScene.manager = allAtOnceChaosMan;

				SceneObject GetSceneObject(string sceneName) =>
					Resources.FindObjectsOfTypeAll<SceneObject>().First(x => x.GetInstanceID() > 0 && x.levelTitle == sceneName);

				LevelObject GetRandomizedLevelObject(string sceneName, LevelType? preferredLevelType = null)
				{
					var sce = GetSceneObject(sceneName);
					if (preferredLevelType != null)
						return sce.randomizedLevelObject.First(x => x.selection.type == preferredLevelType).selection;

					if (sce.randomizedLevelObject.Length != 0)
						return sce.randomizedLevelObject[0].selection;

					return sce.levelObject;
				}

				LevelObject GetActualLevelObject(string sceneName, LevelType? preferredLevelType = null)
				{
					var sce = GetSceneObject(sceneName);
					LevelObject lvlObj;

					if (preferredLevelType == null && sce.levelObject)
						lvlObj = sce.levelObject;
					else
					{
						var selLevelObject = sce.randomizedLevelObject.FirstOrDefault(x => x.selection.type == preferredLevelType);
						if (selLevelObject == null)
							lvlObj = sce.randomizedLevelObject[0].selection; // Selects the first one then
						else
							lvlObj = selLevelObject.selection;
					}

					return lvlObj;
				}

				// RoomGroup GetARoomGroupFromSceneObject(string sceneName, string specificLevelObjectName, string groupName) =>
				// 	 GetRandomizedLevelObject(sceneName, specificLevelObjectName).roomGroup.First(x => x.name == groupName);


				SceneObject CreateSceneObjectClone(string lvlName, LevelType? levelType = null)
				{
					var sce = GetSceneObject(lvlName);
					var newSce = Instantiate(sce);
					newSce.MarkAsNeverUnload();

					LevelObject lvlObj;

					if (levelType == null && newSce.levelObject)
						lvlObj = newSce.levelObject;
					else
					{
						var selLevelObject = newSce.randomizedLevelObject.FirstOrDefault(x => x.selection.type == levelType);
						if (selLevelObject == null)
							lvlObj = newSce.randomizedLevelObject[0].selection; // Selects the first one then
						else
							lvlObj = selLevelObject.selection;
					}

					lvlObj = Instantiate(lvlObj);
					lvlObj.maxSpecialBuilders = Mathf.Min(lvlObj.maxSpecialBuilders, lvlObj.potentialStructures.Length);
					lvlObj.minSpecialBuilders = Mathf.Min(lvlObj.minSpecialBuilders, lvlObj.maxSpecialBuilders);
					lvlObj.finalLevel = false;
					newSce.levelObject = lvlObj;
					newSce.randomizedLevelObject = [];


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



					var chaos = newMan.gameObject.SwapComponent<MainGameManager, ChaosGameManager>();

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
			, LoadingEventOrder.Post);

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

				// Add All-at-once Chaos button
				modeBut = chaosScreen.StandardButtonBuilder.CreateModeButton(scObjs[3], lives: 0)
							.AddTextVisual("Men_AllTypesChallenge_Name", out _);
				chaosScreen.StandardButtonBuilder.AddDescriptionText(modeBut, "Men_AllTypesChallenge_Desc");

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
		internal const string Version = "1.2.4";
	}


}

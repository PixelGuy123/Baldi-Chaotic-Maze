﻿using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;

namespace BBSchoolMaze.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
	// 	[BepInDependency("pixelguy.pixelmodding.baldiplus.bbgenfixes", BepInDependency.DependencyFlags.HardDependency)] <-- obsolete

	public class BasePlugin : BaseUnityPlugin
	{
		void Awake()
		{
			LoadingEvents.RegisterOnAssetsLoaded(Info, () => {
				var poster = ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value;
				poster.price = 100;
				poster.value = 15;
				poster.MarkAsNeverUnload();
				Resources.FindObjectsOfTypeAll<SceneObject>().Do(z => z.shopItems.DoIf(x => x.selection.itemType == Items.PortalPoster, (x) => x.weight *= 192));
			}
			, false);
			GeneratorManagement.Register(this, GenerationModType.Base, (_, _2, ld) =>
			{
				ld.timeBonusLimit *= 2;
				ld.timeBonusVal *= 2;

				if (ld.potentialItems.Any(x => x.selection.itemType == Items.PortalPoster))
				{
					ld.potentialItems.DoIf(x => x.selection.itemType == Items.PortalPoster, (x) => {
						x.weight *= 192;
					});
				}
				else
					ld.potentialItems = ld.potentialItems.AddToArray(new WeightedItemObject() { selection = ItemMetaStorage.Instance.FindByEnum(Items.PortalPoster).value, weight = 924914701});
				
				ld.maxItemValue *= 4;
				ld.roomGroup.Do(y =>
				{
					y.potentialRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => { z.chance *= 20f; z.weight = 9999; }));
					if (y.name == "Faculty")
					{
						y.minRooms *= 4;
						y.maxRooms *= 4;
					}
				});
			});

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();
		}

	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbcrazymaze";
		internal const string Name = "BB+ Crazy School Maze";
		internal const string Version = "1.0.4";
	}


}

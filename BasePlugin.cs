using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Linq;

namespace BBSchoolMaze.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
	// 	[BepInDependency("pixelguy.pixelmodding.baldiplus.bbgenfixes", BepInDependency.DependencyFlags.HardDependency)] <-- obsolete

	public class BasePlugin : BaseUnityPlugin
	{
		void Awake()
		{
			LoadingEvents.RegisterOnAssetsLoaded(Info, () => {
				var poster = Items.PortalPoster.GetFirstInstance();
				poster.price = 25;
				poster.value = 35;
				poster.MarkAsNeverUnload();
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
					ld.shopItems.DoIf(x => x.selection.itemType == Items.PortalPoster, (x) =>
					{
						x.weight *= 192;
					});
				}
				else
					ld.potentialItems = ld.potentialItems.AddToArray(new WeightedItemObject() { selection = Items.PortalPoster.GetFirstInstance(), weight = 924914701});
				
				ld.maxItemValue *= 4;
				ld.maxFacultyRooms *= 4;
				ld.minFacultyRooms *= 4;
				ld.potentialClassRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => { z.chance *= 20f; z.weight = 9999; }));
				ld.potentialFacultyRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => { z.chance *= 20f; z.weight = 9999; }));
				ld.potentialOffices.Do(x => x.selection.itemSpawnPoints.ForEach(z => { z.chance *= 20f; z.weight = 9999; }));
				ld.potentialExtraRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => { z.chance *= 20f; z.weight = 9999; }));
			});

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();
		}

	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbcrazymaze";
		internal const string Name = "BB+ Crazy School Maze";
		internal const string Version = "1.0.3";
	}


}

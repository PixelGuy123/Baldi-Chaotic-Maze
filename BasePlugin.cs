using BepInEx;
using HarmonyLib;
using MTM101BaldAPI.Registers;

namespace BBSchoolMaze.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
	// 	[BepInDependency("pixelguy.pixelmodding.baldiplus.bbgenfixes", BepInDependency.DependencyFlags.HardDependency)] <-- obsolete

	public class BasePlugin : BaseUnityPlugin
	{
		void Awake()
		{
			GeneratorManagement.Register(this, GenerationModType.Base, (_, _2, ld) =>
			{
				ld.timeBonusLimit *= 2;
				ld.timeBonusVal *= 2;
				ld.items.DoIf(x => x.selection.itemType == Items.PortalPoster, (x) => {
					x.weight *= 12;
					x.selection.cost = 10;
					});
				ld.maxFacultyRooms *= 4;
				ld.minFacultyRooms *= 4;
				ld.potentialClassRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => z.chance *= 20f));
				ld.potentialFacultyRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => z.chance *= 20f));
				ld.potentialOffices.Do(x => x.selection.itemSpawnPoints.ForEach(z => z.chance *= 20f));
				ld.potentialExtraRooms.Do(x => x.selection.itemSpawnPoints.ForEach(z => z.chance *= 20f));
			});

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();
		}

	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbcrazymaze";
		internal const string Name = "BB+ Crazy School Maze";
		internal const string Version = "1.0.2";
	}


}

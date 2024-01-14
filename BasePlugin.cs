using BepInEx;
using HarmonyLib;
using MTM101BaldAPI.Registers;

namespace BBSchoolMaze.Plugin
{
	[BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]

	public class BasePlugin : BaseUnityPlugin
	{
		void Awake()
		{
			GeneratorManagement.Register(this, GenerationModType.Override, (_, _2, ld) =>
			{
				ld.timeBonusLimit *= 2;
				ld.timeBonusVal *= 2;
				ld.items.DoIf(x => x.selection.itemType == Items.PortalPoster, (x) => {
					x.weight *= 12;
					x.selection.cost = 10;
					});
				ld.itemChance *= 3;
				ld.maxFacultyRooms *= 2;
				ld.minFacultyRooms *= 2;
			});

			Harmony harmony = new(ModInfo.GUID);
			harmony.PatchAll();
		}

	}


	internal static class ModInfo
	{
		internal const string GUID = "pixelguy.pixelmodding.baldiplus.bbcrazymaze";
		internal const string Name = "BB+ Crazy School Maze";
		internal const string Version = "1.0.0";
	}


}

using System.Linq;
using HarmonyLib;

namespace BBSchoolMaze.Plugin;

public static class ArrayExtensions{

    public static StructureWithParameters[] AddNewStructures(this StructureWithParameters[] ar, StructureWithParameters[] otherAr, params System.Type[] exceptTypes){
        for (int i = 0; i < otherAr.Length; i++){
            if (!exceptTypes.Contains(otherAr[i].prefab.GetType()) && !ar.Any(x => x.prefab.name == otherAr[i].prefab.name))
                ar = ar.AddToArray(otherAr[i]);
        }
        return ar;
    }

    public static WeightedRoomAsset[] AddNewSpecialRooms(this WeightedRoomAsset[] ar, WeightedRoomAsset[] otherAr){
        for (int i = 0; i < otherAr.Length; i++){
            if (!ar.Any(x => x.selection == otherAr[i].selection))
                ar = ar.AddToArray(otherAr[i]);
        }
        return ar;
    }
}
using BBSchoolMaze.Patches;
using BBSchoolMaze.Plugin;
using System.Collections.Generic;
using UnityEngine;

namespace BBSchoolMaze
{
	public static class MazeGenerator
	{
		public static void Generate(RoomController room, System.Random cRng)
		{
			try
			{
				switch (MazeChaos.ChaosMode)
				{
					case BasePlugin.ChaosMode.MazeChaos:
						Internal_MazeGenerate(room, cRng);
						return;
					case BasePlugin.ChaosMode.HallChaos:
						Internal_HallGenerate(room.position, room);
						return;
					default:
						Debug.LogWarning("MAZE CHAOS: WHAT VALUE WAS GIVEN HERE?!!! Maze chaos by default!");
						Internal_MazeGenerate(room, cRng);
						return;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}
		}

		internal static void Internal_MazeGenerate(RoomController room, System.Random cRng)
		{
			List<Direction> potentialDirs = [];
			var pos = new IntVector2();
			room.ec.CreateCell(15, room.transform, pos + room.position, room);
			List<IntVector2> activeTiles = [pos];
			while (activeTiles.Count > 0)
			{
				int num7;
				if ((float)cRng.NextDouble() * 100f < 10f)
				{
					num7 = cRng.Next(0, activeTiles.Count);
				}
				else
				{
					num7 = activeTiles.Count - 1;
				}
				var tileController = room.ec.CellFromPosition(activeTiles[num7] + room.position);
				room.ec.FillUnfilledDirections(tileController, potentialDirs);
				for (int num8 = 0; num8 < potentialDirs.Count; num8++)
				{
					if (!room.ContainsCoordinates(tileController.position + potentialDirs[num8].ToIntVector2()) || !MazeChaos.IsInBorder(tileController.position + potentialDirs[num8].ToIntVector2()))
					{
						potentialDirs.RemoveAt(num8);
						num8--;
					}
				}
				if (potentialDirs.Count > 0)
				{
					Direction direction = potentialDirs[cRng.Next(0, potentialDirs.Count)];
					var intVector = tileController.position;
					room.ec.CreateCell(tileController.ConstBin - (1 << (int)direction), room.transform, intVector, room);
					room.ec.CreateCell(15 - (1 << (int)direction.GetOpposite()), room.transform, intVector + direction.ToIntVector2(), room);
					activeTiles.Add(intVector + direction.ToIntVector2() - room.position);
				}
				else
				{
					activeTiles.RemoveAt(num7);
				}
			}
		}

		internal static void Internal_HallGenerate(IntVector2 pos, RoomController room) // Since the patch goes into every tile, this can be simplified
		{
			int bin = 0;
			//Debug.Log("Building cell for position: " + pos);
			for (int i = 0; i < 4; i++) // Follows all directions
			{
				var dir = (Direction)i;
				int dirBin = dir.BitPosition();
				var nextPos = pos + dir.ToIntVector2();

				if (!bin.IsBitSet(dirBin) && !room.ec.ContainsCoordinates(nextPos) || !MazeChaos.IsInBorder(nextPos) || room.ec.CellFromPosition(nextPos).offLimits || (!room.ec.CellFromPosition(nextPos).Null && !room.ec.CellFromPosition(nextPos).TileMatches(room)))
					bin = bin.ToggleBit(dirBin);
			}

			room.ec.CreateCell(bin, room.transform, pos, room); // Cool open flag
		}

		static int ToggleBit(this int flag, int position) // 0000 flag (4-bit flag)
		{
			// Use XOR to flip the bit at the specified position
			return flag ^ (1 << position);
		}

		static bool IsBitSet(this int flag, int position)
		{
			// Check if the bit at the specified position is set (1)
			return (flag & (1 << position)) != 0;
		}
	}
}

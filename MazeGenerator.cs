using System.Collections.Generic;

namespace BBSchoolMaze
{
	public static class MazeGenerator
	{
		public static void Generate(RoomController room, System.Random cRng)
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
					if (!room.ContainsCoordinates(tileController.position + potentialDirs[num8].ToIntVector2()))
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
	}
}

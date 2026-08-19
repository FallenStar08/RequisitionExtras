using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerraStorageOverflow.Common.Utils.Items
{
    internal class TileResolverUtils
    {
        public static readonly Dictionary<int, int[]> VanillaAdjTiles = new()
        {
            { TileID.AdamantiteForge, new int[] { TileID.Hellforge, TileID.Furnaces } },
            { TileID.Hellforge, new int[] { TileID.Furnaces } },
            { TileID.MythrilAnvil, new int[] { TileID.Anvils } },
            { TileID.AlchemyTable, new int[] { TileID.Bottles } },
        };

        public static HashSet<int> GetProvidedTiles(int tileId)
        {
            var results = new HashSet<int>();
            if (tileId < TileID.Dirt)
                return results;

            var queue = new Queue<int>();
            queue.Enqueue(tileId);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                if (!results.Add(current))
                    continue;

                if (VanillaAdjTiles.TryGetValue(current, out int[] vanillaAdj))
                {
                    foreach (int adj in vanillaAdj)
                        queue.Enqueue(adj);
                }

                ModTile modTile = TileLoader.GetTile(current);
                if (modTile != null && modTile.AdjTiles != null)
                {
                    foreach (int adj in modTile.AdjTiles)
                        queue.Enqueue(adj);
                }
            }

            return results;
        }

        public static string GetTileName(int tileId)
        {
            if (tileId < 0)
                return "None";

            ModTile modTile = TileLoader.GetTile(tileId);
            if (modTile != null)
                return modTile.Name;

            string name = TileID.Search.GetName(tileId);
            return string.IsNullOrEmpty(name) ? $"Tile_{tileId}" : name;
        }
    }
}

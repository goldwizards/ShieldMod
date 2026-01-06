using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ShieldMod.Systems
{
    /// <summary>
    /// Adds Absorption Sigil to Dungeon biome key chests (Corruption/Crimson),
    /// so Corruption worlds are not locked out by Vampire Knives.
    ///
    /// Tooltip/text changes are NOT handled here. This is loot injection only.
    /// Applies on new world generation (PostWorldGen).
    /// </summary>
    public class AbsorptionSigilChestLootSystem : ModSystem
    {
        public override void PostWorldGen()
        {
            int sigilType = ModContent.ItemType<Items.Accessories.AbsorptionSigil>();

            for (int c = 0; c < Main.maxChests; c++)
            {
                Chest chest = Main.chest[c];
                if (chest == null)
                    continue;

                int x = chest.x;
                int y = chest.y;

                if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY)
                    continue;

                Tile tile = Main.tile[x, y];
                if (tile == null || !tile.HasTile)
                    continue;

                // Biome key chests are vanilla chest tiles and show the biome key as their icon.
                // We detect them by chest style -> icon mapping rather than hardcoding style IDs (more version-proof).
                if (tile.TileType != TileID.Containers && tile.TileType != TileID.Containers2)
                    continue;

                // FrameX is in 36px-wide styles.
                int style = tile.TileFrameX / 36;

                int iconItem = -1;
                if (tile.TileType == TileID.Containers2)
                {
                    if (style >= 0 && style < Chest.maxChestTypes2)
                        iconItem = Chest.chestTypeToIcon2[style];
                }
                else // TileID.Containers
                {
                    if (style >= 0 && style < Chest.maxChestTypes)
                        iconItem = Chest.chestTypeToIcon[style];
                }

                bool isCorruptionChest = iconItem == ItemID.CorruptionKey;
                bool isCrimsonChest = iconItem == ItemID.CrimsonKey;

                if (!isCorruptionChest && !isCrimsonChest)
                    continue;

                // Prevent duplicates if something else already inserted it.
                if (ChestContains(chest, sigilType))
                    continue;

                int slot = FindEmptySlot(chest);
                if (slot < 0)
                    slot = 39; // fallback: last slot (very unlikely to be occupied in biome chests)

                chest.item[slot].SetDefaults(sigilType);
                chest.item[slot].stack = 1;
            }
        }

        private static bool ChestContains(Chest chest, int itemType)
        {
            for (int i = 0; i < Chest.maxItems; i++)
            {
                Item item = chest.item[i];
                if (item != null && item.type == itemType)
                    return true;
            }
            return false;
        }

        private static int FindEmptySlot(Chest chest)
        {
            for (int i = 0; i < Chest.maxItems; i++)
            {
                Item item = chest.item[i];
                if (item == null || item.type == ItemID.None)
                    return i;
            }
            return -1;
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using ShieldMod.Players;

namespace ShieldMod.Items.Accessories
{
    public class CoreOfProtection : ModItem
    {
        public override string Texture => "ShieldMod/Textures/Items/Accessories/CoreOfProtection";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Yellow;
            Item.value = Item.sellPrice(gold: 5);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
                   tooltips.Add(new TooltipLine(Mod, "Tooltip", @"When shield is active:
+20% defense
-10% incoming damage
+50% shield regen"));
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<MyModPlayer>();
            if (modPlayer.CurrentShield > 0)
            {
                player.statDefense *= 1.2f;
                player.endurance += 0.10f;
                modPlayer.ShieldRegenBonus += 0.5f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<EssenceOfProtection>(), 1)
                .AddIngredient(ItemID.LunarBar, 10)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}

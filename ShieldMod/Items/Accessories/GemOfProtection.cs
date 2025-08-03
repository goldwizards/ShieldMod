using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using ShieldMod.Players;

namespace ShieldMod.Items.Accessories
{
    public class GemOfProtection : ModItem
    {
        public override string Texture => "ShieldMod/Textures/Items/Accessories/GemOfProtection";
        
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Tooltip", "When shield is active:\n+10% defense"));
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.GetModPlayer<MyModPlayer>().CurrentShield > 0)
            {
                player.statDefense *= 1.1f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<FragmentOfProtection>(), 1)
                .AddIngredient(ItemID.Sapphire, 5)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}

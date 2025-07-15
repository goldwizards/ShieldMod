using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using ShieldMod.Players;

namespace ShieldMod.Items.Accessories
{
    public class FragmentOfProtection : ModItem
    {
        public override string Texture => "ShieldMod/Textures/Items/Accessories/FragmentOfProtection";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 25);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Tooltip", "When shield is active:\n+5% defense"));
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.GetModPlayer<MyModPlayer>().CurrentShield > 0)
            {
                player.statDefense *= 1.05f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.FallenStar, 2)
                .AddRecipeGroup(RecipeGroupID.IronBar, 5) // Iron or Lead
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}

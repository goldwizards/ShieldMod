using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using ShieldMod.Players;

namespace ShieldMod.Items.Accessories
{
    public class EssenceOfProtection : ModItem
    {
        public override string Texture => "ShieldMod/Textures/Items/Accessories/EssenceOfProtection";
        
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = Item.sellPrice(gold: 1);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "Tooltip", "When shield is active:\n+15% defense\n-5% incoming damage\n+30% shield regen"));
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<MyModPlayer>();
            if (modPlayer.CurrentShield > 0)
            {
                player.statDefense *= 1.15f;
                player.endurance += 0.05f;
                modPlayer.ShieldRegenBonus += 0.3f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<GemOfProtection>(), 1)
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }
}

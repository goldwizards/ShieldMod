using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ShieldMod.Interfaces;

namespace ShieldMod.Items.Accessories
{
    public class FragmentOfProtection : ModItem, IProtectionTierAccessory
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

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.GetModPlayer<MyModPlayer>().CurrentShield > 0)
            {
                // 방어력 +5% (정수 방어력 특성상 반올림, 최소 +1 보장)
				// Math.Round 오버로드 모호성 방지: double 상수 사용
				int add = (int)System.Math.Round(player.statDefense * 0.05);
                if (player.statDefense > 0 && add < 1) add = 1;
                player.statDefense += add;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.FallenStar, 4)
                .AddRecipeGroup("ShieldMod:AnySilverBar", 6) // 은/텅스텐
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            if (player.GetModPlayer<MyModPlayer>().CurrentShield > 0)
            {
                // 방어력 +10% (정수 방어력 특성상 반올림, 최소 +1 보장)
				// Math.Round 오버로드 모호성 방지: double 상수 사용
				int add = (int)System.Math.Round(player.statDefense * 0.10);
                if (player.statDefense > 0 && add < 1) add = 1;
                player.statDefense += add;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<FragmentOfProtection>(), 1) // 업그레이드 체인
                .AddRecipeGroup("ShieldMod:AnyDemoniteBar", 6)                 // 데모/크림테인
                .AddIngredient(ItemID.Sapphire, 8)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}

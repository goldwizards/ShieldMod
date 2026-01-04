using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

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

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<MyModPlayer>();
            if (modPlayer.CurrentShield > 0)
            {
                // 방어력 +20% (정수 방어력 특성상 반올림, 최소 +1 보장)
				// Math.Round 오버로드 모호성 방지: double 상수 사용
				int add = (int)System.Math.Round(player.statDefense * 0.20);
                if (player.statDefense > 0 && add < 1) add = 1;
                player.statDefense += add;
                player.endurance += 0.10f;
                modPlayer.ShieldRegenBonus += 0.6f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<EssenceOfProtection>(), 1) // 업그레이드 체인
                .AddIngredient(ItemID.FragmentNebula, 5)
                .AddIngredient(ItemID.FragmentSolar, 5)
                .AddIngredient(ItemID.FragmentVortex, 5)
                .AddIngredient(ItemID.FragmentStardust, 5)
                .AddIngredient(ItemID.LunarBar, 10)                           // Luminite Bar
                .AddTile(TileID.LunarCraftingStation)                         // Ancient Manipulator
                .Register();
        }
    }
}

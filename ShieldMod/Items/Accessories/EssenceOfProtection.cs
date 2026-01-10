using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ShieldMod.Interfaces;

namespace ShieldMod.Items.Accessories
{
    public class EssenceOfProtection : ModItem, IProtectionTierAccessory
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

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<MyModPlayer>();
            if (modPlayer.CurrentShield > 0)
            {
                // 방어력 +15% (정수 방어력 특성상 반올림, 최소 +1 보장)
				// Math.Round 오버로드 모호성 방지: double 상수 사용
				int add = (int)System.Math.Round(player.statDefense * 0.15);
                if (player.statDefense > 0 && add < 1) add = 1;
                player.statDefense += add;
                player.endurance += 0.05f;
                modPlayer.ShieldRegenBonus += 0.3f;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ModContent.ItemType<GemOfProtection>(), 1) // 업그레이드 체인
                .AddIngredient(ItemID.SoulofNight, 5)
                .AddIngredient(ItemID.SoulofLight, 5)
                .AddIngredient(ItemID.CrystalShard, 6)                    // 요청 반영: 수정 파편
                .AddTile(TileID.MythrilAnvil)                             // 미스릴/오리할콘 모루
                .Register();
        }
    }
}

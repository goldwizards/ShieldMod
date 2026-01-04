using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ShieldMod.Items.Accessories
{
    /// <summary>
    /// 흡수의 인장
    /// - 입힌 피해량의 4%만큼 보호막 회복 (직접 타격만)
    /// - 디버프/DoT(지속피해)는 OnHit 훅이 호출되지 않으므로 자연히 제외됩니다.
    /// </summary>
    public class AbsorptionSigil : ModItem
    {
        public override string Texture => "ShieldMod/Textures/Items/Accessories/AbsorptionSigil";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Lime; // post-Plantera 느낌
            Item.value = Item.sellPrice(gold: 7);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<AbsorptionSigilPlayer>().HasAbsorptionSigil = true;
        }

        public override void AddRecipes()
        {
            // 뱀파이어 단검처럼 후반(플랜테라 이후) 기준
            CreateRecipe()
                .AddIngredient(ItemID.VampireKnives, 1)
                .AddIngredient(ModContent.ItemType<EssenceOfProtection>(), 1)
                .AddIngredient(ItemID.Ectoplasm, 10)
                .AddIngredient(ItemID.SoulofNight, 12)
                .AddTile(TileID.TinkerersWorkbench)
                .Register();
        }
    }
}

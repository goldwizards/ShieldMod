using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ShieldMod.Items.Accessories
{
    public class EmergencyAegis : ModItem
    {
        public override string Texture => "ShieldMod/Textures/Items/Accessories/EmergencyAegis";

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.buyPrice(gold: 10);
            Item.rare = ItemRarityID.Red; // Moon Lord tier
            Item.accessory = true;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<EmergencyAegisPlayer>().HasAegis = true;
        }
    }
}
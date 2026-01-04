using Terraria;
using Terraria.ModLoader;

namespace ShieldMod.Systems
{
    public class ShieldTextFlushSystem : ModSystem
    {
        public override void PostUpdateEverything()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                var p = Main.player[i];
                if (p?.active == true)
                    p.GetModPlayer<EmergencyAegisPlayer>().FlushShieldHealTextIfAny();
            }
        }
    }
}
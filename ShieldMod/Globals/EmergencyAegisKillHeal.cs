using Terraria;
using Terraria.ModLoader;

namespace ShieldMod.Globals
{
    public class EmergencyAegisKillHeal : GlobalNPC
    {
        public override void OnKill(NPC npc)
        {
            int idx = npc.lastInteraction;
            if (idx < 0 || idx >= Main.maxPlayers) return;

            Player player = Main.player[idx];
            if (player == null || !player.active) return;

            var aegis = player.GetModPlayer<EmergencyAegisPlayer>();
            if (!aegis.HasAegis) return;

            // 세그먼트 보스: 마스터(realLife)만 처리
            bool isBoss = npc.boss || (npc.realLife >= 0 && Main.npc[npc.realLife].boss);
            if (isBoss && npc.realLife >= 0 && npc.whoAmI != npc.realLife) return;

            aegis.TryOnKillHeal(isBoss);
        }
    }
}
using Terraria;
using Terraria.ModLoader;

namespace ShieldMod.Globals
{
    public class BossKillShieldHeal : GlobalNPC
    {
        private const float HealRatio = 0.25f; // 보스 처치 시 최대 보호막의 25%

        public override void OnKill(NPC npc)
        {
            // 보스 또는 세그먼트 보스의 마스터만 처리
            bool isBoss = npc.boss || (npc.realLife >= 0 && Main.npc[npc.realLife].boss);
            if (!isBoss) return;
            if (npc.realLife >= 0 && npc.whoAmI != npc.realLife) return; // 세그먼트 중복 방지

            int idx = npc.lastInteraction;
            if (idx < 0 || idx >= Main.maxPlayers) return;

            Player player = Main.player[idx];
            if (player == null || !player.active) return;

            var mp = player.GetModPlayer<MyModPlayer>();
            if (mp.maxShield <= 0) return;

            int heal = (int)(mp.maxShield * HealRatio);
            if (heal <= 0) return;

            int before = mp.shield;
            int after = before + heal;
            if (after > mp.maxShield) after = mp.maxShield;
            int gained = after - before;
            if (gained <= 0) return;

            mp.shield = after;

            // 보스 25% 회복은 Aegis 합산 텍스트로 묶어서 표시
            player.GetModPlayer<EmergencyAegisPlayer>().QueueShieldHealText(gained);
        }
    }
}

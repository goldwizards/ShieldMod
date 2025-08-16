using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using ShieldMod.Players; // ← MyModPlayer 네임스페이스에 맞게 수정

namespace ShieldMod.Globals
{
    public class BossKillShieldHeal : GlobalNPC
    {
        private const float HealRatio = 0.25f; // 25%

        public override void OnKill(NPC npc)
        {
            if (!npc.boss) return;

            int idx = npc.lastInteraction;
            if (idx < 0 || idx >= Main.maxPlayers) return;

            Player player = Main.player[idx];
            if (player == null || !player.active) return;

            var mp = player.GetModPlayer<MyModPlayer>();

            int maxShield = mp.maxShield; // 소문자 필드 사용
            if (maxShield <= 0) return;

            int heal = (int)(maxShield * HealRatio);
            if (heal <= 0) return;

            int before = mp.shield; // 소문자 필드 사용
            int after = before + heal;
            if (after > maxShield) after = maxShield;
            int gained = after - before;
            if (gained <= 0) return;

            mp.shield = after; // 소문자 필드 사용

            CombatText.NewText(player.getRect(), Color.Cyan, $"+{gained}", dramatic: true);
        }
    }
}

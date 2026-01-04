using Terraria;
using Terraria.ModLoader;

namespace ShieldMod.Globals
{
    public class MyGlobalNPC : GlobalNPC
    {
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            // 보호막 흡수/감산은 ModifyHurt(ShieldPreAbsorbPlayer)에서 단일 처리합니다.
            // (중복 흡수/쿨다운 덮어쓰기/텍스트·사운드 중복 출력 방지)
        }
    }
}


using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace ShieldMod.Buffs
{
    public class EmergencyAegisCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // 이름/설명은 Localization에서 불러옵니다.
            Main.debuff[Type] = true;                   // 디버프로 보이기
            Main.buffNoTimeDisplay[Type] = false;       // 남은 시간 표시
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true; // 간호사 제거 불가
        }
    }
}
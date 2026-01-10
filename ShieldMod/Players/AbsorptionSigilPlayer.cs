using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ShieldMod
{
    /// <summary>
    /// 흡수의 인장 효과:
    /// - 입힌 피해량의 3%만큼 보호막 회복 (누적 방식)
    /// - DoT/디버프 피해는 OnHit 콜백이 없으므로 자동 제외
    /// - 보호막이 0 이하로 '떨어지는 순간'(파괴 순간)부터 흡수량 50% 유지(= 1.5%)
    ///   · 기본 6초(360틱)
    ///   · Emergency Aegis 착용 시 4초(240틱)
    /// </summary>
    public class AbsorptionSigilPlayer : ModPlayer
    {
        public bool HasAbsorptionSigil;

        // 3% = 30/1000 → (damageDone * 30)을 누적해 1000마다 1 보호막 회복
        private int _accumPermille;

        // 보호막 파괴(0 이하로 떨어짐) 순간 감지용
        private int _prevShield;

        // 흡수량 50% 유지 남은 시간(틱)
        private int _siphonPenaltyTicks;

        // UI 표시용(읽기 전용)
        public int SiphonPenaltyTicks => _siphonPenaltyTicks;

        public override void Initialize()
        {
            _accumPermille = 0;
            _prevShield = 0;
            _siphonPenaltyTicks = 0;
        }

        public override void ResetEffects() => HasAbsorptionSigil = false;

        public override void OnEnterWorld()
        {
            _accumPermille = 0;
            _siphonPenaltyTicks = 0;

            var mp = Player.GetModPlayer<MyModPlayer>();
            _prevShield = mp?.shield ?? 0;
        }

        public override void OnRespawn()
        {
            _accumPermille = 0;
            _prevShield = 0;
            _siphonPenaltyTicks = 0;
        }

        public override void PostUpdate()
        {
            if (_siphonPenaltyTicks > 0)
                _siphonPenaltyTicks--;

            var mp = Player.GetModPlayer<MyModPlayer>();
            int curShield = mp?.shield ?? 0;

            // 이전이 1 이상이고, 현재가 0 이하가 되는 순간에만 발동(연속 재발동 방지)
            if (_prevShield > 0 && curShield <= 0)
            {
                bool hasAegis = Player.GetModPlayer<EmergencyAegisPlayer>()?.HasAegis == true;
                _siphonPenaltyTicks = hasAegis ? 180 : 300; // 3초 or 5초
            }

            _prevShield = curShield;
        }

        // ✅ tModLoader 1.4.4(net8) 정식 훅: 아이템 타격
        public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryHealShieldFromDamage(target, damageDone);
        }

        // ✅ tModLoader 1.4.4(net8) 정식 훅: 투사체 타격
        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            TryHealShieldFromDamage(target, damageDone);
        }

        private void TryHealShieldFromDamage(NPC target, int damageDone)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!HasAbsorptionSigil || damageDone <= 0 || Player.statLife <= 0)
                return;

            if (target == null || !target.active || target.friendly || target.townNPC)
                return;

            var mp = Player.GetModPlayer<MyModPlayer>();
            if (mp == null || mp.maxShield <= 0 || mp.shield >= mp.maxShield)
                return;

            // 흡수량: 기본 2%, 보호막 파괴 후 패널티 동안 50%(= 1%)
            int permille = (_siphonPenaltyTicks > 0) ? 10 : 20;

            // 누적
            _accumPermille += damageDone * permille;

            int heal = _accumPermille / 1000;
            if (heal <= 0) return;

            _accumPermille %= 1000;

            int before = mp.shield;
            int after = before + heal;
            if (after > mp.maxShield) after = mp.maxShield;

            int gained = after - before;
            if (gained <= 0) return;

            mp.shield = after;

            // 텍스트는 Aegis 합산 시스템을 그대로 재사용(다음 프레임 1번만 표시)
            Player.GetModPlayer<EmergencyAegisPlayer>()?.QueueShieldHealText(gained);
        }

        public bool IsSiphonPenaltyActive => _siphonPenaltyTicks > 0;
    }
}

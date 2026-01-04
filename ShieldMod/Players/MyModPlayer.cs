using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ShieldMod
{
    public class MyModPlayer : ModPlayer
    {
        // === 기본 실드 상태 ===
        public int shield;
        public int maxShield;
        public int shieldBreakCooldown;   // 실드 파괴 후 대기(틱)
        public int timeSinceLastHit;      // 마지막으로 피격된 후 경과 틱
        private int regenTimer;           // 자연 재생 타이머(틱)

        // 시각 효과(원본에 맞게 유지)
        public bool showHitEffect;
        private int hitEffectTimer;

        // 완전 흡수(HP가 실제로 닳지 않아야 하는 상황)에서 빨간 데미지 CombatText(최소 1 등)를 잠깐 숨기기 위한 플래그
        // - 엔진 내부에서 데미지 텍스트가 먼저 생성되는/최소 1로 클램프되는 케이스 대응
        public int suppressRedDamageTextTicks;

        // 외부에서 조정하던 보너스 값(원본 유지)
        public float ShieldRegenBonus;
        public float DamageReduction;

        // 편의 프로퍼티
        public int CurrentShield => shield;
        public int MaxShield => maxShield;
        public int HitEffectTimer => hitEffectTimer;

        // 외부(선흡수 등)에서 안전하게 호출할 수 있는 유틸(리플렉션 제거)
        public void ResetAegisRegenTokens()
        {
            _aegisTickAccum = 0;
            _aegisPending = 0;
        }

        public void TriggerShieldOverlay(int minTimer = 10)
        {
            showHitEffect = true;
            if (hitEffectTimer < minTimer) hitEffectTimer = minTimer;
        }

        public void SuppressRedDamageText(int ticks = 2)
        {
            if (ticks > suppressRedDamageTextTicks)
                suppressRedDamageTextTicks = ticks;
        }

        // === Emergency Aegis: 기본 재생(+2/s) 통합 ===
        // 틱당 +2 누적 → 60마다 토큰 1개 생성(= +1 회복), 초당 정확히 +2
        private int _aegisTickAccum;   // 생성 누적기
        private int _aegisPending;     // 소비 대기 토큰(= +1 회복)

        public override void OnEnterWorld()
        {
            // 설정은 건드리지 않되, 기존 로직 그대로 사용
            float ratio = MathHelper.Clamp(ModContent.GetInstance<ShieldModConfig>().ShieldMaxRatio, 0.25f, 1f);
            maxShield = (int)(Player.statLifeMax2 * ratio);
            shield = maxShield;
        }

        public override void OnRespawn()
        {
            float ratio = MathHelper.Clamp(ModContent.GetInstance<ShieldModConfig>().ShieldMaxRatio, 0.25f, 1f);
            maxShield = (int)(Player.statLifeMax2 * ratio);
            shield = maxShield;
            shieldBreakCooldown = 0;

            // Aegis 보너스 재생 상태 초기화
            _aegisTickAccum = 0;
            _aegisPending = 0;
        }

        public override void ResetEffects()
        {
            if (shield > maxShield) shield = maxShield;
            ShieldRegenBonus = 0f;
            DamageReduction = 0f;
        }

        public override void PostUpdate()
        {
            bool hasAegis = Player.GetModPlayer<EmergencyAegisPlayer>().HasAegis;
            bool hasAbsorptionSigil = Player.GetModPlayer<AbsorptionSigilPlayer>().HasAbsorptionSigil;

            if (suppressRedDamageTextTicks > 0)
                suppressRedDamageTextTicks--;

            // 최대치 재계산(원본 유지)
            float ratio = MathHelper.Clamp(ModContent.GetInstance<ShieldModConfig>().ShieldMaxRatio, 0.25f, 1f);
            int newMax = (int)(Player.statLifeMax2 * ratio);
            if (newMax != maxShield)
            {
                float keepRatio = maxShield > 0 ? (float)shield / maxShield : 1f;
                maxShield = newMax;
                shield = (int)(maxShield * keepRatio);
                if (shield > maxShield) shield = maxShield;
            }
            else
            {
                maxShield = newMax;
                if (shield > maxShield) shield = maxShield;
            }

            regenTimer++;
            timeSinceLastHit++;

            if (hitEffectTimer > 0)
            {
                hitEffectTimer--;
                if (hitEffectTimer <= 0) showHitEffect = false;
            }

            // 쿨다운 중에는 자연 재생/보너스 재생 둘 다 정지
            if (shieldBreakCooldown > 0)
            {
                shieldBreakCooldown--;
                return;
            }

            // ===== 흡수의 인장: '모든 재생' 차단 =====
            // - 자연 재생 + Emergency Aegis의 기본(+2/s) 재생은 모두 차단
            // - Absorption(딜 4%)과 Emergency Aegis의 '긴급 HoT'는 별도 시스템이므로 정상 작동
            if (hasAbsorptionSigil)
            {
                // 재생이 완전히 멈춘 상태를 유지(가속도/틱 쌓임 방지)
                regenTimer = 0;
                timeSinceLastHit = 0; 
                               
                // 토큰 누적/잔여가 남아있으면, 인장 해제 순간에 튀어오르는 것 방지
                _aegisTickAccum = 0;
                _aegisPending = 0;
                return;
            }

            // ===== 기본 자연 재생 로직(원본 유지) =====
            float regenPerSecond = 1f;
            if (timeSinceLastHit >= 300) regenPerSecond = 2f;
            if (timeSinceLastHit >= 600) regenPerSecond = 3f;
            if (timeSinceLastHit >= 900) regenPerSecond = 5f;
            if (timeSinceLastHit >= 1200) regenPerSecond = 8f;
            if (timeSinceLastHit >= 1800) regenPerSecond = 12f;
            if (timeSinceLastHit >= 2400) regenPerSecond = 20f;

            regenPerSecond *= 1f + ShieldRegenBonus;

            // 보스전 상한(원본 유지) - LINQ 제거(할당/오버헤드 방지)
            bool anyBoss = false;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (n != null && n.active && n.boss) { anyBoss = true; break; }
            }
            if (anyBoss)
            {
                float bossLimit = 5f * (1f + ShieldRegenBonus);
                if (regenPerSecond > bossLimit) regenPerSecond = bossLimit;
            }

            int interval = (int)(60f / regenPerSecond);
            if (regenPerSecond > 0f && interval > 0 && regenTimer % interval == 0 && shield < maxShield)
            {
                shield++; // 자연 재생 +1
            }

            // ===== Emergency Aegis: 기본 재생 +2/s (파괴 후 3초 체감, 정확히 +2/s 보장) =====
            if (hasAegis && Player.statLife > 0 && shield < maxShield)
            {
                // 토큰 생성: 틱당 +2 누적 → 60마다 1개 = 초당 정확히 +2
                _aegisTickAccum += 2;
                while (_aegisTickAccum >= 60)
                {
                    _aegisTickAccum -= 60;
                    _aegisPending++;
                }

                // 토큰 소비: 그 틱에 1개만 소비(자연 재생과 비슷한 타이밍으로 보임)
                if (_aegisPending > 0)
                {
                    shield += 1;
                    if (shield > maxShield) shield = maxShield;
                    _aegisPending -= 1;
                    // 텍스트 없음
                }
            }
            else
            {
                // 조건 미충족 시 폭주 방지
                if (shield >= maxShield)
                {
                    _aegisTickAccum = 0;
                    _aegisPending = 0;
                }
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            // 보호막 흡수/감산은 ModifyHurt(ShieldPreAbsorbPlayer)에서 선처리합니다.
            // 여기서는 "피격 이후 자연재생 단계"를 위해 마지막 피격 시간만 리셋합니다.
            if (info.Damage > 0)
                timeSinceLastHit = 0;
        }
    }

}


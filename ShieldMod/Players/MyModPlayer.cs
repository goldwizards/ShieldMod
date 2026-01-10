using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

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


        // === 저장/복원 (재접속으로 풀충전 악용 방지) ===
        private bool _loadedFromSave;
        private int _savedShield;
        private int _savedBreakCd;
        // 시각 효과(원본에 맞게 유지)
        public bool showHitEffect;
        public bool LastShieldHitStrong; // for Impact-only VFX
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
        public int ShieldBreakCooldownTicks => shieldBreakCooldown;
        public int TimeSinceLastHitTicks => timeSinceLastHit;
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

        // === 멀티 동기화 ===
        private int _netSyncTick;
        private int _netLastShield;
        private int _netLastMax;
        private int _netLastBreakCd;
        private int _netLastTimeSinceHit;

        private static float GetShieldMaxRatio()
        {
            // 멀티에서는 서버 설정을 권위로 사용합니다.
            if (Main.netMode == NetmodeID.Server)
                return MathHelper.Clamp(ModContent.GetInstance<ShieldModServerConfig>().ShieldMaxRatio, 0.25f, 1f);

            return MathHelper.Clamp(ModContent.GetInstance<ShieldModConfig>().ShieldMaxRatio, 0.25f, 1f);
        }
        public override void SaveData(TagCompound tag)
        {
            // 현재 보호막을 그대로 저장 (재접속 풀충전 방지)
            tag["ShieldMod_Shield"] = shield;
            tag["ShieldMod_BreakCd"] = shieldBreakCooldown;
        }

        public override void LoadData(TagCompound tag)
        {
            _loadedFromSave = tag.ContainsKey("ShieldMod_Shield");
            if (_loadedFromSave)
            {
                _savedShield = tag.GetInt("ShieldMod_Shield");
                _savedBreakCd = tag.GetInt("ShieldMod_BreakCd");
            }
            else
            {
                _savedShield = 0;
                _savedBreakCd = 0;
            }
        }



        public override void OnEnterWorld()
        {
            // 싱글/서버: 설정값으로 초기화
            // 클라는 서버에서 Sync를 받기 전까지 임시값을 넣되, 서버에 즉시 동기화를 요청합니다.
            float ratio = GetShieldMaxRatio();
            maxShield = (int)(Player.statLifeMax2 * ratio);

            // 재접속 시 풀충전하지 말고, 저장된 현재 보호막을 유지합니다.
            if (_loadedFromSave)
            {
                shield = Utils.Clamp(_savedShield, 0, maxShield);
                shieldBreakCooldown = Utils.Clamp(_savedBreakCd, 0, 60 * 60 * 60); // 안전 클램프(1시간)
            }
            else
            {
                shield = maxShield; // 최초 입장(저장값 없음)만 풀 충전
                shieldBreakCooldown = 0;
            }

            // 재접속으로 재생 단계(timeSinceLastHit)가 쌓이는 악용/튐 방지
            regenTimer = 0;
            timeSinceLastHit = 0;

            _netLastShield = shield;
            _netLastMax = maxShield;
            _netLastBreakCd = shieldBreakCooldown;
            _netLastTimeSinceHit = timeSinceLastHit;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write((byte)ShieldMod.Msg.RequestShieldSync);
                packet.Send();
            }
        }

        public override void OnRespawn()
        {
            float ratio = GetShieldMaxRatio();
            maxShield = (int)(Player.statLifeMax2 * ratio);
            shield = (maxShield + 1) / 2; // 부활 시 보호막은 절반만
            shieldBreakCooldown = 0;
            regenTimer = 0;
            timeSinceLastHit = 0;

            // Aegis 보너스 재생 상태 초기화
            _aegisTickAccum = 0;
            _aegisPending = 0;

            // 리스폰 직후 서버가 값 갱신을 즉시 퍼뜨리도록
            if (Main.netMode == NetmodeID.Server)
                NetSendShield(-1);
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
            var absorption = Player.GetModPlayer<AbsorptionSigilPlayer>();
            bool hasAbsorptionSigil = absorption.HasAbsorptionSigil;

            if (suppressRedDamageTextTicks > 0)
                suppressRedDamageTextTicks--;

            // 최대치 재계산:
            // - 싱글/서버에서는 설정값으로 재계산
            // - 멀티 클라에서는 서버에서 Sync된 maxShield를 우선(로컬 설정으로 흔들리지 않게)
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float ratio = GetShieldMaxRatio();
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
            }
            else
            {
                // 클라에서 maxShield가 0이라 UI가 완전히 죽는 걸 방지하는 최소 안전장치
                if (maxShield <= 0)
                {
                    float ratio = MathHelper.Clamp(ModContent.GetInstance<ShieldModConfig>().ShieldMaxRatio, 0.25f, 1f);
                    maxShield = (int)(Player.statLifeMax2 * ratio);
                    if (shield > maxShield) shield = maxShield;
                }
            }

            if (hasAbsorptionSigil)
            {
                // 흡수의 인장 착용 중에는 자연 재생이 차단되므로,
                // 노피격 시간 누적으로 재생 단계가 쌓이지 않게 고정합니다.
                regenTimer = 0;
                timeSinceLastHit = 0;
            }
            else
            {
                regenTimer++;
                timeSinceLastHit++;
            }

            if (hitEffectTimer > 0)
            {
                hitEffectTimer--;
                if (hitEffectTimer <= 0) showHitEffect = false;
            }

            // 쿨다운 중에는 자연 재생/보너스 재생 둘 다 정지
            // ===== 보호막 파괴 페널티 기간 =====
            if (shieldBreakCooldown > 0)
            {
                shieldBreakCooldown--;

                // 흡수 페널티가 겹칠 때 자연 재생은 더 강하게 억제(피드백: 자연 회복 상한 완화)
                if (hasAbsorptionSigil && absorption.IsSiphonPenaltyActive && shield < maxShield)
                {
                    regenTimer = 0;
                    timeSinceLastHit = 0;
                }

                NetMaybeSync();
                return;
            }

            // 흡수의 인장(A): 자연 보호막 재생 완전 차단
            float naturalRegenMultiplier = hasAbsorptionSigil ? 0f : 1f;

            // ===== 기본 자연 재생 로직(원본 유지) =====
            float regenPerSecond = CalculateNaturalRegenPerSecond() * naturalRegenMultiplier;

            // regenPerSecond가 0일 수 있으므로(흡수의 인장 등), 0 나눗셈을 피하기 위해 가드합니다.
            if (regenPerSecond > 0f && shield < maxShield)
            {
                int interval = (int)(60f / regenPerSecond);
                if (interval < 1) interval = 1;
                if (regenTimer % interval == 0)
                    shield++; // 자연 재생 +1
            }

            // ===== Emergency Aegis: 기본 재생 +2/s (파괴 후 3초 체감, 정확히 +2/s 보장) =====
            if (hasAegis && !hasAbsorptionSigil && Player.statLife > 0 && shield < maxShield)
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

            NetMaybeSync();
        }

        private void NetMaybeSync()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            // 10틱마다, 혹은 값 변경 시 바로 전파
            _netSyncTick++;
            bool changed = shield != _netLastShield
                || maxShield != _netLastMax
                || shieldBreakCooldown != _netLastBreakCd
                || timeSinceLastHit != _netLastTimeSinceHit;

            if (changed || _netSyncTick >= 10)
            {
                _netSyncTick = 0;
                _netLastShield = shield;
                _netLastMax = maxShield;
                _netLastBreakCd = shieldBreakCooldown;
                _netLastTimeSinceHit = timeSinceLastHit;
                NetSendShield(-1);
            }
        }

        internal void NetSendShield(int toWho)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)ShieldMod.Msg.SyncPlayerShield);
            packet.Write((byte)Player.whoAmI);
            packet.Write(shield);
            packet.Write(maxShield);
            packet.Write(shieldBreakCooldown);
            packet.Write(timeSinceLastHit);
            packet.Send(toWho, -1);
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            // 보호막 흡수/감산은 ModifyHurt(ShieldPreAbsorbPlayer)에서 선처리합니다.
            // 여기서는 "피격 이후 자연재생 단계"를 위해 마지막 피격 시간만 리셋합니다.
            if (info.Damage > 0)
                timeSinceLastHit = 0;
        }

        public (float naturalRegenPerSecond, float aegisRegenPerSecond) GetShieldRegenPerSecond()
        {
            var absorption = Player.GetModPlayer<AbsorptionSigilPlayer>();
            bool hasAbsorptionSigil = absorption.HasAbsorptionSigil;
            bool hasAegis = Player.GetModPlayer<EmergencyAegisPlayer>().HasAegis;

            if (shieldBreakCooldown > 0 || shield >= maxShield)
                return (0f, 0f);

            float natural = CalculateNaturalRegenPerSecond();
            // 흡수의 인장(A): 자연 보호막 재생 완전 차단
            if (hasAbsorptionSigil)
                natural = 0f;
            float aegis = 0f;

            if (hasAegis && !hasAbsorptionSigil && Player.statLife > 0 && shield < maxShield)
                aegis = 2f;

            return (natural, aegis);
        }

        private float CalculateNaturalRegenPerSecond()
        {
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

            return regenPerSecond;
        }
    }

}
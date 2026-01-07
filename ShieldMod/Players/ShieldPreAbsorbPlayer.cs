using Microsoft.Xna.Framework;      // Color, Vector2, MathHelper
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;              // SoundEngine
using Terraria.ID;                 // SoundID, DustID

namespace ShieldMod
{
    /// <summary>
    /// 사망 판정 이전(ModifyHurt)에서 보호막 선흡수.
    ///
    /// 요구사항:
    /// 1) 보호막이 전부 막아낸 타격(HP 실제 피해 0)에서는 빨간 데미지 숫자(CombatText)가 뜨지 않게
    /// 2) 보호막이 깨지고 HP에 데미지가 들어가면(remaining > 0) 빨간 숫자는 정상적으로 뜨게
    /// 3) 완전 흡수(HP 0)도 바닐라와 동일한 i-frame(무적시간)을 받게 (무적 연장 장신구 효과 포함)
    ///
    /// 구현 방식:
    /// - 완전 흡수는 바닐라 i-frame을 만들기 위해 Hurt가 한 번은 발생해야 하므로 info.Damage=1로 강제
    /// - PostHurt에서 그 1 데미지를 즉시 복구(실질 HP 감소 0) + HP 피격과 동일한 무적시간 보장
    /// - 완전 흡수는 MyModPlayer.SuppressRedDamageText(2)로 혹시 새는 작은 빨간 숫자(1~2)까지 제거
    /// </summary>
    public class ShieldPreAbsorbPlayer : ModPlayer
    {
        // 이펙트/사운드 큐
        private int _queuedAbsorb = 0;
        private bool _queuedStrong = false;
        private int _queuedHitDir = 0;
        private bool _queuedBreak = false;

        // 완전 흡수 플래그 + 1데미지 생존 보정
        private bool _fullyAbsorbedFlag = false;
        private int _lifeBump = 0;
        private int _fullAbsorbFrame = -1;

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            var mp = Player.GetModPlayer<MyModPlayer>();
            if (mp == null || mp.shield <= 0) return;

            modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
            {
                if (info.Damage <= 0 || mp.shield <= 0) return;

                int incoming = info.Damage;
                int beforeShield = mp.shield;
                int absorb = System.Math.Min(mp.shield, incoming);
                if (absorb <= 0) return;

                mp.shield -= absorb;

                int remaining = incoming - absorb;
                info.Damage = remaining;

                mp.timeSinceLastHit = 0;

                // 실드 파괴 쿨다운(요구 스펙 유지: Aegis 착용 시 3초, 아니면 5초)
                if (mp.shield <= 0)
                {
                    // '지금 타격'으로 보호막이 0이 된 순간만 깨짐 연출(중복 방지)
                    if (beforeShield > 0)
                        _queuedBreak = true;

                    bool hasAegis = Player.GetModPlayer<EmergencyAegisPlayer>()?.HasAegis == true;
                    mp.shieldBreakCooldown = hasAegis ? 180 : 300;
                    mp.ResetAegisRegenTokens();
                }

                // ── 보호막 이팩트(overlay) 트리거 ─────────────────────────────
                mp.TriggerShieldOverlay(10);
                // ──────────────────────────────────────────────────────────────

                // 연출 큐
                _queuedAbsorb = absorb;
                _queuedStrong = (remaining <= 0);
                mp.LastShieldHitStrong = _queuedStrong;
                _queuedHitDir = info.HitDirection;

                // =========================
                // 빨간 숫자 표시 규칙
                // =========================
                if (remaining <= 0)
                {
                    // 완전 흡수: HP 데미지는 "없어야" 하므로 빨간 숫자 숨김
                    _fullyAbsorbedFlag = true;
                    _fullAbsorbFrame = (int)Main.GameUpdateCount;

                    // 체력 1일 때 1데미지로 죽는 것 방지
                    if (Player.statLife <= 1)
                    {
                        _lifeBump = 1;
                        Player.statLife += 1;
                    }

                    // 바닐라 i-frame 유도용(반드시 Hurt가 발생해야 함)
                    info.Damage = 1;

                    // 바닐라 연출은 커스텀으로
                    info.SoundDisabled = true;
                    info.DustDisabled = true;

                    // 엔진 내부에서 최소 1(혹은 아주 작은 값) CombatText가 새는 케이스 대비(기존 MyModSystem이 처리)
                    mp.SuppressRedDamageText(2);
                }
                else
                {
                    // 부분 흡수(HP 데미지 있음): 빨간 숫자 정상 출력
                    // → HideCombatText / SuppressRedDamageText 건드리지 않음
                }

                if (Main.netMode == NetmodeID.Server)
                    mp.NetSendShield(-1);
            };
        }

        public override void PostUpdate()
        {
            // 내 클라이언트에서만 재생
            if (_queuedAbsorb <= 0 || Main.dedServ || Player.whoAmI != Main.myPlayer) return;

            PlayShieldImpactSfx(_queuedStrong);
            var cfg = ModContent.GetInstance<ShieldModConfig>();
            // Hit VFX (Arc):
            // - Strong: always Arc
            // - Impact-only: Arc only when the last absorbed hit is "strong"
            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Strong)
                SpawnShieldDeflectArcDust(_queuedAbsorb, _queuedHitDir, strongStyle: true, fullAbsorb: _queuedStrong);
            else if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.ImpactOnly && _queuedStrong)
                SpawnShieldDeflectArcDust(_queuedAbsorb, _queuedHitDir, strongStyle: false, fullAbsorb: true);
if (_queuedBreak)
                PlayShieldBreakSfx();

            if (ModContent.GetInstance<ShieldModConfig>().ShowShieldText && !Main.dedServ)
            {
                Color hitColor = ModContent.GetInstance<ShieldModConfig>().ShieldHitColor;
                CombatText.NewText(Player.Hitbox, hitColor, "-" + _queuedAbsorb);
            }

            _queuedAbsorb = 0;
            _queuedStrong = false;
            _queuedBreak = false;
        }

        private void PlayShieldImpactSfx(bool strong)
        {
            // 요구: Item93 제거 → Item30 하나만 사용
            SoundEngine.PlaySound(SoundID.Item30 with { Pitch = strong ? -0.1f : 0.05f, Volume = 1f }, Player.Center);
        }

        private 
        void SpawnShieldDeflectArcDust(int absorb, int hitDir, bool strongStyle, bool fullAbsorb)
        {
            // Directional Arc on the incoming side (eye-catching + not electric).
            int side = hitDir;
            if (side == 0) side = Player.direction;

            float baseAngle = (side == 1) ? 0f : MathHelper.Pi; // right / left
            float arcSpan = MathHelper.ToRadians(fullAbsorb ? 140f : 120f);

            int count = (strongStyle ? 14 : 10) + absorb / 80;
            if (fullAbsorb) count += 3;
            if (count > 26) count = 26;

            Color c = ModContent.GetInstance<ShieldModConfig>().ShieldHitColor;

            float radiusMin = strongStyle ? 18f : 14f;
            float radiusMax = strongStyle ? 28f : 22f;

            for (int i = 0; i < count; i++)
            {
                float t = (count <= 1) ? 0.5f : i / (float)(count - 1);
                float ang = baseAngle + MathHelper.Lerp(-arcSpan * 0.5f, arcSpan * 0.5f, t) + Main.rand.NextFloat(-0.08f, 0.08f);
                Vector2 dir = ang.ToRotationVector2();

                float radius = Main.rand.NextFloat(radiusMin, radiusMax);
                Vector2 pos = Player.Center + dir * radius;

                float spd = strongStyle ? Main.rand.NextFloat(2.6f, 4.1f) : Main.rand.NextFloat(2.0f, 3.2f);
                if (fullAbsorb) spd *= 1.1f;

                int dustId = Dust.NewDust(pos, 0, 0, DustID.MagicMirror, 0f, 0f, 150, c, strongStyle ? 1.2f : 1.05f);
                var d = Main.dust[dustId];
                d.noGravity = true;
                d.velocity = dir * spd + Main.rand.NextVector2Circular(0.6f, 0.6f);

                // pull slightly along the arc to look like a curved deflect
                d.position -= dir * Main.rand.NextFloat(2f, 6f);
            }
        }

        private void PlayShieldBreakSfx()
        {
            // 보호막 파괴 SFX(바닐라, 항아리 느낌 방지): Item4 + Item29(작게)
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.95f, Pitch = 0.05f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.22f, Pitch = 0.18f }, Player.Center);
        }


        public override void PostHurt(Player.HurtInfo info)
        {
            // 완전 흡수 시, 강제한 1데미지를 즉시 되돌려서 "실제 체력은 안 깎이게" 처리
            if (_fullyAbsorbedFlag && info.Damage > 0
                && Main.GameUpdateCount - _fullAbsorbFrame <= 1)
            {
                // 1) HP 복구
                Player.statLife = System.Math.Min(Player.statLifeMax2, Player.statLife + info.Damage);

                // 2) 체력 1 보호 bump 원복
                if (_lifeBump > 0)
                {
                    Player.statLife = System.Math.Max(1, Player.statLife - _lifeBump);
                    _lifeBump = 0;
                }

                // 3) 무적시간을 "HP 피격과 동일하게" 보장(장신구 보너스 포함)
                EnforceShieldIFrames();
            }

            _fullyAbsorbedFlag = false;
            _fullAbsorbFrame = -1;
        }

        private void EnforceShieldIFrames()
        {
            // 기본 HP 피격과 동일한 i-frame을 최소한으로 부여
            // - 바닐라: 40프레임, longInvince(Cross Necklace/Star Veil 등) 시 80프레임
            int minImmune = Player.longInvince ? 80 : 40;

            if (Player.immuneTime < minImmune)
                Player.immuneTime = minImmune;

            Player.immune = true;

            int[] cooldowns = Player.hurtCooldowns;
            for (int i = 0; i < cooldowns.Length; i++)
            {
                if (cooldowns[i] < minImmune)
                    cooldowns[i] = minImmune;
            }
        }
    }
}

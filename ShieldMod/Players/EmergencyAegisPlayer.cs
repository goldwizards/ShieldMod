using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ShieldMod.Buffs;

namespace ShieldMod
{
    // 싱글 전용 설계 (멀티 비고려)
    public class EmergencyAegisPlayer : ModPlayer
    {
        public bool HasAegis;

        // === 긴급 3초 회복(HoT) ===
        private int _aegisHotTicks;   // 남은 틱(최대 180)
        private int _aegisFrac;       // 분수 누적(분모=180)
        private int _aegisRateNum;    // 180틱 동안 나눠줄 총량 = 발동 시 '부족량'
        private int _aegisBudget;     // 총 회복 예산 = 발동 시 '부족량'(피격 시 감소)

        // 처치 회복 ICD(잡몹만 2초)
        private int _killIcdTicks;

        // 실드 감소 감지(HP 무관)
        private int _prevShield;

        // 합산 텍스트(보스 25% + 장신구 +20 묶음)
        private int _healTextSum;
        private int _healTextTick = -1;

        // === 멀티 동기화 ===
        private int _netHotTicks;
        private int _netHotFrac;
        private int _netHotRateNum;
        private int _netHotBudget;
        private int _netCooldownTime;
        private int _netSyncTimer;

        public override void ResetEffects() => HasAegis = false;

        public override void OnEnterWorld()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                RequestAegisHotSync();
        }

        public override void OnRespawn()
        {
            // 300초 쿨타임은 사망 시 초기화
            Player.ClearBuff(ModContent.BuffType<EmergencyAegisCooldown>());

            // HoT 상태 초기화
            _aegisHotTicks = 0;
            _aegisFrac = 0;
            _aegisBudget = 0;
            _aegisRateNum = 0;

            if (Main.netMode == NetmodeID.MultiplayerClient)
                RequestAegisHotSync();
        }

        public override void PostUpdate()
        {
            var mp = Player.GetModPlayer<MyModPlayer>();
            if (mp.maxShield <= 0) { _prevShield = 0; return; }

            // 멀티에서는 서버 권위 값이 주기적으로 Sync됩니다.
            // 긴급 HoT/처치회복 로직은 클라에서도 "표시" 상 문제를 만들지 않지만,
            // 중복 실행을 피하기 위해 최소한의 가드만 둡니다(서버에서만 쿨/예산을 확정).
            bool serverAuth = Main.netMode != NetmodeID.MultiplayerClient;

            // 이번 틱 실드 감소량(HP와 무관)
            int shieldDropThisTick = 0;
            if (_prevShield > mp.shield)
                shieldDropThisTick = _prevShield - mp.shield;

            // ----- 긴급 발동: 체력 ≤35% & 쿨없음 -----
            if (serverAuth
                && HasAegis
                && Player.statLife > 0
                && Player.statLife <= Player.statLifeMax2 * 0.35f
                && mp.shield <= 0 // ✅ 추가 조건: 보호막이 0 이하일 때만
                && _aegisHotTicks <= 0
                && !Player.HasBuff(ModContent.BuffType<EmergencyAegisCooldown>()))
            {
                _aegisHotTicks = 180; // 3초
                _aegisFrac = 0;

                // 발동 시 '부족량'만 3초에 걸쳐 회복
                int missingAtStart = mp.maxShield - mp.shield;
                _aegisRateNum = missingAtStart; // 180틱 동안 나눠줄 총량
                _aegisBudget  = missingAtStart; // 총 회복 예산
                if (_aegisBudget <= 0) _aegisHotTicks = 0;

                // ✅ "회복할 게 없으면 쿨만 소비" 방지: 예산이 있을 때만 쿨 부여
                if (_aegisHotTicks > 0 && _aegisBudget > 0 && serverAuth)
                {
                    // 300초 쿨타임을 디버프 아이콘으로 표시 (사망 시 초기화는 OnRespawn에서 처리)
                    Player.AddBuff(ModContent.BuffType<EmergencyAegisCooldown>(), 300 * 60);
                }
            }

            // 회복 중 피격: 예산만 줄임(정지 없음) → 끝에 풀까지 못 참
            if (_aegisHotTicks > 0 && shieldDropThisTick > 0)
            {
                int budgetAfterHit = (_aegisBudget > shieldDropThisTick)
                    ? (_aegisBudget - shieldDropThisTick) : 0;
                if (serverAuth || Main.netMode == NetmodeID.SinglePlayer)
                    _aegisBudget = budgetAfterHit;
                else
                    _aegisBudget = budgetAfterHit; // 표시/예측용 소비(서버가 주기적으로 재동기화)
            }

            // ----- 긴급 회복 진행(정지 없음, HoT 텍스트 표시 안 함) -----
            if (_aegisHotTicks > 0)
            {
                _aegisFrac += _aegisRateNum; // 분모=180

                while (_aegisFrac >= 180 && _aegisHotTicks > 0 && _aegisBudget > 0 && mp.shield < mp.maxShield)
                {
                    _aegisFrac -= 180;
                    _aegisBudget--;  // 총량 소모
                    if (serverAuth || Main.netMode == NetmodeID.SinglePlayer)
                    {
                        mp.shield++;     // ← 텍스트 호출 없음(요청 사항)
                    }
                    else
                    {
                        // 표시/예측용으로만 증가(서버 SyncPlayerShield가 주기적으로 덮어씀)
                        mp.shield++;
                    }
                }

                _aegisHotTicks--;
                if (_aegisBudget <= 0 || mp.shield >= mp.maxShield)
                {
                    _aegisHotTicks = 0;
                    _aegisFrac = 0;
                    _aegisRateNum = 0;
                    if (_aegisBudget < 0) _aegisBudget = 0;
                }
            }

            // ----- 처치 회복 ICD -----
            if (_killIcdTicks > 0) _killIcdTicks--;

            _prevShield = mp.shield; // 다음 틱 비교용
            NetMaybeSyncAegisHot();
        }

        // === 잡/보스 처치 힐 진입점(GlobalNPC에서 호출) ===
        public void TryOnKillHeal(bool isBossKill)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            var mp = Player.GetModPlayer<MyModPlayer>();
            if (!HasAegis || Player.statLife <= 0 || mp.maxShield <= 0) return;

            if (isBossKill)
            {
                ApplyShieldHeal(20); // 보스는 ICD 무시
            }
            else if (_killIcdTicks <= 0)
            {
                ApplyShieldHeal(20);
                _killIcdTicks = 120; // 2s
            }
        }

        private void ApplyShieldHeal(int amount)
        {
            var mp = Player.GetModPlayer<MyModPlayer>();
            int before = mp.shield;
            int after = before + amount;
            if (after > mp.maxShield) after = mp.maxShield;
            int gained = after - before;
            if (gained <= 0) return;

            mp.shield = after;
            QueueShieldHealText(gained);
        }

        // === 합산 텍스트(보스 25% & 처치 +20 묶어서 '다음 프레임' 1번만 출력) ===
        public void QueueShieldHealText(int amount)
        {
            int tick = (int)Main.GameUpdateCount;
            if (_healTextTick != tick)
            {
                _healTextTick = tick;
                _healTextSum = 0;
            }
            _healTextSum += amount;
        }

        public void FlushShieldHealTextIfAny()
        {
            if (_healTextSum > 0 && (int)Main.GameUpdateCount > _healTextTick)
            {
                if (Main.netMode == NetmodeID.Server)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)ShieldMod.Msg.ShieldHealText);
                    packet.Write((byte)Player.whoAmI);
                    packet.Write(_healTextSum);
                    packet.Send(-1, -1);
                }
                else
                {
                    CombatText.NewText(Player.getRect(), Color.Cyan, $"+{_healTextSum}", true);
                }
                _healTextSum = 0;
            }
        }

        // 동적 툴팁 등에서 조회 가능
        public int KillIcdTicks => _killIcdTicks;

        private int GetCooldownTime()
        {
            int buffType = ModContent.BuffType<EmergencyAegisCooldown>();
            int idx = Player.FindBuffIndex(buffType);
            return idx >= 0 ? Player.buffTime[idx] : 0;
        }

        private void RequestAegisHotSync()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)ShieldMod.Msg.RequestAegisHot);
            packet.Write((byte)Player.whoAmI);
            packet.Send();
        }

        private void NetMaybeSyncAegisHot()
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            _netSyncTimer++;
            int cooldown = GetCooldownTime();

            bool changed = _aegisHotTicks != _netHotTicks
                || _aegisFrac != _netHotFrac
                || _aegisRateNum != _netHotRateNum
                || _aegisBudget != _netHotBudget
                || cooldown != _netCooldownTime;

            if (!changed && _netSyncTimer < 10)
                return;

            _netSyncTimer = 0;
            _netHotTicks = _aegisHotTicks;
            _netHotFrac = _aegisFrac;
            _netHotRateNum = _aegisRateNum;
            _netHotBudget = _aegisBudget;
            _netCooldownTime = cooldown;

            NetSendAegisHot(-1);
        }

        internal void NetSendAegisHot(int toWho)
        {
            if (Main.netMode != NetmodeID.Server)
                return;

            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)ShieldMod.Msg.SyncAegisHot);
            packet.Write((byte)Player.whoAmI);
            packet.Write(_aegisHotTicks);
            packet.Write(_aegisFrac);
            packet.Write(_aegisRateNum);
            packet.Write(_aegisBudget);
            packet.Write(GetCooldownTime());
            packet.Send(toWho, -1);
        }

        internal void NetReceiveAegisHot(int hotTicks, int hotFrac, int hotRateNum, int hotBudget, int cooldownTime)
        {
            _aegisHotTicks = hotTicks;
            _aegisFrac = hotFrac;
            _aegisRateNum = hotRateNum;
            _aegisBudget = hotBudget;

            int buffType = ModContent.BuffType<EmergencyAegisCooldown>();
            int idx = Player.FindBuffIndex(buffType);
            if (cooldownTime > 0)
            {
                if (idx < 0)
                {
                    Player.AddBuff(buffType, cooldownTime);
                }
                else
                {
                    Player.buffTime[idx] = cooldownTime;
                }
            }
            else if (idx >= 0)
            {
                Player.DelBuff(idx);
            }
        }
    }
}

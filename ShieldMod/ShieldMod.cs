using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ShieldMod
{
    public class ShieldMod : Mod
    {
        internal enum Msg : byte
        {
            SyncPlayerShield,
            RequestShieldSync,
            ShieldHealText,
            SyncAegisHot,
            RequestAegisHot,

            // Multiplayer fix: player hurt is processed client-side.
            // Client reports how much shield was consumed, server applies and broadcasts.
            ReportShieldAbsorb
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            Msg msg = (Msg)reader.ReadByte();
            switch (msg)
            {
                case Msg.SyncPlayerShield:
                {
                    // Never trust client->server SyncPlayerShield (cheat/invincibility vector).
                    if (Main.netMode == NetmodeID.Server)
                        break;

                    byte playerId = reader.ReadByte();
                    int shield = reader.ReadInt32();
                    int maxShield = reader.ReadInt32();
                    int breakCd = reader.ReadInt32();
                    int timeSinceHit = reader.ReadInt32();

                    if (playerId < Main.maxPlayers)
                    {
                        Player p = Main.player[playerId];
                        if (p != null)
                        {
                            var mp = p.GetModPlayer<MyModPlayer>();
                            // Always sync these (authoritative / prevents config wobble in MP)
                            mp.maxShield = maxShield;
                            mp.shieldBreakCooldown = breakCd;
                            mp.timeSinceLastHit = timeSinceHit;

                            // UI jitter fix (MP): during regeneration, packets can arrive slightly late.
                            // If we always overwrite, the local number can "dip" then rise again.
                            // Rule: allow decreases only right after being hit / during break penalty,
                            // otherwise ignore small backward snaps.
                            bool allowDecrease = (timeSinceHit <= 1) || (breakCd > 0);
                            if (playerId == Main.myPlayer && shield < mp.shield && !allowDecrease)
                            {
                                // If it's a large correction (true desync), accept it.
                                if (mp.shield - shield >= 15)
                                    mp.shield = shield;
                                // else: keep current local value (prevents 1~2 point dips)
                            }
                            else
                            {
                                mp.shield = shield;
                            }

                            if (mp.shield > mp.maxShield)
                                mp.shield = mp.maxShield;
                        }
                    }
                    break;
                }

                case Msg.ReportShieldAbsorb:
                {
                    if (Main.netMode != NetmodeID.Server)
                        break;

                    byte playerId = reader.ReadByte();
                    int absorbed = reader.ReadInt32();
                    if (playerId >= Main.maxPlayers)
                        break;

                    // Only allow a client to report its OWN shield consumption.
                    if (playerId != whoAmI)
                        break;

                    if (absorbed <= 0)
                        break;

                    Player p = Main.player[playerId];
                    if (p == null || !p.active)
                        break;

                    var mp = p.GetModPlayer<MyModPlayer>();
                    if (mp == null)
                        break;

                    // Clamp to prevent weird values; this message can only DECREASE shield.
                    if (absorbed > mp.maxShield)
                        absorbed = mp.maxShield;

                    int before = mp.shield;
                    mp.shield -= absorbed;
                    if (mp.shield < 0) mp.shield = 0;

                    mp.timeSinceLastHit = 0;

                    if (before > 0 && mp.shield <= 0)
                    {
                        bool hasAegis = p.GetModPlayer<EmergencyAegisPlayer>()?.HasAegis == true;
                        mp.shieldBreakCooldown = hasAegis ? 180 : 300;
                        mp.ResetAegisRegenTokens();
                    }

                    // Immediately broadcast corrected state to everyone (including the owner).
                    mp.NetSendShield(-1);
                    break;
                }

                case Msg.RequestShieldSync:
                {
                    if (Main.netMode != NetmodeID.Server)
                        break;

                    // 접속 직후 UI가 0으로 보이는 시간을 줄이기 위해,
                    // 요청한 클라에게 현재 접속자 전원의 실드 상태를 즉시 브로드캐스트합니다.
                    int toWho = whoAmI;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player p = Main.player[i];
                        if (p != null && p.active)
                            p.GetModPlayer<MyModPlayer>().NetSendShield(toWho);
                    }
                    break;
                }

                case Msg.ShieldHealText:
                {
                    if (Main.netMode == NetmodeID.Server)
                        break;

                    byte playerId = reader.ReadByte();
                    int healAmount = reader.ReadInt32();
                    if (healAmount <= 0)
                        break;

                    if (playerId < Main.maxPlayers)
                    {
                        Player p = Main.player[playerId];
                        if (p != null && p.active)
                        {
                            Color regenColor = ModContent.GetInstance<ShieldModConfig>().ShieldRegenColor;
                            CombatText.NewText(p.getRect(), regenColor, $"+{healAmount}", true);
                        }
                    }
                    break;
                }

                case Msg.SyncAegisHot:
                {
                    // Never trust client->server Aegis sync.
                    if (Main.netMode == NetmodeID.Server)
                        break;

                    byte playerId = reader.ReadByte();
                    int hotTicks = reader.ReadInt32();
                    int hotFrac = reader.ReadInt32();
                    int hotRateNum = reader.ReadInt32();
                    int hotBudget = reader.ReadInt32();
                    int cooldownTime = reader.ReadInt32();

                    if (playerId < Main.maxPlayers)
                    {
                        Player p = Main.player[playerId];
                        if (p != null && p.active)
                            p.GetModPlayer<EmergencyAegisPlayer>()?.NetReceiveAegisHot(hotTicks, hotFrac, hotRateNum, hotBudget, cooldownTime);
                    }
                    break;
                }

                case Msg.RequestAegisHot:
                {
                    if (Main.netMode != NetmodeID.Server)
                        break;

                    byte playerId = reader.ReadByte();
                    if (playerId < Main.maxPlayers)
                    {
                        Player p = Main.player[playerId];
                        if (p != null && p.active)
                            p.GetModPlayer<EmergencyAegisPlayer>()?.NetSendAegisHot(whoAmI);
                    }
                    break;
                }
            }
        }
    }
}

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
            RequestAegisHot
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            Msg msg = (Msg)reader.ReadByte();
            switch (msg)
            {
                case Msg.SyncPlayerShield:
                {
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
                            mp.shield = shield;
                            mp.maxShield = maxShield;
                            mp.shieldBreakCooldown = breakCd;
                            mp.timeSinceLastHit = timeSinceHit;
                        }
                    }
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

using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using ShieldMod.Players;
using ShieldMod;

namespace ShieldMod.Globals
{
    public class MyGlobalProjectile : GlobalProjectile
    {
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
        {
            var modPlayer = target.GetModPlayer<MyModPlayer>();
            int damage = (int)modifiers.FinalDamage.Base;
            if (damage <= 0) return;

            if (modPlayer.shield > 0)
            {
                int absorbed = System.Math.Min(modPlayer.shield, damage);
                int remaining = damage - absorbed;
                modPlayer.shield -= absorbed;

                // ✅ 조건에 따라 파란 숫자 출력
                if (!Main.dedServ && absorbed > 0 && ModContent.GetInstance<ShieldModConfig>().ShowShieldText)
                    CombatText.NewText(target.Hitbox, Color.DodgerBlue, "-" + absorbed);

                modPlayer.timeSinceLastHit = 0;
                if (modPlayer.shield <= 0)
                    modPlayer.shieldBreakCooldown = 180;

                modifiers.FinalDamage.Base = remaining;
            }
        }
    }
}

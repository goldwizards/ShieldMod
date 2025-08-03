using Terraria;
using Terraria.ModLoader;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.DataStructures;
using Terraria.Audio;
using System.Collections.Generic;

namespace ShieldMod.Players
{
    public class MyModPlayer : ModPlayer
    {
        public int shield;
        public int maxShield;
        public int shieldBreakCooldown;
        public int timeSinceLastHit;
        private int regenTimer;

        public bool showHitEffect;
        private int hitEffectTimer;

        public float ShieldRegenBonus;
        public float DamageReduction;

        public int CurrentShield => shield;
        public int MaxShield => maxShield;

        public override void OnEnterWorld()
        {
            maxShield = Player.statLifeMax2;
            shield = maxShield;
        }

        public override void OnRespawn()
        {
            maxShield = Player.statLifeMax2;
            shield = maxShield;
            shieldBreakCooldown = 0;
        }

        public override void ResetEffects()
        {
            if (shield > maxShield)
                shield = maxShield;

            ShieldRegenBonus = 0f;
            DamageReduction = 0f;
        }

        public override void PostUpdate()
        {
            // Recalculate shield capacity after all effects, such as buffs,
            // modify the player's maximum life. Keep the shield ratio
            // consistent when the maximum changes.
            int newMax = Player.statLifeMax2;
            if (newMax != maxShield)
            {
                float ratio = maxShield > 0 ? (float)shield / maxShield : 1f;
                maxShield = newMax;
                shield = (int)(maxShield * ratio);
                if (shield > maxShield)
                    shield = maxShield;
            }
            else
            {
                maxShield = newMax;
                if (shield > maxShield)
                    shield = maxShield;
            }

            regenTimer++;
            timeSinceLastHit++;

            if (hitEffectTimer > 0)
            {
                hitEffectTimer--;
                if (hitEffectTimer <= 0)
                    showHitEffect = false;
            }

            if (shieldBreakCooldown > 0)
            {
                shieldBreakCooldown--;
                return;
            }

            float regenPerSecond = 1f;
            if (timeSinceLastHit >= 300) regenPerSecond = 2f;
            if (timeSinceLastHit >= 600) regenPerSecond = 3f;
            if (timeSinceLastHit >= 900) regenPerSecond = 5f;
            if (timeSinceLastHit >= 1200) regenPerSecond = 8f;
            if (timeSinceLastHit >= 1800) regenPerSecond = 12f;
            if (timeSinceLastHit >= 2400) regenPerSecond = 20f;

            regenPerSecond *= 1f + ShieldRegenBonus;

            if (Main.npc.Any(n => n.active && n.boss))
            {
                float bossLimit = 5f * (1f + ShieldRegenBonus);
                if (regenPerSecond > bossLimit)
                    regenPerSecond = bossLimit;
            }

            int interval = (int)(60f / regenPerSecond);
            if (regenPerSecond > 0f && regenTimer % interval == 0 && shield < maxShield)
            {
                shield++;
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (shield > 0)
            {
                int incomingDamage = (int)modifiers.FinalDamage.Base;

                if (incomingDamage <= shield)
                {
                    modifiers.DisableSound(); // 일반 피격 사운드 제거
                }
            }
        }

        public override void PostHurt(Player.HurtInfo info)
        {
            if (shield > 0)
            {
                int absorbed = System.Math.Min(shield, info.Damage);
                shield -= absorbed;

                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, Player.Center); // 커스텀 사운드

                showHitEffect = true;
                hitEffectTimer = 10;

                if (absorbed >= info.Damage)
                    Player.statLife += absorbed;

                if (absorbed > 0 && ModContent.GetInstance<ShieldModConfig>().ShowShieldText)
                    CombatText.NewText(Player.Hitbox, Color.DodgerBlue, "-" + absorbed);

                timeSinceLastHit = 0;

                if (shield <= 0)
                    shieldBreakCooldown = 300;
            }
        }
    }
}

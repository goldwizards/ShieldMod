using Terraria;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace ShieldMod.Layers
{
    /// <summary>
    /// Minimal "bubble shield" overlay around the player when shield > 0.
    /// Uses a single texture (Assets/ShieldBubble.png) and color from config.
    /// </summary>
    public class ShieldBubbleLayer : PlayerDrawLayer
    {
        private static Asset<Texture2D> _bubbleTex;

        // tModLoader 1.4.4.x does not expose a "Body" layer.
        // Anchor after an always-present layer (ArmOverItem exists in 1.4.4.x).
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            if (!p.active || p.dead) return false;

            var cfg = ModContent.GetInstance<ShieldModConfig>();
            if (!cfg.EnableBubbleShield) return false;

            // Subtle hit style: do not show the always-on bubble overlay
            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Subtle) return false;
            // Hit effect style Off: also hide the always-on bubble overlay
            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Off) return false;

            var mp = p.GetModPlayer<MyModPlayer>();
            return mp != null && mp.shield > 0;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player p = drawInfo.drawPlayer;
            var cfg = ModContent.GetInstance<ShieldModConfig>();
            var mp = p.GetModPlayer<MyModPlayer>();
            if (mp == null || mp.shield <= 0) return;

            _bubbleTex ??= ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldBubble", AssetRequestMode.ImmediateLoad);
            Texture2D tex = _bubbleTex.Value;
            if (tex == null) return;

            // Base opacity
            float alpha = MathHelper.Clamp(cfg.BubbleShieldOpacity, 0f, 1f);

            // Extra flash when hit (re-use existing hit overlay timer)
            if (mp.showHitEffect && mp.HitEffectTimer > 0)
            {
                float t = MathHelper.Clamp(mp.HitEffectTimer / 12f, 0f, 1f);
                alpha = MathHelper.Clamp(alpha + 0.35f * t, 0f, 1f);
            }

            // Detect low shield (<= 25%)
            bool isLowShield = cfg.BubbleShieldPulseOnLow
                               && mp.maxShield > 0
                               && mp.shield <= (int)(mp.maxShield * 0.25f);

            // Low-shield pulse (stronger): alpha + scale pulse
            float lowPhase = 0f; // 0..1
            float lowGlowBoost = 0f; // 0..1 for double-draw boost
            if (isLowShield)
            {
                // 0..1 phase
                lowPhase = 0.5f + 0.5f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 8f);

                // Stronger alpha pulse (0..0.18)
                alpha = MathHelper.Clamp(alpha + 0.2f * lowPhase, 0f, 1f);

                // Use same phase to slightly boost glow pass if enabled
                lowGlowBoost = lowPhase;
            }

            // Color uses the existing hit color option for consistency
            Color color = cfg.ShieldHitColor * alpha;

            // Center around the player (slightly above feet)
            Vector2 worldCenter = p.MountedCenter + new Vector2(0f, -6f);
            Vector2 screenPos = worldCenter - Main.screenPosition;

            // Scale bubble to roughly fit the player sprite
            float target = System.Math.Max(p.width, p.height) * 1.50f;
            float scale = target / tex.Width;

            // Scale pulse when low shield (max +6%)
            if (isLowShield)
            {
                scale *= 1f + 0.03f * lowPhase;
            }

            // Small bob so it feels "alive"
            if (cfg.BubbleShieldBob)
            {
                screenPos.Y += (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 3f) * 1.5f;
            }

            DrawData dd = new DrawData(
                tex,
                screenPos,
                null,
                color,
                0f,
                tex.Size() * 0.5f,
                scale,
                SpriteEffects.None,
                0
            );

            // Additive-like feel by drawing slightly brighter once more (still alpha-safe)
            if (cfg.BubbleShieldDoubleDraw)
            {
                DrawData dd2 = dd;

                // Base glow strength
                float glowAlpha = alpha * 0.35f;

                // If low shield, boost glow a bit (up to +0.15 * alpha)
                if (isLowShield)
                {
                    glowAlpha += alpha * (0.15f * lowGlowBoost);
                }

                dd2.color = cfg.ShieldHitColor * MathHelper.Clamp(glowAlpha, 0f, 1f);
                drawInfo.DrawDataCache.Add(dd2);
            }

            drawInfo.DrawDataCache.Add(dd);
        }
    }
}

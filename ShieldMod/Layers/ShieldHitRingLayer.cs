using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;

namespace ShieldMod.Layers
{
    /// <summary>
    /// Hit ripple ring effect using the existing bubble texture.
    /// - Normal: ring only
    /// - Strong: ring + (dust already handled elsewhere)
    /// - ImpactOnly: only show ring when the last absorbed hit is flagged as "strong"
    /// </summary>
    public class ShieldHitRingLayer : PlayerDrawLayer
    {
        private static Asset<Texture2D> _ringTex;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var p = drawInfo.drawPlayer;
            if (p == null || !p.active || p.dead) return false;

            var cfg = ModContent.GetInstance<ShieldModConfig>();
            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Off) return false;
            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Subtle) return false; // Subtle uses rune image only

            var mp = p.GetModPlayer<MyModPlayer>();
            if (mp == null || mp.HitEffectTimer <= 0) return false;

            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.ImpactOnly)
                return false; // Impact-only uses Arc-only (no ring)

            return true;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var p = drawInfo.drawPlayer;
            var mp = p.GetModPlayer<MyModPlayer>();
            var cfg = ModContent.GetInstance<ShieldModConfig>();
            if (mp == null || mp.HitEffectTimer <= 0) return;

            if (cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Subtle ||
                cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Off ||
                cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.ImpactOnly)
                return;

            _ringTex ??= ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldBubble", AssetRequestMode.ImmediateLoad);
            if (_ringTex == null || !_ringTex.IsLoaded) return;

            int timer = mp.HitEffectTimer;

            // Style presets (keep options clean):
            // - Normal: Outline flash (single ring, quick + subtle expansion)
            // - Strong: Double shockwave ring (two rings)
            bool strongStyle = cfg.HitEffectStyle == ShieldModConfig.ShieldHitVfxStyle.Strong;

            float alpha1, scale1;
            float alpha2 = 0f, scale2 = 0f;
            bool drawSecond = false;

            if (!strongStyle)
            {
                // Outline flash
                float t = MathHelper.Clamp(timer / 8f, 0f, 1f);
                alpha1 = 0.70f * t;
                scale1 = 0.98f + 0.06f * (1f - t);
            }
            else
            {
                // Double shockwave
                float t = MathHelper.Clamp(timer / 12f, 0f, 1f);
                alpha1 = 0.55f * t;
                scale1 = 0.80f + 0.20f * (1f - t);

                float t2 = MathHelper.Clamp((timer - 3) / 12f, 0f, 1f);
                alpha2 = 0.30f * t2;
                scale2 = 1.00f + 0.25f * (1f - t2);
                drawSecond = true;
            }

            Vector2 pos = p.Center - Main.screenPosition + new Vector2(0f, 4f);
            Texture2D tex = _ringTex.Value;
            Vector2 origin = tex.Size() * 0.5f;

            Color c = ModContent.GetInstance<ShieldModConfig>().ShieldHitColor;

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                pos,
                null,
                c * alpha1,
                0f,
                origin,
                scale1,
                SpriteEffects.None,
                0
            ));
        
            if (drawSecond)
            {
                drawInfo.DrawDataCache.Add(new DrawData(
                    tex,
                    pos,
                    null,
                    c * alpha2,
                    0f,
                    origin,
                    scale2,
                    SpriteEffects.None,
                    0
                ));
            }
}
    }
}
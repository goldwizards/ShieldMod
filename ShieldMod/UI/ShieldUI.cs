using System;
using Terraria;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using ShieldMod;
using ShieldMod.Players;

namespace ShieldMod.UI
{
    public class ShieldUI : UIState
    {
        public override void Draw(SpriteBatch spriteBatch)
        {
            var modPlayer = Main.LocalPlayer.GetModPlayer<MyModPlayer>();
            var config = ModContent.GetInstance<ShieldModConfig>();

            if (modPlayer.maxShield <= 0)
                return;

            if (config.ShieldUIStyle == ShieldModConfig.ShieldUIDisplayStyle.Icon)
                DrawShieldIcons(spriteBatch, modPlayer, config);
            else
                DrawShieldBar(spriteBatch, modPlayer);
        }

        private void DrawShieldBar(SpriteBatch spriteBatch, MyModPlayer modPlayer)
        {
            int shield = modPlayer.shield;
            int maxShield = modPlayer.maxShield;
            float percent = MathHelper.Clamp((float)shield / maxShield, 0f, 1f);

            Vector2 position = new Vector2(Main.screenWidth - 350, 120);
            int barWidth = 26;
            int barHeight = 200;
            int fillHeight = (int)(barHeight * percent);

            spriteBatch.Draw(MyModSystem.PixelTexture,
                new Rectangle((int)position.X, (int)position.Y, barWidth, barHeight),
                Color.Black * 0.5f);

            spriteBatch.Draw(MyModSystem.PixelTexture,
                new Rectangle((int)position.X, (int)(position.Y + (barHeight - fillHeight)), barWidth, fillHeight),
                Color.DodgerBlue);

            Texture2D frameTex = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldFrame").Value;
            float frameScale = 1f;
            Vector2 frameOffset = new Vector2((barWidth - frameTex.Width * frameScale) / 2f, -40f);
            spriteBatch.Draw(frameTex, position + frameOffset, null, Color.White, 0f, Vector2.Zero, frameScale, SpriteEffects.None, 0f);

            string text = $"{shield} / {maxShield}";
            Vector2 textPos = new Vector2(position.X + (barWidth / 2f), position.Y + barHeight + 18f);
            Utils.DrawBorderString(spriteBatch, text, textPos, Color.White, 1f, 0.5f, 0.5f);
        }

        private void DrawShieldIcons(SpriteBatch spriteBatch, MyModPlayer modPlayer, ShieldModConfig config)
        {
            Texture2D icon = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldIcon").Value;

            int iconCount = 5;
            float shieldPerIcon = modPlayer.maxShield / (float)iconCount;
            float shieldValue = modPlayer.shield;

            Vector2 startPos = new Vector2(Main.screenWidth - 350, 120);
            int spacing = 36;

            int activeIndex = Math.Clamp((int)(shieldValue / shieldPerIcon), 0, iconCount - 1);

            for (int i = 0; i < iconCount; i++)
            {
                float min = i * shieldPerIcon;
                float max = (i + 1) * shieldPerIcon;

                float alpha = 0f;
                if (shieldValue >= max)
                    alpha = 1f;
                else if (shieldValue > min)
                    alpha = (shieldValue - min) / shieldPerIcon;

                alpha = MathHelper.Clamp(alpha, 0.1f, 1f);

                float scale = 1f;
                if (config.UseShieldPulseEffect && i == activeIndex)
                    scale = 1f + 0.1f * (float)Math.Sin(Main.GameUpdateCount / 10f);

                Vector2 pos = new Vector2(startPos.X, startPos.Y + i * spacing);
                spriteBatch.Draw(icon, pos, null, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }

            string text = $"{modPlayer.shield} / {modPlayer.maxShield}";
            Vector2 textPos = new Vector2(startPos.X + 16, startPos.Y + iconCount * spacing + 8);
            Utils.DrawBorderString(spriteBatch, text, textPos, Color.White, 1f, 0.5f, 0.5f);
        }
    }
}

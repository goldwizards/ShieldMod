using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

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
                DrawShieldBar(spriteBatch, modPlayer, config);
        }

        // =========================
        // BAR UI (Vertical Bar)
        // =========================
        private void DrawShieldBar(SpriteBatch spriteBatch, MyModPlayer modPlayer, ShieldModConfig config)
        {
            int shield = modPlayer.shield;
            int maxShield = modPlayer.maxShield;
            float percent = MathHelper.Clamp((float)shield / maxShield, 0f, 1f);

            // 위치/크기(필요 시 여기만 조절)
            Vector2 position = new Vector2(Main.screenWidth - 350, 120);
            int barWidth = 26;
            int barHeight = 200;

            int fillHeight = (int)(barHeight * percent);

            // 배경
            var barRect = new Rectangle((int)position.X, (int)position.Y, barWidth, barHeight);
            spriteBatch.Draw(MyModSystem.PixelTexture, barRect, Color.Black * 0.5f);

            // 채워진 영역(아래 -> 위)
            var fillRect = new Rectangle(
                (int)position.X,
                (int)(position.Y + (barHeight - fillHeight)),
                barWidth,
                fillHeight
            );

            if (fillHeight > 0)
            {
                // 기본 채움
                spriteBatch.Draw(MyModSystem.PixelTexture, fillRect, Color.DodgerBlue);

                // ✅ 일정 간격 스캔라인(여러 줄) + 글로우 스윕
                // ✅ 채워진 구간(fillRect)에만 적용
                if (config.EnableShieldElectricEffect)
                {
                    DrawScanLineTrain_FixedStrong(spriteBatch, fillRect, (int)Main.GameUpdateCount);
                }
            }

            // 프레임(윗 구슬/장식 포함)
            Texture2D frameTex = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldFrame").Value;
            float frameScale = 1f;
            Vector2 frameOffset = new Vector2((barWidth - frameTex.Width * frameScale) / 2f, -40f);
            spriteBatch.Draw(frameTex, position + frameOffset, null, Color.White, 0f, Vector2.Zero, frameScale, SpriteEffects.None, 0f);

            // 텍스트
            string text = $"{shield} / {maxShield}";
            Vector2 textPos = new Vector2(position.X + (barWidth / 2f), position.Y + barHeight + 18f);
            Utils.DrawBorderString(spriteBatch, text, textPos, Color.White, 0.9f, 0.5f, 0.5f);

            Vector2 regenPos = new Vector2(textPos.X, textPos.Y + 16f);
            DrawRegenAndCooldownInfo(spriteBatch, regenPos, modPlayer, config, alignLeft: false);
        }

        /// <summary>
        /// ✅ 일정 간격의 스캔라인 여러 개가 아래→위로 계속 올라가는 효과
        /// - 텍스처 없이 PixelTexture만 사용
        /// - fillRect 내부에서만 그려짐
        /// - 고정 강도(낮을수록 강해짐 없음)
        /// - ✅ 라인 수는 보조 레이어 제거로 절반
        /// - ✅ 속도도 절반(3.10 -> 1.55)
        /// </summary>
        private static void DrawScanLineTrain_FixedStrong(SpriteBatch spriteBatch, Rectangle fillRect, int tick)
        {
            if (fillRect.Width <= 2 || fillRect.Height <= 2)
                return;

            int pad = 1;
            int left = fillRect.X + pad;
            int right = fillRect.Right - pad;
            int top = fillRect.Y + pad;
            int bottom = fillRect.Bottom - pad;

            int width = right - left;
            int height = bottom - top;
            if (width <= 2 || height <= 2)
                return;

            // ✅ 속도 절반
            float speedPxPerTick = 1.55f;

            // 강도(원래 값 유지)
            float intensity = 1.55f;

            // 라인 간격(픽셀)
            int spacing = 22;
            spacing = ClampInt(spacing, 10, 60);

            // 글로우 폭
            int band = ClampInt(10 + (int)Math.Floor(intensity * 4f), 10, 18);

            // 기본 글로우
            float baseGlow = 0.12f;
            spriteBatch.Draw(
                MyModSystem.PixelTexture,
                new Rectangle(left, top, width, height),
                new Color(170, 240, 255) * baseGlow
            );

            // 아래→위 이동 오프셋
            float baseOffset = (tick * speedPxPerTick) % spacing;

            // 주 레이어만
            for (float yCenter = bottom - baseOffset; yCenter >= top - band; yCenter -= spacing)
            {
                DrawScanLineAtY(spriteBatch, left, top, width, height, (int)Math.Round(yCenter), band, intensity);
            }
        }

        private static void DrawScanLineAtY(SpriteBatch spriteBatch, int left, int top, int width, int height,
                                            int yCenter, int band, float intensity)
        {
            DrawHLineClamped(spriteBatch, left, top, width, height, yCenter, 1,
                new Color(240, 255, 255) * (0.55f * intensity));

            DrawHLineClamped(spriteBatch, left, top, width, height, yCenter - 2, 1,
                new Color(210, 255, 255) * (0.24f * intensity));

            for (int d = 1; d <= band; d++)
            {
                float k = 1f - (d / (float)(band + 1));
                k *= k;

                float a = 0.26f * intensity * k;
                if (a <= 0.001f) continue;

                Color c = new Color(120, 205, 255) * a;

                DrawHLineClamped(spriteBatch, left, top, width, height, yCenter - d, 1, c);
                DrawHLineClamped(spriteBatch, left, top, width, height, yCenter + d, 1, c);
            }
        }

        private static void DrawHLineClamped(SpriteBatch spriteBatch, int left, int top, int width, int height,
                                             int y, int thick, Color color)
        {
            int yMin = top;
            int yMax = top + height - thick;

            if (y < yMin) return;
            if (y > yMax) return;

            spriteBatch.Draw(
                MyModSystem.PixelTexture,
                new Rectangle(left, y, width, thick),
                color
            );
        }

        // =========================
        // ICON UI (5 icons)
        // =========================
        private void DrawShieldIcons(SpriteBatch spriteBatch, MyModPlayer modPlayer, ShieldModConfig config)
        {
            Texture2D icon = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldIcon").Value;

            int iconCount = 5;
            float shieldPerIcon = modPlayer.maxShield / (float)iconCount;
            float shieldValue = modPlayer.shield;

            Vector2 startPos = new Vector2(Main.screenWidth - 350, 120);
            int spacing = 36;

            // 현재 칸만 펄스 + 보호막 0이면 펄스 OFF
            int activeIndex = -1;
            if (shieldValue > 0f)
            {
                int activeBucket = (int)Math.Floor((shieldValue - 0.0001f) / shieldPerIcon);
                activeBucket = ClampInt(activeBucket, 0, iconCount - 1);
                activeIndex = iconCount - 1 - activeBucket;
            }

            for (int i = 0; i < iconCount; i++)
            {
                int bucket = iconCount - 1 - i;

                float min = bucket * shieldPerIcon;
                float max = (bucket + 1) * shieldPerIcon;

                float alpha = 0f;
                if (shieldValue >= max) alpha = 1f;
                else if (shieldValue > min) alpha = (shieldValue - min) / shieldPerIcon;

                alpha = (alpha <= 0f) ? 0.1f : MathHelper.Clamp(alpha, 0.1f, 1f);

                float scale = 1f;
                if (config.UseShieldPulseEffect && i == activeIndex)
                    scale = 1f + 0.13f * (1f + (float)Math.Sin(Main.GameUpdateCount / 10f));

                Vector2 origin = icon.Size() / 2f;
                Vector2 pos = new Vector2(startPos.X, startPos.Y + i * spacing) + origin;
                spriteBatch.Draw(icon, pos, null, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            string text = $"{modPlayer.shield} / {modPlayer.maxShield}";
            Vector2 textPos = new Vector2(startPos.X + 16, startPos.Y + iconCount * spacing + 8);
            Utils.DrawBorderString(spriteBatch, text, textPos, Color.White, 0.7f, 0.5f, 0.5f);

            Vector2 regenPos = new Vector2(textPos.X, textPos.Y + 16f);
            DrawRegenAndCooldownInfo(spriteBatch, regenPos, modPlayer, config, alignLeft: true);
        }

        // =========================
        // UTIL
        // =========================
        private static int ClampInt(int v, int min, int max)
        {
            if (v < min) return min;
            if (v > max) return max;
            return v;
        }

        private void DrawRegenAndCooldownInfo(SpriteBatch spriteBatch, Vector2 anchor, MyModPlayer modPlayer, ShieldModConfig config, bool alignLeft)
        {
            if (!config.ShowRegenCooldownIndicator)
                return;

            int cooldown = modPlayer.ShieldBreakCooldownTicks;
            (float natural, float aegis) = modPlayer.GetShieldRegenPerSecond();

            // 흡수의 인장(흡수 반감) 표시
            var sigil = Main.LocalPlayer.GetModPlayer<AbsorptionSigilPlayer>();
            bool hasSigil = sigil?.HasAbsorptionSigil == true;
            int penaltyTicks = sigil?.SiphonPenaltyTicks ?? 0;

            // Localization helper (uses Localization/*.hjson)
            string T(string key, params object[] args)
                => Language.GetTextValue($"Mods.ShieldMod.UI.{key}", args);

            string label;
            Color color;

            // ✅ 흡수의 인장 착용 시: '쿨다운' 대신 '흡수 감소' 타이머를 우선 표시
            if (hasSigil)
            {
                // 패널티 타이머가 아직 잡히지 않은 경우(초기 동기화 타이밍 등)에도,
                // 실드 파괴 쿨다운이 켜져 있다면 그 시간을 대체 표기로 사용한다.
                int showTicks = penaltyTicks > 0 ? penaltyTicks : cooldown;

                if (showTicks > 0)
                {
                    label = T("AbsorbReduced", FormatSeconds(showTicks));
                    color = new Color(250, 140, 110);
                }
                else
                {
                    label = T("AbsorbNormal");
                    color = Color.LightGray;
                }
            }
            else
            {
                if (cooldown > 0)
                {
                    label = T("Cooldown", FormatSeconds(cooldown));
                    color = new Color(250, 140, 110);
                }
                else if (natural <= 0f && aegis <= 0f)
                {
                    label = T("RegenPaused");
                    color = Color.LightGray;
                }
                else
                {
                    label = T("Regen", natural.ToString("0.#"));
                    if (aegis > 0f)
                        label += $" (+{aegis:0.#})";

                    float k = MathHelper.Clamp(natural / 12f, 0f, 1f);
                    color = Color.Lerp(new Color(120, 190, 255), new Color(70, 230, 255), k);
                }
            }

            float scale = 0.65f; // 30% smaller (0.85 * 0.7)
            Utils.DrawBorderString(spriteBatch, label, anchor, color, scale, alignLeft ? 0f : 0.5f, 0f);
        }

        private static string FormatSeconds(int ticks)
        {
            return $"{(ticks / 60f):0.0}s";
        }
    }
}

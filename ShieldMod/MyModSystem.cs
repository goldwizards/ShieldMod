using Terraria;
using Terraria.ID;               // ← 추가
using Terraria.Localization;     // ← 추가
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using ShieldMod.UI;

namespace ShieldMod
{
    public class MyModSystem : ModSystem
    {
        private UserInterface shieldInterface;
        internal ShieldUI shieldUI;

        public static Texture2D PixelTexture;

        // ■ 커스텀 레시피 그룹 등록 (은/텅스텐, 데모/크림테인)
        public override void AddRecipeGroups()
        {
            var anySilverBar = new RecipeGroup(
                () => Language.GetTextValue("LegacyMisc.37") + " Silver/Tungsten Bar",
                ItemID.SilverBar, ItemID.TungstenBar
            );
            RecipeGroup.RegisterGroup("ShieldMod:AnySilverBar", anySilverBar);

            var anyDemoniteBar = new RecipeGroup(
                () => Language.GetTextValue("LegacyMisc.37") + " Demonite/Crimtane Bar",
                ItemID.DemoniteBar, ItemID.CrimtaneBar
            );
            RecipeGroup.RegisterGroup("ShieldMod:AnyDemoniteBar", anyDemoniteBar);
        }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                shieldUI = new ShieldUI();
                shieldInterface = new UserInterface();
                shieldInterface.SetState(shieldUI);
            }
        }

        public override void UpdateUI(GameTime gameTime)
        {
            shieldInterface?.Update(gameTime);
        }

        public override void PostUpdateEverything()
        {
            // 완전 흡수인데도 엔진 내부에서 빨간 '1' CombatText가 생성되는 케이스가 있어
            // 그 상황(플래그가 켜진 짧은 구간)에는 플레이어 근처에 생성된 "작은 빨간 숫자" CombatText를 비활성화합니다.
            if (Main.dedServ) return;

            Player p = Main.LocalPlayer;
            if (p == null || !p.active) return;

            var mp = p.GetModPlayer<MyModPlayer>();
            if (mp == null || mp.suppressRedDamageTextTicks <= 0) return;

            Vector2 center = p.Center;
            const float maxDist = 140f;

            for (int i = 0; i < Main.combatText.Length; i++)
            {
                CombatText ct = Main.combatText[i];
                if (ct == null || !ct.active) continue;

                // 위치 필터(플레이어 근처만)
                if (Vector2.Distance(ct.position, center) > maxDist) continue;

                // "빨간 숫자" 판정(대략적인 색상)
                Color c = ct.color;
                if (c.R < 180 || c.G > 120 || c.B > 120) continue;

                // 숫자 파싱(비숫자는 무시)
                if (!TryParsePositiveInt(ct.text, out int val)) continue;

                // 완전 흡수 케이스에서 문제였던 건 주로 최소 1(혹은 아주 작은 값)
                if (val <= 2)
                {
                    ct.active = false;
                }
            }
        }

        private static bool TryParsePositiveInt(string s, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(s)) return false;

            bool any = false;
            int acc = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch >= '0' && ch <= '9')
                {
                    any = true;
                    acc = (acc * 10) + (ch - '0');
                    if (acc > 9999) break;
                }
            }
            if (!any) return false;
            value = acc;
            return value > 0;
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            // ✅ 메인 스레드-safe: 여기서 PixelTexture 생성
            if (PixelTexture == null)
            {
                PixelTexture = new Texture2D(Main.graphics.GraphicsDevice, 1, 1);
                PixelTexture.SetData(new[] { Color.White });
            }

            int index = layers.FindIndex(layer => layer.Name == "Vanilla: Resource Bars");
            if (index != -1)
            {
                layers.Insert(index + 1, new LegacyGameInterfaceLayer(
                    "ShieldMod: Shield UI",
                    delegate
                    {
                        shieldInterface?.Draw(Main.spriteBatch, new GameTime());
                        return true;
                    },
                    InterfaceScaleType.UI));
            }

            // ✅ 타인(다른 유저) 남은 보호막 확인: 머리 위 체력바 스타일로 2px 보호막 라인 표시
            // - 바닐라 Entity Health Bars 이후에 그려서, 체력바/이름 표시와 겹침이 덜합니다.
            int hb = layers.FindIndex(layer => layer.Name == "Vanilla: Entity Health Bars");
            if (hb != -1)
            {
                layers.Insert(hb + 1, new LegacyGameInterfaceLayer(
                    "ShieldMod: Player Shield Overhead",
                    delegate
                    {
                        DrawOtherPlayerShieldLine(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.UI));
            }
        }

        private static void DrawOtherPlayerShieldLine(SpriteBatch spriteBatch)
        {
            if (Main.dedServ) return;

            Player local = Main.LocalPlayer;
            if (local == null || !local.active) return;

            // 화면 난잡함 방지: 너무 멀면 안 그림(필요 시 값 조정)
            const float maxDistSq = 1800f * 1800f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (p == null || !p.active || p.dead) continue;
                if (i == Main.myPlayer) continue; // 내 UI는 기존 ShieldUI가 있으니 제외

                // 거리 제한
                if (Vector2.DistanceSquared(p.Center, local.Center) > maxDistSq) continue;

                var mp = p.GetModPlayer<MyModPlayer>();
                if (mp == null || mp.maxShield <= 0) continue;
                if (mp.shield <= 0) continue;
                bool showWhenFull = ModContent.GetInstance<ShieldModConfig>().ShowOtherPlayersShieldWhenFull;
                if (!showWhenFull && mp.shield >= mp.maxShield) continue; // 기본: 풀일 때 숨김

                float frac = mp.maxShield > 0 ? (mp.shield / (float)mp.maxShield) : 0f;
                frac = MathHelper.Clamp(frac, 0f, 1f);

                // 위치: 머리 위(바닐라 체력바가 뜨는 위치 근처)
                Vector2 pos = p.Top - Main.screenPosition;
                pos.Y -= 18f; // 체력바 기준으로 2px 위쪽 느낌

                const int w = 46;
                const int h = 2; // ✅ 요청: 2px

                int x = (int)(pos.X - w * 0.5f);
                int y = (int)pos.Y;

                // 배경(얇게)
                spriteBatch.Draw(PixelTexture, new Rectangle(x, y, w, h), Color.Black * 0.55f);
                // 채움(파랑)
                spriteBatch.Draw(PixelTexture, new Rectangle(x, y, (int)(w * frac), h), new Color(60, 170, 255) * 0.95f);
            }
        }

        public override void Unload()
        {
            if (!Main.dedServ)
                Main.QueueMainThreadAction(() => { PixelTexture?.Dispose(); PixelTexture = null; });
        }
    }
}
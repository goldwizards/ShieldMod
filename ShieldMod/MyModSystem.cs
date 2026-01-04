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
        }

        public override void Unload()
        {
            if (!Main.dedServ)
                Main.QueueMainThreadAction(() => { PixelTexture?.Dispose(); PixelTexture = null; });
        }
    }
}
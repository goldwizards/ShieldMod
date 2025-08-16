using Terraria;
using Terraria.ID;               // ← 추가
using Terraria.Localization;     // ← 추가
using Terraria.ModLoader;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
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
using Terraria;
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
            PixelTexture = null;
        }
    }
}

using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ShieldMod.Players; // ✅ 네임스페이스 제대로 지정

namespace ShieldMod.Layers
{
    public class ShieldHitEffectLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            return drawInfo.drawPlayer.active &&
                   !drawInfo.drawPlayer.dead &&
                   drawInfo.drawPlayer.GetModPlayer<MyModPlayer>().showHitEffect;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;

            Texture2D tex = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldHit", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Vector2 pos = player.Center - Main.screenPosition + new Vector2(0f, 4f);

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                pos,
                null,
                Color.White * 0.8f,
                0f,
                tex.Size() / 2f,
                1f,
                SpriteEffects.None,
                0
            ));
        }
    }
}

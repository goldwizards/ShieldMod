using Terraria;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;                 // Asset 캐싱
using ShieldMod.Players;

namespace ShieldMod.Layers
{
    public class ShieldHitEffectLayer : PlayerDrawLayer
    {
        private static Asset<Texture2D> _hitTex;

        public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.ArmOverItem);

        public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
        {
            var p = drawInfo.drawPlayer;
            return p != null && p.active && !p.dead && p.GetModPlayer<MyModPlayer>().showHitEffect;
        }

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            var player = drawInfo.drawPlayer;
            var mp = player.GetModPlayer<MyModPlayer>();

            int timer = mp.HitEffectTimer; // ✅ 공개 게터 사용
            if (timer <= 0) return;

            _hitTex ??= ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldHit", AssetRequestMode.ImmediateLoad);
            if (_hitTex == null || !_hitTex.IsLoaded) return;

            // 0~1 비율로 페이드/스케일 (기본 15프레임 기준)
            float t = timer / 15f;
            if (t < 0f) t = 0f; if (t > 1f) t = 1f;

            float alpha = t;
            float scale = 1.0f + 0.15f * (1f - t);

            // ✅ 위치는 파일 2 버전 그대로 유지
            Vector2 pos = player.Center - Main.screenPosition + new Vector2(0f, 4f);

            Texture2D tex = _hitTex.Value;
            Vector2 origin = tex.Size() * 0.5f;

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                pos,
                null,
                Color.Cyan * alpha,      // Cyan 페이드
                0f,
                origin,
                scale,
                SpriteEffects.None,
                0
            ));
        }
    }
}
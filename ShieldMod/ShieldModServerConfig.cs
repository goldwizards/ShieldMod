using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace ShieldMod
{
    // 멀티플레이 밸런스는 서버가 권위(authoritative)를 갖도록 ServerSide로 분리합니다.
    // 싱글플레이에서는 기존 ClientSide(ShieldModConfig)의 비율을 그대로 사용합니다.
    public class ShieldModServerConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Label("Shield Max Health Ratio (Server)")]
        [Tooltip("Multiplayer only: Server-authoritative maximum shield ratio.\nSet maximum shield as a percentage of the player's max health (statLifeMax2).\nExample: 1.00 = 100%, 0.25 = 25%")]
        [Range(0.25f, 1f)]
        [Increment(0.05f)]
        [DefaultValue(1f)]
        [Slider]
        public float ShieldMaxRatio { get; set; } = 1f;
    }
}

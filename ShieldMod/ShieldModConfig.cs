using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace ShieldMod
{
    public class ShieldModConfig : ModConfig
    {
        // 필요에 따라 ServerSide 로 바꾸셔도 됩니다.
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [Label("Show blue numbers")]
        [Tooltip("Displays blue numbers when the shield absorbs damage.")]
        [DefaultValue(true)]
        public bool ShowShieldText;

        [Label("How the Shield UI is displayed")]
        [Tooltip("Choose the UI type from the bar or icon method.")]
        [DefaultValue(ShieldUIDisplayStyle.Bar)]
        public ShieldUIDisplayStyle ShieldUIStyle;

        [Label("Pulse animation effect")]
        [Tooltip("When the icon UI is on, apply pulse animation to only one remaining shield.")]
        [DefaultValue(false)]
        public bool UseShieldPulseEffect;

        // ▼ 공백 없는 Header를 쓰려면 [Header("Shield")] 처럼 쓰세요.
        // 이번엔 아예 Header를 빼서 안전하게 처리합니다.

        [Label("Shield Max Health Ratio")]
        [Tooltip("Set the maximum shield as a percentage of the player's max health (statLifeMax2).\nExample: 1.00 = 100%, 0.25 = 25%")]
        [Range(0.25f, 1f)]
        [Increment(0.05f)]
        [DefaultValue(1f)]
        [Slider]
        public float ShieldMaxRatio { get; set; } = 1f;

        public enum ShieldUIDisplayStyle
        {
            [Label("Bar UI")]
            Bar,

            [Label("Icon UI")]
            Icon
        }
    }
}
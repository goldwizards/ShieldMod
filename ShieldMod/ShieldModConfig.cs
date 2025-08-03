using Terraria.ModLoader.Config;
using System.ComponentModel;

namespace ShieldMod
{
    public class ShieldModConfig : ModConfig
    {
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

        // ⚠️ 내부 enum으로 정의해야 한글 라벨 UI에 정상 표시됨!
        public enum ShieldUIDisplayStyle
        {
            [Label("Bar UI")]
            Bar,

            [Label("Icon UI")]
            Icon
        }
    }
}

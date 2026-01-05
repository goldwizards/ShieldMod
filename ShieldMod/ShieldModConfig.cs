using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader.Config;

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

        [Label("Pulse animation effect (icon UI)")]
        [Tooltip("When the icon UI is enabled, apply pulse animation to the current shield segment only.")]
        [DefaultValue(false)]
        public bool UseShieldPulseEffect;

        // ✅ 막대 UI 스캔라인/글로우 이펙트 On/Off (기존 필드명 유지: 설정 파일 호환성)
        [Label("Scanline effect (bar UI)")]
        [Tooltip("Draws scanlines and glow sweep animation inside the filled part of the bar UI.")]
        [DefaultValue(true)]
        public bool EnableShieldElectricEffect;

        [Label("Show regen/cooldown hint")]
        [Tooltip("Displays your current shield regeneration tier (1/2/3/5/8/12/20 per second) or break cooldown as a small HUD label.")]
        [DefaultValue(true)]
        public bool ShowRegenCooldownIndicator;

        [Label("Shield hit effect color")]
        [Tooltip("Color used for the shield hit overlay and absorb text (default: blue).")]
        [DefaultValue(typeof(Color), "30, 144, 255, 255")]
        public Color ShieldHitColor { get; set; } = Color.DodgerBlue;

        [Label("Shield regen effect color")]
        [Tooltip("Color used for shield heal text effects.")]
        [DefaultValue(typeof(Color), "0, 255, 255, 255")]
        public Color ShieldRegenColor { get; set; } = Color.Cyan;

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

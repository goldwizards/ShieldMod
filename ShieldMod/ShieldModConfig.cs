using Microsoft.Xna.Framework;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ShieldMod
{
    public class ShieldModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;        [Header("$Mods.ShieldMod.Configs.ShieldModConfig.Headers.VFXNumbers")]


        [Label("Show shield absorption numbers")]
        [Tooltip("Displays damage numbers when your shield absorbs hits.")]
        [DefaultValue(true)]
        public bool ShowShieldText;
        [Header("$Mods.ShieldMod.Configs.ShieldModConfig.Headers.UI")]


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
        [Header("$Mods.ShieldMod.Configs.ShieldModConfig.Headers.Multiplayer")]


        [Label("Show other players' shield when full")]
        [Tooltip("When enabled, the overhead 2px shield line is shown whenever other players have shield > 0 (even at full).")]
        [DefaultValue(false)]
        public bool ShowOtherPlayersShieldWhenFull;
        [Header("$Mods.ShieldMod.Configs.ShieldModConfig.Headers.VFXHitEffects")]



[Label("Hit effect style")]
        [Tooltip("Select how the shield hit visual effect is displayed.")]
        [DefaultValue(ShieldHitVfxStyle.Subtle)]
        public ShieldHitVfxStyle HitEffectStyle = ShieldHitVfxStyle.Subtle;

        [Label("Shield hit effect color")]
        [Tooltip("Color used for the shield hit overlay and absorption numbers.")]
        [DefaultValue(typeof(Color), "0, 110, 255, 255")]
        public Color ShieldHitColor { get; set; } = new Color(0, 110, 255, 255);

        [Label("Shield regen effect color")]
        [Tooltip("Color used for shield heal text effects.")]
        [DefaultValue(typeof(Color), "0, 110, 255, 255")]
        public Color ShieldRegenColor { get; set; } = Color.SkyBlue;
        [Header("$Mods.ShieldMod.Configs.ShieldModConfig.Headers.BubbleShield")]


        

        [Label("Enable bubble shield (player)")]
        [Tooltip("Draws a simple bubble overlay around your character when shield > 0.")]
        [DefaultValue(false)]
        public bool EnableBubbleShield;

        [Label("Bubble shield opacity")]
        [Tooltip("Opacity of the bubble overlay (0.0 = invisible, 1.0 = solid).")]
        [Range(0f, 1f)]
        [Increment(0.05f)]
        [DefaultValue(0.45f)]
        public float BubbleShieldOpacity = 0.45f;

        [Label("Pulse when shield is low")]
        [Tooltip("Adds a subtle pulse to the bubble when shield is at or below 25%.")]
        [DefaultValue(true)]
        public bool BubbleShieldPulseOnLow = true;

        [Label("Bubble bobbing")]
        [Tooltip("Adds a small vertical bobbing motion to the bubble.")]
        [DefaultValue(true)]
        public bool BubbleShieldBob = true;

        [Label("Extra glow pass")]
        [Tooltip("Draws the bubble twice for a slightly stronger glow.")]
        [DefaultValue(true)]
        public bool BubbleShieldDoubleDraw = true;

        [Label("Shield Max Health Ratio")]
        [Tooltip("Set the maximum shield as a percentage of the player's max health (statLifeMax2).\nExample: 1.00 = 100%, 0.25 = 25%")]
        [Range(0.25f, 1f)]
        [Increment(0.05f)]
        [DefaultValue(1f)]
        [Slider]
        public float ShieldMaxRatio { get; set; } = 1f;


public enum ShieldHitVfxStyle
{
    [Label("Off")]
    Off,

    [Label("Subtle")]
    Subtle,

    [Label("Normal")]
    Normal,

    [Label("Strong")]
    Strong,

    [Label("Impact-only")]
    ImpactOnly
}


        public enum ShieldUIDisplayStyle
        {
            [Label("Bar UI")]
            Bar,

            [Label("Icon UI")]
            Icon
        }
    }
}

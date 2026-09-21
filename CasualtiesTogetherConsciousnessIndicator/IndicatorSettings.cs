using BepInEx.Configuration;

namespace CasualtiesTogetherConsciousnessIndicator;

internal sealed class IndicatorSettings
{
    public readonly ConfigEntry<bool> Enabled;
    public readonly ConfigEntry<string> IconFile;
    public readonly ConfigEntry<bool> DoTint;
    public readonly ConfigEntry<float> Scale;
    public readonly ConfigEntry<AnimationType> AnimationType;
    public readonly IconTextureLoader Icon = new();

    public IndicatorSettings(ConfigFile config, string section, string defaultIcon)
    {
        Enabled = config.Bind(
            section,
            "Enabled",
            true,
            "Enable this indicator");
        IconFile = config.Bind(
            section,
            "IconFile",
            defaultIcon,
            $"Which file within BepInEx/plugins/{Plugin.ModName} to use as the icon");
        DoTint = config.Bind(
            section,
            "DoTint",
            true,
            "Tint the icons with the player's color");
        Scale = config.Bind(
            section,
            "Scale",
            6f,
            "The scale of the icons");
        AnimationType = config.Bind(
            section,
            "AnimationType",
            CasualtiesTogetherConsciousnessIndicator.AnimationType.None,
            "How to animate the icon above the player");
    }
}

# Casualties: Unknown Consciousness Indicator

Shows a customizable indicator above players who are unconscious or sleeping. Works in multiplayer and in vanilla.

# Installation

1. Download CasualtiesTogetherConsciousnessIndicator.zip from [Releases](https://github.com/Voxeles/CasualtiesTogetherConsciousnessIndicator/releases)
2. Unzip the directory in "Casualties Unknown Demo/BepInEx/plugins"
3. Add any icons you want to "Casualties Unknown Demo/BepInEx/plugins/CasualtiesTogetherConsciousnessIndicator"

# Features

1. Shows a custom indicator above players who are unconscious, this indicator can be set to any image you want. The image file is hot-reloaded, no need to restart the game.
2. Each indicator has its own enabled, icon file, tint, scale, and animation settings (`None`, `RotateAround`, or `Jumping`).
3. Works in multiplayer (client-side), without the network running (offline), or without the multiplayer mod installed (vanilla)

# Configuration

The mod can be configured either by editing the "BepInEx/config/cump.consciousness.indicator.cfg" config file,
or in-game through the console (see commands below).

# Commands

All five commands take `default` (unconsciousness) or `sleeping` as their first argument.

1. `ConsciousnessIndicatorEnabled <indicator> [true|false]` - Enable/disable the selected indicator; omit the value to toggle.
2. `ConsciousnessIndicatorIconFile <indicator> <file>` - Choose an image within BepInEx/plugins/CasualtiesTogetherConsciousnessIndicator.
3. `ConsciousnessIndicatorAnimationType <indicator> <type>` - Choose `None`, `RotateAround`, or `Jumping`.
4. `ConsciousnessIndicatorScale <indicator> <scale>` - Change the scale of the selected indicator.
5. `ConsciousnessIndicatorDoTint <indicator> [true|false]` - Enable/disable tinting with the player's color; omit the value to toggle.

For example, here's the config for the rotating stars unconsciousness indicator:

```text
ConsciousnessIndicatorEnabled default true
ConsciousnessIndicatorIconFile default star.png
ConsciousnessIndicatorAnimationType default RotateAround
ConsciousnessIndicatorScale default 6
ConsciousnessIndicatorDoTint default false
```

# Shoutouts

vee-m

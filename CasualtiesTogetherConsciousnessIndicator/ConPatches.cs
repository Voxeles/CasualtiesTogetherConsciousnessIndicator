using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace CasualtiesTogetherConsciousnessIndicator;

[HarmonyPatch]
internal class ConPatches
{
    private static readonly List<string> _files = [];
    private static readonly List<string> _indicators = ["default", "sleeping"];

    private static MethodBase TargetMethod()
    {
        if (!Plugin.MpModLoaded)
            return AccessTools.Method(typeof(ConsoleScript), nameof(ConsoleScript.RegisterAllCommands));
        return AccessTools.Method(Plugin.MpModCon, nameof(KrokoshaCasualtiesMP.Con._RegisterMultiplayerConsoleCommands));
    }

    private static void UpdateFiles()
    {
        try
        {
            _files.Clear();
            var dir = Directory.CreateDirectory(Plugin.TextureDir);
            foreach (var fileInfo in dir.GetFiles())
            {
                var ext = fileInfo.Extension.ToLowerInvariant();
                if (ext.Equals(".png") || ext.Equals(".jpg") || ext.Equals(".jpeg"))
                    _files.Add(fileInfo.Name);
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning($"Failed to read files: " + ex);
        }
    }

    private static IndicatorSettings GetSettings(string[] args, int requiredArguments)
    {
        ConsoleScript.instance.CheckArgumentCount(args, requiredArguments);
        if (args.Length > 3)
            throw new Exception("Expected an indicator (default or sleeping) followed by a value.");

        return args[1].ToLowerInvariant() switch
        {
            "default" => Plugin.Unconscious,
            "sleeping" => Plugin.Sleeping,
            _ => throw new Exception($"Unknown indicator \"{args[1]}\". Choose default or sleeping.")
        };
    }

    private static void Postfix()
    {
        UpdateFiles();
        ConsoleScript.Commands.Add(new Command(
            "ConsciousnessIndicatorEnabled",
            "Enable or disable the selected indicator",
            args =>
            {
                var settings = GetSettings(args, 1);
                var result = args.Length < 3 ? !settings.Enabled.Value : bool.Parse(args[2]);
                settings.Enabled.Value = result;
                ConsoleScript.instance.LogToConsole($"{args[1]} indicator {(result ? "enabled" : "disabled")}!");
            },
            new Dictionary<int, List<string>> {
                {0, _indicators}
            },
            ("indicator", "default or sleeping"),
            ("bool", "optional, leave empty to toggle")
        ));

        ConsoleScript.Commands.Add(new Command(
            "ConsciousnessIndicatorIconFile",
            $"Which file within BepInEx/plugins/{Plugin.ModName} to use as the selected indicator icon",
            args =>
            {
                var settings = GetSettings(args, 2);
                UpdateFiles();
                var path = Path.Combine(Plugin.TextureDir, args[2]);
                if (!File.Exists(path))
                    throw new Exception($"\nFile {path} does not exist!");

                settings.IconFile.Value = args[2];
                ConsoleScript.instance.LogToConsole($"{args[1]} indicator icon file set to {settings.IconFile.Value}!");
            },
            new Dictionary<int, List<string>> {
                {0, _indicators},
                {1, _files}
            },
            ("indicator", "default or sleeping"),
            ("file", $"a file within BepInEx/plugins/{Plugin.ModName}")
        ));

        ConsoleScript.Commands.Add(new Command(
            "ConsciousnessIndicatorAnimationType",
            "Set the animation for the selected indicator",
            args =>
            {
                var settings = GetSettings(args, 2);
                if (!Enum.TryParse(args[2], true, out AnimationType result) || !Enum.IsDefined(typeof(AnimationType), result))
                    throw new Exception($"Invalid animation type \"{args[2]}\"!");

                settings.AnimationType.Value = result;
                ConsoleScript.instance.LogToConsole($"{args[1]} indicator animation set to {result}!");
            },
            new Dictionary<int, List<string>> {
                {0, _indicators},
                {1, Enum.GetNames(typeof(AnimationType)).ToList()}
            },
            ("indicator", "default or sleeping"),
            ("type", "Animation type")
        ));

        ConsoleScript.Commands.Add(new Command(
            "ConsciousnessIndicatorScale",
            "The scale of the selected indicator icons",
            args =>
            {
                var settings = GetSettings(args, 2);
                var result = float.Parse(args[2]);
                settings.Scale.Value = result;
                ConsoleScript.instance.LogToConsole($"{args[1]} indicator icon scale set to {result}!");
            },
            new Dictionary<int, List<string>> {
                {0, _indicators}
            },
            ("indicator", "default or sleeping"),
            ("float", "the scale of the icons, 6 by default")
        ));

        ConsoleScript.Commands.Add(new Command(
            "ConsciousnessIndicatorDoTint",
            "Tint the selected indicator icons with the player's color",
            args =>
            {
                var settings = GetSettings(args, 1);
                var result = args.Length < 3 ? !settings.DoTint.Value : bool.Parse(args[2]);
                settings.DoTint.Value = result;
                ConsoleScript.instance.LogToConsole($"{args[1]} indicator icon tint {(result ? "enabled" : "disabled")}!");
            },
            new Dictionary<int, List<string>> { { 0, _indicators } },
            ("indicator", "default or sleeping"),
            ("bool", "optional, leave empty to toggle")
        ));
    }
}

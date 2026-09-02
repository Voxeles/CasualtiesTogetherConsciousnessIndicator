using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using KrokoshaCasualtiesMP;

namespace CasualtiesTogetherConsciousnessIndicator;

[HarmonyPatch(typeof(Con), nameof(Con._RegisterMultiplayerConsoleCommands))]
internal class ConPatches
{
	private static List<string> _files = [];

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

    private static void Postfix()
    {
        var command = new Command(
        	"ConsciousnessIndicatorEnabled",
        	"Enable the consciousness indicator",
        	args =>
	        {
		        bool result;
		        if (args.Length < 2)
			        result = !Plugin.ConfigEnabled.Value;
		        else
			        result = bool.Parse(args[1]);
        		Plugin.ConfigEnabled.Value = result;
		        Con.con.LogToConsole($"Consciousness indicator {(result ? "enabled" : "disabled")}!");
	        },
	        null,
	        ("bool", "optional, leave empty to toggle")
        );
        Con.RegisterCommand(command);

        command = new Command(
        	"ConsciousnessIndicatorIconFile",
        	$"Which file within BepinEx/plugins/{Plugin.ModName} to use as the indicator icon",
        	args =>
	        {
		        UpdateFiles();

        		Con.con.CheckArgumentCount(args, 1);

        		var path = Path.Combine(Plugin.TextureDir, args[1]);

        		if (!File.Exists(path))
        			throw new Exception($"\nFile {path} does not exist!");

		        Plugin.ConfigIconFile.Value = args[1];
        		Con.con.LogToConsole($"Consciousness indicator icon file set to {Plugin.ConfigIconFile.Value}!");
        	},
	        new Dictionary<int, List<string>> {
		        {0, _files}
	        },
	        ("file", $"a file within BepinEx/plugins/{Plugin.ModName}")
        );
        Con.RegisterCommand(command);
        UpdateFiles();

        command = new Command(
            "ConsciousnessIndicatorDoRotate",
            "Should the consciousness indicator be three rotating icons",
            args =>
            {
	            bool result;
	            if (args.Length < 2)
		            result = !Plugin.ConfigDoRotate.Value;
	            else
		            result = bool.Parse(args[1]);
	            Plugin.ConfigDoRotate.Value = result;
	            Con.con.LogToConsole($"Consciousness indicator rotation animation {(result ? "enabled" : "disabled")}!");
            },
            null,
            ("bool", "optional, leave empty to toggle")
        );
        Con.RegisterCommand(command);

        command = new Command(
	        "ConsciousnessIndicatorScale",
	        "The scale of the indicator icons",
	        args =>
	        {
		        Con.con.CheckArgumentCount(args, 1);

		        var result = float.Parse(args[1]);
		        Plugin.ConfigScale.Value = result;
		        Con.con.LogToConsole($"Consciousness indicator icon scale set to {result}!");
	        },
	        null,
	        ("float", "the scale of the icons, 6 by default")
        );
        Con.RegisterCommand(command);

        command = new Command(
	        "ConsciousnessIndicatorDoTint",
	        "Should the consciousness icons be tinted to the player's color",
	        args =>
	        {
		        bool result;
		        if (args.Length < 2)
			        result = !Plugin.ConfigDoTint.Value;
		        else
			        result = bool.Parse(args[1]);
		        Plugin.ConfigDoTint.Value = result;
		        Con.con.LogToConsole($"Consciousness indicator icon tint {(result ? "enabled" : "disabled")}!");
	        },
	        null,
	        ("bool", "optional, leave empty to toggle")
        );
        Con.RegisterCommand(command);
    }
}

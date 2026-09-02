using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using KrokoshaCasualtiesMP;

namespace CasualtiesTogetherConsciousnessIndicator;

[HarmonyPatch(typeof(Con), nameof(Con._RegisterMultiplayerConsoleCommands))]
internal class ConPatches
{
    private static void Postfix()
    {
        var command = new Command(
        	"ConsciousnessIndicatorEnabled",
        	"Enable the consciousness indicator",
        	args =>
        	{
        		Con.con.CheckArgumentCount(args, 1);
        
        		var result = bool.Parse(args[1]);
        		Plugin.ConfigEnabled.Value = result;
		        Con.con.LogToConsole(result
			        ? "Consciousness indicator enabled!"
			        : "Consciousness indicator disabled!");
	        }, new Dictionary<int, List<string>>
	        {
		        {0, ["true", "false"]}
	        },
	        ("enabled", "is the consciousness indicator enabled")
        );
        Con.RegisterCommand(command);

        command = new Command(
        	"ConsciousnessIndicatorIconFile",
        	$"Which icon file within BepinEx/plugins/{Plugin.ModName} to use",
        	args =>
        	{
        		Con.con.CheckArgumentCount(args, 1);
        
        		var path = Path.Combine(Plugin.TextureDir, args[1]);
        
        		if (!File.Exists(path))
        			throw new Exception($"File {path} does not exist!");
        
		        Plugin.ConfigIconFile.Value = args[1];
        		Con.con.LogToConsole($"Consciousness indicator icon file set to {Plugin.ConfigIconFile.Value}!");
        
        	},
        	null,
	        ("file", $"a file within BepinEx/plugins/{Plugin.ModName}, like icon.png")
        );
        Con.RegisterCommand(command);

        command = new Command(
            "ConsciousnessIndicatorDoRotate",
            "Should the consciousness indicator be three rotating icons",
            args =>
            {
                Con.con.CheckArgumentCount(args, 1);
		
                var result = bool.Parse(args[1]);
                Plugin.ConfigDoRotate.Value = result;
                Con.con.LogToConsole(result
	                ? "Consciousness indicator rotation enabled!"
	                : "Consciousness indicator rotation disabled!");
            }, new Dictionary<int, List<string>>
            {
	            {0, ["true", "false"]}
            },
            ("enabled", "are the three rotating icons enabled")
        );
        Con.RegisterCommand(command);
    }
}
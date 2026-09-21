using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CasualtiesTogetherConsciousnessIndicator;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("KrokoshaCasualtiesMP", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    public const string ModName = MyPluginInfo.PLUGIN_NAME;
    public const string ModVersion = MyPluginInfo.PLUGIN_VERSION;

    internal new static ManualLogSource Logger;

    private readonly Harmony _harmony = new(ModGuid);

    public static Plugin Instance { get; private set; } = null!;

    public static bool MpModLoaded = false;
    public static Type MpModCon;
    public static Type MpModNetBody;
    public static Type MpModNetPlayer;
    public static Type MpModColor24;
    public static MethodInfo MpModNetworkIsRunningGetter;
    public static MethodInfo MpModIsPlayerGetter;
    public static MethodInfo MpModNetPlayerGetter;
    public static FieldInfo MpModNetPlayerColorField;
    public static MethodInfo MpModToColorWithAlpha;

    public static ConfigEntry<bool> ConfigEnabled;
    public static ConfigEntry<string> ConfigIconFile;
    public static ConfigEntry<AnimationType> ConfigAnimationType;
    public static ConfigEntry<float> ConfigScale;
    public static ConfigEntry<bool> ConfigDoTint;

    public static string TextureDir;

    private float _t = 0f;

    public void Awake()
    {
        Logger = base.Logger;
        Instance = this;

        foreach (var assembly in AccessTools.AllAssemblies())
        {
            if (!assembly.GetName().Name.Equals("KrokoshaCasualtiesMP"))
                continue;
            MpModLoaded = true;
            var mpModScavMultiplayer = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.KrokoshaScavMultiplayer)}", true);
            MpModCon = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.Con)}", true);
            MpModNetBody = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.NetBody)}", true);
            MpModNetPlayer = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.NetPlayer)}", true);
            MpModColor24 = assembly.GetType($"{nameof(KrokoshaCasualtiesMP)}.{nameof(KrokoshaCasualtiesMP.Color24)}", true);
            MpModNetworkIsRunningGetter = AccessTools.PropertyGetter(mpModScavMultiplayer, nameof(KrokoshaCasualtiesMP.KrokoshaScavMultiplayer.network_system_is_running));
            MpModIsPlayerGetter = AccessTools.PropertyGetter(MpModNetBody, nameof(KrokoshaCasualtiesMP.NetBody.is_player));
            MpModNetPlayerGetter = AccessTools.PropertyGetter(MpModNetBody, nameof(KrokoshaCasualtiesMP.NetBody.player));
            MpModNetPlayerColorField = AccessTools.Field(MpModNetPlayer, nameof(KrokoshaCasualtiesMP.NetPlayer.plrcolor));
            MpModToColorWithAlpha = AccessTools.Method(MpModColor24, nameof(KrokoshaCasualtiesMP.Color24.ToColorWithAlpha), [typeof(float)]);
            break;
        }

        TextureDir = Path.Combine(Paths.PluginPath, $"{ModName}");
        Directory.CreateDirectory(TextureDir);

        ConfigEnabled = Config.Bind(
            "General",
            "Enabled",
            true,
            "Set to true to enable the consciousness indicator");
        ConfigIconFile = Config.Bind(
            "General",
            "IconFile",
            "zzz.png",
            "Which file within BepInEx/plugins/ConsciousnessIndicator to use as the icon");
        ConfigAnimationType = Config.Bind(
            "General",
            "AnimationType",
            AnimationType.None,
            "How to animate the icon above the player\nNone: simply show up above the player\nRotateAround: Three rotating icons around their head\nJumping: Moving up and down above their head");
        ConfigScale = Config.Bind(
            "General",
            "Scale",
            6f,
            "The scale of the icons");
        ConfigDoTint = Config.Bind(
            "General",
            "DoTint",
            true,
            "Set to true to tint the icons with the player's color");

        _harmony.PatchAll();

        Logger.LogInfo($"Plugin {ModName} is loaded!");
    }

    public void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        Instance = null;
    }

    public void LateUpdate()
    {
        if (!ConfigEnabled.Value)
            return;

        _t += Time.unscaledDeltaTime;
        if (_t < 3f)
            return;
        _t = 0f;

        foreach (var body in FindObjectsByType<Body>(FindObjectsSortMode.None))
        {
            var character = body.transform.parent.gameObject;
            if (character.TryGetComponent<PlayerConsciousnessIndicator>(out _))
                continue;
            object netPlayer = null;
            if (MpModLoaded && (bool)MpModNetworkIsRunningGetter.Invoke(null, null))
            {
                var netBody = body.GetComponent(MpModNetBody);
                var isPlayer = (bool)MpModIsPlayerGetter.Invoke(netBody, null);
                if (!isPlayer)
                    continue;
                netPlayer = MpModNetPlayerGetter.Invoke(netBody, null);
            }
            var indicator = character.AddComponent<PlayerConsciousnessIndicator>();
            indicator.body = body;
            indicator.netPlayer = netPlayer;
        }
    }

    internal static void PrintWarning(string message)
    {
        Logger.LogWarning(message);
        ConsoleScript.instance.LogToConsole($"<color=yellow>[{Plugin.ModName}] {message}</color>");
    }

    internal static void PrintError(string message)
    {
        Logger.LogError(message);
        ConsoleScript.instance.LogToConsole($"<color=red>[{Plugin.ModName}] {message}</color>");
    }
}
